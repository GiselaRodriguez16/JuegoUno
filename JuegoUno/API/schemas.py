from pydantic import BaseModel
from datetime import datetime

class JugadorCrear(BaseModel):
    nombre: str


class JugadorRespuesta(BaseModel):
    id: int
    nombre: str

    class Config:
        from_attributes = True

class PartidaRespuesta(BaseModel):
    id: int
    fecha: datetime

    class Config:
        from_attributes = True

class LogCrear(BaseModel):
    id_partida: int
    id_jugador: int
    movimiento: str

class HistorialCrear(BaseModel):
    id_partida: int
    id_jugador: int
    resultado: str