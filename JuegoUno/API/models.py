from datetime import datetime
from sqlalchemy import Column, Integer, String, DateTime, ForeignKey, Enum
from database import Base


class Jugador(Base):
    __tablename__ = "Jugadores"

    id = Column(Integer, primary_key=True, autoincrement=True)
    nombre = Column(String(100), nullable=False)

class Partida(Base):
    __tablename__ = "Partidas"

    id = Column(Integer, primary_key=True, autoincrement=True)
    fecha = Column(DateTime, default=datetime.now, nullable=False)


class HistorialPartida(Base):
    __tablename__ = "HistorialPartidas"

    id = Column(Integer, primary_key=True, autoincrement=True)
    id_partida = Column(Integer, ForeignKey("Partidas.id"), nullable=False)
    id_jugador = Column(Integer, ForeignKey("Jugadores.id"), nullable=False)
    resultado = Column(
        Enum("Ganada", "Perdida"),
        nullable=False
    )

class LogJuego(Base):
    __tablename__ = "LogJuego" 
    id = Column(Integer, primary_key=True, autoincrement=True)
    id_partida = Column(Integer, ForeignKey("Partidas.id"), nullable=False)
    id_jugador = Column(Integer, ForeignKey("Jugadores.id"), nullable=False)
    movimiento = Column(String(255), nullable=False)
    fecha_hora = Column(DateTime)