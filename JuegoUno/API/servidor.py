import socket
import threading
import json

from database import SessionLocal   # NUEVO: para guardar jugadoras en la BD
from models import Jugador          # NUEVO

HOST = "0.0.0.0"   # Acepta conexiones de cualquier computadora de la red
PUERTO = 5000
MIN_JUGADORAS = 2  # NUEVO
MAX_JUGADORAS = 8  # NUEVO

clientes = []                 # Sockets conectados
jugadoras = {}                # NUEVO: conexion -> {"id": ..., "nombre": ...} (las que se unieron a la sala)
partida_iniciada = False      # NUEVO
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


# ---------- NUEVO: sala de espera ----------

def lista_jugadoras():
    """Lista de jugadoras en la sala. Se llama con el candado tomado."""
    return [{"id": j["id"], "nombre": j["nombre"]} for j in jugadoras.values()]


def avisar_sala():
    """Manda a todas la lista actualizada de la sala."""
    with candado:
        lista = lista_jugadoras()
    enviar_a_todos({"tipo": "sala", "jugadoras": lista})


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


def procesar_mensaje(conexion, datos):
    """Decide qué hacer según el tipo de mensaje que mandó la jugadora."""
    global partida_iniciada
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
                partida_iniciada = True
                lista = lista_jugadoras()

        if error:
            enviar(conexion, {"tipo": "error", "mensaje": error})
            return

        print("[sala] ¡La partida empieza!")
        enviar_a_todos({"tipo": "iniciar", "jugadoras": lista})

    else:
        print(f"[?] Mensaje desconocido: {datos}")

# ---------- fin de lo nuevo ----------


def atender_cliente(conexion, direccion):
    """Cada jugadora se atiende en su propio hilo."""
    global partida_iniciada
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
                procesar_mensaje(conexion, datos)   # NUEVO
    except OSError:
        pass
    finally:
        with candado:
            if conexion in clientes:
                clientes.remove(conexion)
            salio = jugadoras.pop(conexion, None)   # NUEVO
            total = len(clientes)
            if total == 0:
                partida_iniciada = False            # NUEVO: si se van todas, se puede empezar otra
        print(f"[-] Se desconectó {direccion[0]}")
        enviar_a_todos({"tipo": "info", "mensaje": f"Jugadoras conectadas: {total}"})
        if salio:
            avisar_sala()                           # NUEVO


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