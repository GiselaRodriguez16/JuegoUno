import socket
import threading
import json
from datetime import datetime                       # NUEVO (#3)

from database import SessionLocal
from models import Jugador, LogJuego, HistorialPartida   # NUEVO (#8): HistorialPartida
from models import Partida as PartidaBD             # NUEVO (#3): tabla Partidas
from juego import Partida, SEGUNDOS_UNO             # NUEVO (#7): SEGUNDOS_UNO                      # NUEVO (#3): lógica del juego

HOST = "0.0.0.0"   # Acepta conexiones de cualquier computadora de la red
PUERTO = 5000
MIN_JUGADORAS = 2
MAX_JUGADORAS = 8

clientes = []                 # Sockets conectados
jugadoras = {}                # conexion -> {"id": ..., "nombre": ...} (las que se unieron a la sala)
partida_iniciada = False
partida = None                # NUEVO (#3): la partida en curso (objeto Partida)
candado = threading.Lock()    # Evita que dos hilos modifiquen las listas al mismo tiempo


def enviar(conexion, datos):
    """Manda un mensaje JSON a una sola jugadora. Cada mensaje termina en salto de línea."""
    mensaje = json.dumps(datos) + "\n"
    conexion.sendall(mensaje.encode("utf-8"))


def enviar_a_todos(datos):
    """Manda el mismo mensaje a todas las conectadas."""
    with candado:
        for c in list(clientes):
            try:
                enviar(c, datos)
            except OSError:
                pass


# ---------- Base de datos ----------

def registrar_en_bd(nombre):
    """Busca a la jugadora por nombre o la crea. Regresa su id."""
    db = SessionLocal()
    try:
        jugador = db.query(Jugador).filter(Jugador.nombre == nombre).first()
        if jugador is None:
            jugador = Jugador(nombre=nombre)
            db.add(jugador)
            db.commit()
            db.refresh(jugador)
        return jugador.id
    finally:
        db.close()


def crear_partida_bd():                              # NUEVO (#3)
    """Crea un registro en la tabla Partidas y regresa su id."""
    db = SessionLocal()
    try:
        nueva = PartidaBD()
        db.add(nueva)
        db.commit()
        db.refresh(nueva)
        return nueva.id
    finally:
        db.close()


def registrar_log(id_partida, id_jugador, movimiento):   # NUEVO (#3)
    """Guarda un movimiento en LogJuego. Si falla, solo lo avisa en consola."""
    db = SessionLocal()
    try:
        db.add(LogJuego(
            id_partida=id_partida,
            id_jugador=id_jugador,
            movimiento=movimiento,
            fecha_hora=datetime.now()
        ))
        db.commit()
    except Exception as e:
        print(f"[!] No se pudo guardar el log: {e}")
    finally:
        db.close()



def guardar_historial(p):                            # NUEVO (#8)
    """Guarda en HistorialPartidas: 'Ganada' para la ganadora y 'Perdida' para las demás."""
    db = SessionLocal()
    try:
        for j in p.todas:
            resultado = "Ganada" if j["id"] == p.ganadora else "Perdida"
            db.add(HistorialPartida(
                id_partida=p.id,
                id_jugador=j["id"],
                resultado=resultado
            ))
        db.commit()
        print(f"[juego] Historial de la partida {p.id} guardado")
    except Exception as e:
        print(f"[!] No se pudo guardar el historial: {e}")
    finally:
        db.close()


def revisar_fin_de_partida(p):                       # NUEVO (#8)
    """Si la partida ya tiene ganadora: guarda el historial y deja el servidor libre para otra."""
    global partida, partida_iniciada
    with candado:
        if p.ganadora is None or p.historial_guardado:
            return
        p.historial_guardado = True
        if partida is p:
            partida = None
            partida_iniciada = False
            jugadoras.clear()
    print(f"[juego] Fin de la partida {p.id}")
    guardar_historial(p)

# ---------- Sala de espera ----------

def lista_jugadoras():
    """Lista de jugadoras en la sala. Se llama con el candado tomado."""
    return [{"id": j["id"], "nombre": j["nombre"]} for j in jugadoras.values()]


def avisar_sala():
    """Manda a todas la lista actualizada de la sala."""
    with candado:
        lista = lista_jugadoras()
    enviar_a_todos({"tipo": "sala", "jugadoras": lista})


