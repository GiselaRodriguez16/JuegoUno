import socket
import threading
import json

HOST = "0.0.0.0"   # Acepta conexiones de cualquier computadora de la red
PUERTO = 5000

clientes = []                 # Sockets de las jugadoras conectadas
candado = threading.Lock()    # Evita que dos hilos modifiquen la lista al mismo tiempo


def enviar(conexion, datos):
    """Manda un mensaje JSON a una sola jugadora. Cada mensaje termina en salto de línea."""
    mensaje = json.dumps(datos) + "\n"
    conexion.sendall(mensaje.encode("utf-8"))


def enviar_a_todos(datos):
    """Manda el mismo mensaje a todas las jugadoras conectadas."""
    with candado:
        for c in list(clientes):
            try:
                enviar(c, datos)
            except OSError:
                pass


def atender_cliente(conexion, direccion):
    """Cada jugadora se atiende en su propio hilo."""
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
    except OSError:
        pass
    finally:
        with candado:
            if conexion in clientes:
                clientes.remove(conexion)
            total = len(clientes)
        print(f"[-] Se desconectó {direccion[0]}")
        enviar_a_todos({"tipo": "info", "mensaje": f"Jugadoras conectadas: {total}"})


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