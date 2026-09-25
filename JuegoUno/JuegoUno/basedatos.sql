CREATE DATABASE IF NOT EXISTS juego_uno;
USE juego_uno;

CREATE TABLE Jugadores (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL
);

CREATE TABLE Partidas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    fecha DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE HistorialPartidas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_partida INT NOT NULL,
    id_jugador INT NOT NULL,
    resultado ENUM('Ganada', 'Perdida') NOT NULL,
    FOREIGN KEY (id_partida) REFERENCES Partidas(id),
    FOREIGN KEY (id_jugador) REFERENCES Jugadores(id)
);

CREATE TABLE LogJuego (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_partida INT NOT NULL,
    id_jugador INT NOT NULL,
    movimiento VARCHAR(255) NOT NULL,
    fecha_hora DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_partida) REFERENCES Partidas(id),
    FOREIGN KEY (id_jugador) REFERENCES Jugadores(id)
);