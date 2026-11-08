import random
import time                                            # NUEVO (#7)
import math                                            # NUEVO (#7)

COLORES = ["Rojo", "Amarillo", "Verde", "Azul"]
SEGUNDOS_UNO = 10                                      # NUEVO (#7): tiempo para cantar UNO
PENALIZACION_UNO = 2                                   # NUEVO (#7): cartas de castigo si no lo canta


def crear_mazo():
    """Crea las 108 cartas del UNO y las baraja."""
    mazo = []
    for color in COLORES:
        # Un solo 0 por color
        mazo.append({"color": color, "tipo": "Numero", "numero": 0})

        # Dos de cada número del 1 al 9
        for n in range(1, 10):
            for _ in range(2):
                mazo.append({"color": color, "tipo": "Numero", "numero": n})

        # Dos de cada carta especial por color
        for _ in range(2):
            for tipo in ("Salta", "Reversa", "Mas2"):
                mazo.append({"color": color, "tipo": tipo, "numero": None})

    # Comodines sin color, 4 de cada tipo
    for _ in range(4):
        mazo.append({"color": "Ninguno", "tipo": "Comodin", "numero": None})
        mazo.append({"color": "Ninguno", "tipo": "Mas4", "numero": None})

    random.shuffle(mazo)
    return mazo


def es_comodin(carta):
    return carta["tipo"] in ("Comodin", "Mas4")


def texto_carta(carta):
    """Texto legible de una carta, para el log. Ej: 'Rojo 5', 'Azul Salta', 'Comodín (Verde)'."""
    if carta["tipo"] == "Numero":
        return f'{carta["color"]} {carta["numero"]}'
    if es_comodin(carta):
        nombre = "Comodín" if carta["tipo"] == "Comodin" else "+4"
        if carta["color"] in COLORES:
            return f'{nombre} ({carta["color"]})'
        return nombre
    return f'{carta["color"]} {carta["tipo"]}'


def es_valida(carta, actual):
    """Las mismas reglas que Carta.EsValidaSobre en C#."""
    if carta["color"] == "Ninguno":                    # Comodines siempre se pueden tirar
        return True
    if carta["color"] == actual["color"]:              # Mismo color
        return True
    if carta["tipo"] == "Numero" and actual["tipo"] == "Numero" and carta["numero"] == actual["numero"]:
        return True                                    # Mismo número
    if carta["tipo"] != "Numero" and carta["tipo"] == actual["tipo"]:
        return True                                    # Misma carta especial (ej. Salta sobre Salta)
    return False


