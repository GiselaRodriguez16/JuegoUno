import socket
import threading
import json
from datetime import datetime                       # NUEVO (#3)

from database import SessionLocal
from models import Jugador, LogJuego                # NUEVO (#3): LogJuego
from models import Partida as PartidaBD             # NUEVO (#3): tabla Partidas
from juego import Partida                           # NUEVO (#3): lógica del juego

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
        with candado:
            if conexion in clientes:
                clientes.remove(conexion)
            salio = jugadoras.pop(conexion, None)
            total = len(clientes)
            if total == 0:
                partida_iniciada = False   # Si se van todas, se puede empezar otra
                partida = None             # NUEVO (#3)
        print(f"[-] Se desconectó {direccion[0]}")
        enviar_a_todos({"tipo": "info", "mensaje": f"Jugadoras conectadas: {total}"})
        if salio:
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