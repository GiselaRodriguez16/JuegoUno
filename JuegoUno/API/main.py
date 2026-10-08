from fastapi import FastAPI, Depends
from sqlalchemy.orm import Session

from database import SessionLocal
from models import Jugador, Partida, LogJuego, HistorialPartida
from schemas import (
    JugadorCrear,
    JugadorRespuesta,
    PartidaRespuesta,
    LogCrear,
    HistorialCrear
)

app = FastAPI()


def obtener_db():
    db = SessionLocal()

    try:
        yield db
    finally:
        db.close()


@app.get("/")
def inicio():
    return {"mensaje": "API de Juego UNO funcionando"}


@app.post("/jugadores", response_model=JugadorRespuesta)
def crear_jugador(
    jugador: JugadorCrear,
    db: Session = Depends(obtener_db)
):
    jugador_existente = (
        db.query(Jugador)
        .filter(Jugador.nombre == jugador.nombre)
        .first()
    )

    if jugador_existente:
        return jugador_existente

    nuevo_jugador = Jugador(nombre=jugador.nombre)

    db.add(nuevo_jugador)
    db.commit()
    db.refresh(nuevo_jugador)

    return nuevo_jugador

@app.post("/partidas", response_model=PartidaRespuesta)
def crear_partida(
    db: Session = Depends(obtener_db)
):
    nueva_partida = Partida()

    db.add(nueva_partida)
    db.commit()
    db.refresh(nueva_partida)

    return nueva_partida

@app.post("/log")
def crear_log(
    log: LogCrear,
    db: Session = Depends(obtener_db)
):
    nuevo_log = LogJuego(
        id_partida=log.id_partida,
        id_jugador=log.id_jugador,
        movimiento=log.movimiento
    )

    db.add(nuevo_log)
    db.commit()
    db.refresh(nuevo_log)

    return {
        "mensaje": "Movimiento registrado",
        "id": nuevo_log.id
    }

@app.post("/historial")
def crear_historial(
    historial: HistorialCrear,
    db: Session = Depends(obtener_db)
):
    nuevo_historial = HistorialPartida(
        id_partida=historial.id_partida,
        id_jugador=historial.id_jugador,
        resultado=historial.resultado
    )

    db.add(nuevo_historial)
    db.commit()
    db.refresh(nuevo_historial)

    return {
        "mensaje": "Historial guardado",
        "id": nuevo_historial.id
    }