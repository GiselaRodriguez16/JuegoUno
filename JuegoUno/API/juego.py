import random

COLORES = ["Rojo", "Amarillo", "Verde", "Azul"]


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


def texto_carta(carta):
    """Texto legible de una carta, para el log. Ej: 'Rojo 5', 'Azul Salta'."""
    if carta["tipo"] == "Numero":
        return f'{carta["color"]} {carta["numero"]}'
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
        self.logs_pendientes = []                        # NUEVO (#5): (id_jugadora, texto) para LogJuego
        self.repartir()

    # ---------- Log ----------

    def anotar(self, id_jugador, texto):               # NUEVO (#5)
        """Guarda un movimiento para que el servidor lo mande a LogJuego."""
        self.logs_pendientes.append((id_jugador, texto))

    def sacar_logs(self):                              # NUEVO (#5)
        """Entrega los movimientos pendientes y vacía la lista."""
        logs = self.logs_pendientes
        self.logs_pendientes = []
        return logs

    # ---------- Mazo ----------

    def robar(self):
        if not self.mazo:
            raise RuntimeError("El mazo está vacío")     # Rebarajar se programa en el commit #6
        return self.mazo.pop()

    def robar_varias(self, id_jugador, cantidad):      # NUEVO (#5)
        """Le da 'cantidad' cartas a una jugadora (o las que queden si se acaba el mazo)."""
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

    # ---------- Jugadas ----------

    def tirar(self, id_jugador, indice):
        """Intenta tirar una carta. Regresa None si salió bien, o un texto con el error."""
        if id_jugador != self.id_turno():
            return "No es tu turno."

        mano = self.manos[id_jugador]
        if not (0 <= indice < len(mano)):
            return "Esa carta no existe."

        carta = mano[indice]

        if carta["color"] == "Ninguno":
            return "Los comodines todavía no están disponibles."   # Se activan en el commit #6

        if not es_valida(carta, self.carta_actual):
            return f"No puedes tirar {texto_carta(carta)} sobre {texto_carta(self.carta_actual)}."

        mano.pop(indice)
        self.descarte.append(carta)
        self.ultima_accion = f"{self.nombre_de(id_jugador)} tiró {texto_carta(carta)}"
        self.anotar(id_jugador, f"Tiró {texto_carta(carta)}")       # NUEVO (#5)

        self.aplicar_efecto(id_jugador, carta)                       # NUEVO (#5)
        return None

    def aplicar_efecto(self, id_jugador, carta):       # NUEVO (#5)
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

        else:
            # Carta de número: turno normal
            self.siguiente_turno()

    def robar_carta(self, id_jugador):                 # NUEVO (#5)
        """La jugadora en turno roba una carta y el turno pasa a la siguiente."""
        if id_jugador != self.id_turno():
            return "No es tu turno."

        if self.robar_varias(id_jugador, 1) == 0:
            return "Ya no hay cartas en el mazo."

        self.ultima_accion = f"{self.nombre_de(id_jugador)} robó una carta"
        self.anotar(id_jugador, "Robó una carta")
        self.siguiente_turno()
        return None

    # ---------- Lo que ve cada jugadora ----------

    def estado_para(self, id_jugador):
        """Lo que ve UNA jugadora: su mano completa y de las demás solo cuántas cartas tienen."""
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
            "cartasEnMazo": len(self.mazo),              # NUEVO (#5)
        }