# ---------- Juego ----------

def enviar_estado_a_todas():                         # NUEVO (#3)
    """Manda a cada jugadora SU propio estado (cada una ve solo su mano).
    Se usará cada vez que alguien tire o robe (commit #4 en adelante)."""
    with candado:
        if partida is None:
            return
        for conexion, j in jugadoras.items():
            try:
                enviar(conexion, partida.estado_para(j["id"]))
            except OSError:
                pass

def ejecutar_jugada(conexion, jugada):
    """Hace una jugada (tirar, robar, UNO...) de forma segura y avisa a todas.
    'jugada' es una función que recibe (partida, id_jugadora) y regresa None o un texto de error."""
    with candado:
        if partida is None or conexion not in jugadoras:
            return
        p = partida
        id_jugador = jugadoras[conexion]["id"]
        error = jugada(p, id_jugador)
        accion = p.ultima_accion
        id_partida = p.id
        logs = p.sacar_logs()
        token_uno = p.tomar_uno_nuevo()              # NUEVO (#7)

    if error:
        enviar(conexion, {"tipo": "error", "mensaje": error})
        return

    print(f"[juego] {accion}")
    for id_j, texto in logs:
        registrar_log(id_partida, id_j, texto)

    # NUEVO (#7): si alguien se quedó con 1 carta, arranca su temporizador
    if token_uno is not None:
        temporizador = threading.Timer(SEGUNDOS_UNO, se_acabo_tiempo_uno, args=(p, token_uno))
        temporizador.daemon = True
        temporizador.start()
        print(f"[juego] Temporizador de UNO: {SEGUNDOS_UNO} segundos")

    enviar_estado_a_todas()
    revisar_fin_de_partida(p)                        # NUEVO (#8)

def se_acabo_tiempo_uno(p, token):                   # NUEVO (#7)
    """La llama el temporizador cuando se acaban los segundos para cantar UNO."""
    with candado:
        if partida is not p:
            return                                   # Esa partida ya no existe
        castigada = p.vencer_uno(token)
        accion = p.ultima_accion
        id_partida = p.id
        logs = p.sacar_logs()

    if not castigada:
        return                                       # Sí cantó UNO a tiempo

    print(f"[juego] {accion}")
    for id_j, texto in logs:
        registrar_log(id_partida, id_j, texto)
    enviar_estado_a_todas()

def procesar_mensaje(conexion, datos):
    """Decide qué hacer según el tipo de mensaje que mandó la jugadora."""
    global partida_iniciada, partida
    tipo = datos.get("tipo")

    if tipo == "pedir_sala":
        with candado:
            lista = lista_jugadoras()
        enviar(conexion, {"tipo": "sala", "jugadoras": lista})

    elif tipo == "unirse":
        nombre = str(datos.get("nombre", "")).strip()
        error = None

        with candado:
            if not nombre:
                error = "Escribe tu nombre."
            elif partida_iniciada:
                error = "La partida ya empezó."
            elif conexion in jugadoras:
                error = "Ya estás en la sala."
            elif any(j["nombre"].lower() == nombre.lower() for j in jugadoras.values()):
                error = "Ese nombre ya está en la sala."
            elif len(jugadoras) >= MAX_JUGADORAS:
                error = "La sala está llena."
            else:
                try:
                    id_jugador = registrar_en_bd(nombre)
                    jugadoras[conexion] = {"id": id_jugador, "nombre": nombre}
                except Exception as e:
                    print(f"[!] Error con la base de datos: {e}")
                    error = "No se pudo guardar en la base de datos."

        if error:
            enviar(conexion, {"tipo": "error", "mensaje": error})
            return

        print(f"[sala] {nombre} se unió (id {id_jugador})")
        enviar(conexion, {"tipo": "registrado", "id": id_jugador, "nombre": nombre})
        avisar_sala()

    elif tipo == "empezar":
        error = None
        with candado:
            if partida_iniciada:
                return
            if conexion not in jugadoras:
                error = "Primero únete a la sala."
            elif len(jugadoras) < MIN_JUGADORAS:
                error = f"Se necesitan al menos {MIN_JUGADORAS} jugadoras."
            else:
                # NUEVO (#3): crear la partida, barajar y repartir
                try:
                    lista = lista_jugadoras()
                    id_partida = crear_partida_bd()
                    partida = Partida(id_partida, lista)
                    partida_iniciada = True
                except Exception as e:
                    print(f"[!] No se pudo crear la partida: {e}")
                    error = "No se pudo crear la partida."

        if error:
            enviar(conexion, {"tipo": "error", "mensaje": error})
            return

        print(f"[juego] ¡Partida {partida.id} iniciada! Carta inicial: {partida.carta_actual}")
        for j in lista:                              # NUEVO (#3): log del reparto
            registrar_log(partida.id, j["id"], "Recibió 7 cartas")

        enviar_a_todos({"tipo": "iniciar", "jugadoras": lista})

    elif tipo == "listo":                            # NUEVO (#3)
        # La ventana del juego ya abrió y pide su estado
        with candado:
            if partida is None or conexion not in jugadoras:
                return
            estado = partida.estado_para(jugadoras[conexion]["id"])
        enviar(conexion, estado)

    elif tipo == "tirar":
        try:
            indice = int(datos.get("indice", -1))
        except (TypeError, ValueError):
            indice = -1
        color = datos.get("color")                   # NUEVO (#6): solo se usa en comodines
        ejecutar_jugada(conexion, lambda p, id_j: p.tirar(id_j, indice, color))

    elif tipo == "robar":                            # NUEVO (#5)
        ejecutar_jugada(conexion, lambda p, id_j: p.robar_carta(id_j))

    
    elif tipo == "uno":                              # NUEVO (#7)
        ejecutar_jugada(conexion, lambda p, id_j: p.cantar_uno(id_j))

    else:
        print(f"[?] Mensaje desconocido: {datos}")