class Partida:
    """Todo el estado de una partida. Solo el servidor lo modifica."""

    def __init__(self, id_partida, jugadoras):
        # jugadoras: lista de {"id": ..., "nombre": ...} en el orden en que se unieron
        self.id = id_partida
        self.jugadoras = jugadoras
        self.mazo = crear_mazo()
        self.descarte = []                               # Cartas tiradas; la última es la del centro
        self.manos = {j["id"]: [] for j in jugadoras}    # id de jugadora -> lista de cartas
        self.turno = 0                                   # Posición en self.jugadoras
        self.direccion = 1                               # 1 = normal, -1 = reversa
        self.ultima_accion = "¡Empieza la partida!"      # Se muestra a todas
        self.logs_pendientes = []                        # (id_jugadora, texto) para LogJuego
        self.hubo_rebarajo = False

        # NUEVO (#7): control del UNO
        self.uno_pendiente = None                        # Id de quien debe cantar UNO (None = nadie)
        self.uno_limite = 0.0                            # Momento en que se acaba el tiempo
        self.uno_token = 0                               # Número que identifica cada temporizador
        self.uno_nuevo = False                           # True cuando hay que arrancar un temporizador

        self.repartir()

    # ---------- Log ----------

    def anotar(self, id_jugador, texto):
        """Guarda un movimiento para que el servidor lo mande a LogJuego."""
        self.logs_pendientes.append((id_jugador, texto))

    def sacar_logs(self):
        """Entrega los movimientos pendientes y vacía la lista."""
        logs = self.logs_pendientes
        self.logs_pendientes = []
        return logs

    # ---------- Mazo ----------

    def rebarajar(self):
        """Cuando se acaba el mazo: las cartas tiradas (menos la del centro) se barajan y vuelven al mazo."""
        if len(self.descarte) <= 1:
            return False

        centro = self.descarte.pop()
        for carta in self.descarte:
            if es_comodin(carta):
                carta["color"] = "Ninguno"               # Los comodines regresan sin color

        self.mazo = self.descarte
        random.shuffle(self.mazo)
        self.descarte = [centro]
        self.hubo_rebarajo = True
        print(f"[juego] Se rebarajaron {len(self.mazo)} cartas")
        return True

    def robar(self):
        if not self.mazo and not self.rebarajar():
            raise RuntimeError("El mazo está vacío")
        return self.mazo.pop()

    def robar_varias(self, id_jugador, cantidad):
        """Le da 'cantidad' cartas a una jugadora (o las que queden si se acaba todo)."""
        robadas = 0
        for _ in range(cantidad):
            try:
                self.manos[id_jugador].append(self.robar())
                robadas += 1
            except RuntimeError:
                break
        return robadas

    def repartir(self):
        # 7 cartas a cada una, una por una como en el juego real
        for _ in range(7):
            for j in self.jugadoras:
                self.manos[j["id"]].append(self.robar())

        # La carta inicial debe ser un número; si sale especial, se regresa al fondo del mazo
        carta = self.robar()
        while carta["tipo"] != "Numero":
            self.mazo.insert(0, carta)
            carta = self.robar()
        self.descarte.append(carta)

    def avisar_rebarajo(self):
        """Si en esta jugada se rebarajó, lo agrega al mensaje que ven todas."""
        if self.hubo_rebarajo:
            self.ultima_accion += " (se rebarajó el mazo)"
            self.hubo_rebarajo = False

    # ---------- Turnos ----------

    @property
    def carta_actual(self):
        return self.descarte[-1]

    def id_turno(self):
        return self.jugadoras[self.turno]["id"]

    def nombre_de(self, id_jugador):
        for j in self.jugadoras:
            if j["id"] == id_jugador:
                return j["nombre"]
        return "?"

    def siguiente_turno(self):
        """Pasa el turno a la siguiente jugadora según la dirección."""
        self.turno = (self.turno + self.direccion) % len(self.jugadoras)

    # ---------- UNO ----------                       # NUEVO (#7)

    def esperando_uno(self):
        """Si alguien debe cantar UNO, regresa el texto de error para bloquear jugadas."""
        if self.uno_pendiente is not None:
            return f"Esperando a que {self.nombre_de(self.uno_pendiente)} cante UNO..."
        return None

    def iniciar_uno(self, id_jugador):
        """La jugadora se quedó con 1 carta: empieza su tiempo para cantar UNO."""
        self.uno_pendiente = id_jugador
        self.uno_limite = time.monotonic() + SEGUNDOS_UNO
        self.uno_token += 1
        self.uno_nuevo = True
        self.ultima_accion += f" → ¡a {self.nombre_de(id_jugador)} le queda 1 carta!"

    def tomar_uno_nuevo(self):
        """El servidor la llama después de cada jugada: si hay que arrancar temporizador, regresa su número."""
        if self.uno_nuevo:
            self.uno_nuevo = False
            return self.uno_token
        return None

    def cantar_uno(self, id_jugador):
        """La jugadora presionó el botón UNO."""
        if self.uno_pendiente != id_jugador:
            return "No tienes que cantar UNO ahora."

        self.uno_pendiente = None
        self.ultima_accion = f"¡{self.nombre_de(id_jugador)} cantó UNO!"
        self.anotar(id_jugador, "Cantó UNO")
        return None

    def vencer_uno(self, token):
        """Se acabó el tiempo. Si todavía no cantó UNO, roba cartas de castigo.
        Regresa True si hubo castigo."""
        if self.uno_pendiente is None or token != self.uno_token:
            return False                                 # Ya cantó, o es un temporizador viejo

        id_jugador = self.uno_pendiente
        self.uno_pendiente = None
        robadas = self.robar_varias(id_jugador, PENALIZACION_UNO)
        self.ultima_accion = f"{self.nombre_de(id_jugador)} no cantó UNO → roba {robadas} de castigo"
        self.anotar(id_jugador, f"No cantó UNO: robó {robadas} cartas de penalización")
        self.avisar_rebarajo()
        return True

    # ---------- Jugadas ----------

    def tirar(self, id_jugador, indice, color=None):
        """Intenta tirar una carta. Regresa None si salió bien, o un texto con el error."""
        espera = self.esperando_uno()                    # NUEVO (#7)
        if espera:
            return espera

        if id_jugador != self.id_turno():
            return "No es tu turno."

        mano = self.manos[id_jugador]
        if not (0 <= indice < len(mano)):
            return "Esa carta no existe."

        carta = mano[indice]

        if not es_valida(carta, self.carta_actual):
            return f"No puedes tirar {texto_carta(carta)} sobre {texto_carta(self.carta_actual)}."

        # Los comodines necesitan un color válido
        if es_comodin(carta) and color not in COLORES:
            return "Elige un color para el comodín."

        mano.pop(indice)
        if es_comodin(carta):
            carta["color"] = color                       # El comodín toma el color elegido
        self.descarte.append(carta)

        self.ultima_accion = f"{self.nombre_de(id_jugador)} tiró {texto_carta(carta)}"
        self.anotar(id_jugador, f"Tiró {texto_carta(carta)}")

        self.aplicar_efecto(id_jugador, carta)
        self.avisar_rebarajo()

        if len(mano) == 1:                               # NUEVO (#7): le queda una carta
            self.iniciar_uno(id_jugador)

        return None

    def aplicar_efecto(self, id_jugador, carta):
        """Aplica el efecto de la carta tirada y decide de quién es el siguiente turno."""
        tipo = carta["tipo"]

        if tipo == "Salta":
            self.siguiente_turno()                       # La siguiente...
            saltada = self.id_turno()
            self.siguiente_turno()                       # ...se brinca
            self.ultima_accion += f" → {self.nombre_de(saltada)} pierde su turno"
            self.anotar(saltada, "Perdió su turno (por Salta)")

        elif tipo == "Reversa":
            self.direccion *= -1
            self.anotar(id_jugador, "Cambió el sentido del juego")
            if len(self.jugadoras) == 2:
                # Con 2 jugadoras, Reversa funciona como Salta: vuelve a tirar la misma
                self.ultima_accion += " → cambia el sentido y vuelve a tirar"
            else:
                self.siguiente_turno()
                self.ultima_accion += " → cambia el sentido del juego"

        elif tipo == "Mas2":
            self.siguiente_turno()                       # La víctima es la siguiente
            victima = self.id_turno()
            robadas = self.robar_varias(victima, 2)
            self.siguiente_turno()                       # ...y pierde su turno
            self.ultima_accion += f" → {self.nombre_de(victima)} roba {robadas} y pierde su turno"
            self.anotar(victima, f"Robó {robadas} cartas (por +2)")

        elif tipo == "Mas4":
            self.siguiente_turno()
            victima = self.id_turno()
            robadas = self.robar_varias(victima, 4)
            self.siguiente_turno()
            self.ultima_accion += f" → {self.nombre_de(victima)} roba {robadas} y pierde su turno"
            self.anotar(victima, f"Robó {robadas} cartas (por +4)")

        else:
            # Carta de número o Comodín: turno normal
            self.siguiente_turno()

    def robar_carta(self, id_jugador):
        """La jugadora en turno roba una carta y el turno pasa a la siguiente."""
        espera = self.esperando_uno()                    # NUEVO (#7)
        if espera:
            return espera

        if id_jugador != self.id_turno():
            return "No es tu turno."

        if self.robar_varias(id_jugador, 1) == 0:
            return "Ya no hay cartas en el mazo."

        self.ultima_accion = f"{self.nombre_de(id_jugador)} robó una carta"
        self.anotar(id_jugador, "Robó una carta")
        self.siguiente_turno()
        self.avisar_rebarajo()
        return None

    # ---------- Lo que ve cada jugadora ----------

    def estado_para(self, id_jugador):
        """Lo que ve UNA jugadora: su mano completa y de las demás solo cuántas cartas tienen."""
        segundos_uno = 0                                 # NUEVO (#7)
        if self.uno_pendiente is not None:
            segundos_uno = max(0, math.ceil(self.uno_limite - time.monotonic()))

        return {
            "tipo": "estado",
            "miMano": self.manos.get(id_jugador, []),
            "cartaActual": self.carta_actual,
            "jugadoras": [
                {"id": j["id"], "nombre": j["nombre"], "numCartas": len(self.manos[j["id"]])}
                for j in self.jugadoras
            ],
            "turno": self.id_turno(),
            "direccion": self.direccion,
            "ultimaAccion": self.ultima_accion,
            "cartasEnMazo": len(self.mazo),
            "unoPendiente": self.uno_pendiente,          # NUEVO (#7): id o null
            "unoSegundos": segundos_uno,                 # NUEVO (#7)
        }