def atender_cliente(conexion, direccion):
    """Cada jugadora se atiende en su propio hilo."""
    global partida_iniciada, partida
    conexion.settimeout(None)
    print(f"[+] Se conectó {direccion[0]}")

    with candado:
        clientes.append(conexion)
        total = len(clientes)

    enviar(conexion, {"tipo": "bienvenida", "mensaje": "Conectado al servidor UNO"})
    enviar_a_todos({"tipo": "info", "mensaje": f"Jugadoras conectadas: {total}"})

    try:
        with conexion:
            archivo = conexion.makefile("r", encoding="utf-8")
            for linea in archivo:
                linea = linea.strip()
                if not linea:
                    continue
                try:
                    datos = json.loads(linea)
                except json.JSONDecodeError:
                    print(f"[!] Mensaje inválido de {direccion[0]}: {linea}")
                    continue
                print(f"[{direccion[0]}] {datos}")
                procesar_mensaje(conexion, datos)
    except OSError:
        pass
        finally:
        p = None
        with candado:
            if conexion in clientes:
                clientes.remove(conexion)
            salio = jugadoras.pop(conexion, None)
            total = len(clientes)

            # NUEVO (#8): si se va alguien a media partida, se quita del juego
            if salio and partida is not None:
                p = partida
                p.quitar_jugadora(salio["id"])
                accion = p.ultima_accion
                logs = p.sacar_logs()

            if total == 0:
                partida_iniciada = False   # Si se van todas, se puede empezar otra
                partida = None

        print(f"[-] Se desconectó {direccion[0]}")
        enviar_a_todos({"tipo": "info", "mensaje": f"Jugadoras conectadas: {total}"})

        if p is not None:                  # NUEVO (#8): estaba jugando
            print(f"[juego] {accion}")
            for id_j, texto in logs:
                registrar_log(p.id, id_j, texto)
            enviar_estado_a_todas()
            revisar_fin_de_partida(p)
        elif salio:                        # Estaba en la sala de espera
            avisar_sala()


def iniciar_servidor():
    servidor = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    servidor.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    servidor.bind((HOST, PUERTO))
    servidor.listen()
    servidor.settimeout(1.0)  # Permite cerrar el servidor con Ctrl+C en Windows

    print(f"Servidor UNO escuchando en el puerto {PUERTO}... (Ctrl+C para cerrar)")

    try:
        while True:
            try:
                conexion, direccion = servidor.accept()
            except socket.timeout:
                continue
            hilo = threading.Thread(
                target=atender_cliente,
                args=(conexion, direccion),
                daemon=True
            )
            hilo.start()
    except KeyboardInterrupt:
        print("\nServidor detenido.")
    finally:
        servidor.close()


if __name__ == "__main__":
    iniciar_servidor()