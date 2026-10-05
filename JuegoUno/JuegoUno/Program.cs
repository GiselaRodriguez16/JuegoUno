using JuegoUno;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();

string connectionString = File.ReadAllText("config.txt").Trim();
using var connection = new MySqlConnection(connectionString);

try
{
    connection.Open();

    FormSetup setup = new FormSetup();
    if (setup.ShowDialog() != DialogResult.OK)
    {
        return;
    }

    List<int> idsJugadores = new List<int>();
    List<string> nombresJugadores = setup.NombresJugadores;

    foreach (string nombreJugador in nombresJugadores)
    {
        int idJugador;
        string checkQuery = "SELECT id FROM Jugadores WHERE nombre = @nombre";
        using (var checkCmd = new MySqlCommand(checkQuery, connection))
        {
            checkCmd.Parameters.AddWithValue("@nombre", nombreJugador);
            object resultado = checkCmd.ExecuteScalar();

            if (resultado != null)
            {
                idJugador = Convert.ToInt32(resultado);
            }
            else
            {
                string insertQuery = "INSERT INTO Jugadores (nombre) VALUES (@nombre)";
                using (var insertCmd = new MySqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@nombre", nombreJugador);
                    insertCmd.ExecuteNonQuery();
                }
                using (var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", connection))
                {
                    idJugador = Convert.ToInt32(idCmd.ExecuteScalar());
                }
            }
        }
        idsJugadores.Add(idJugador);
    }

    Mazo mazo = new Mazo();
    mazo.Barajar();

    List<Jugador> jugadores = new List<Jugador>();
    for (int i = 0; i < idsJugadores.Count; i++)
    {
        Jugador j = new Jugador { Id = idsJugadores[i], Nombre = nombresJugadores[i] };
        for (int c = 0; c < 7; c++) j.Mano.Add(mazo.RobarCarta());
        jugadores.Add(j);
    }

    Carta cartaActual = mazo.RobarCarta();
    if (cartaActual.Color == "Ninguno")
    {
        using (var colorForm = new FormColorPicker())
        {
            colorForm.ShowDialog();
            cartaActual.Color = colorForm.ColorElegido ?? "Rojo";
        }
    }

    string insertPartida = "INSERT INTO Partidas (fecha) VALUES (NOW())";
    using (var cmdPartida = new MySqlCommand(insertPartida, connection))
    {
        cmdPartida.ExecuteNonQuery();
    }
    ulong idPartida;
    using (var cmdId = new MySqlCommand("SELECT LAST_INSERT_ID()", connection))
    {
        idPartida = (ulong)cmdId.ExecuteScalar();
    }

    int turnoActual = 0;
    int direccion = 1;

    void RegistrarLog(int idJugadorLog, string movimiento)
    {
        string logQuery = "INSERT INTO LogJuego (id_partida, id_jugador, movimiento) VALUES (@partida, @jugador, @movimiento)";
        using (var logCmd = new MySqlCommand(logQuery, connection))
        {
            logCmd.Parameters.AddWithValue("@partida", idPartida);
            logCmd.Parameters.AddWithValue("@jugador", idJugadorLog);
            logCmd.Parameters.AddWithValue("@movimiento", movimiento);
            logCmd.ExecuteNonQuery();
        }
    }

    void GuardarHistorial(Jugador ganador)
    {
        foreach (var j in jugadores)
        {
            string resultado = (j.Id == ganador.Id) ? "Ganada" : "Perdida";
            string insertHistorial = "INSERT INTO HistorialPartidas (id_partida, id_jugador, resultado) VALUES (@partida, @jugador, @resultado)";
            using (var cmdHistorial = new MySqlCommand(insertHistorial, connection))
            {
                cmdHistorial.Parameters.AddWithValue("@partida", idPartida);
                cmdHistorial.Parameters.AddWithValue("@jugador", j.Id);
                cmdHistorial.Parameters.AddWithValue("@resultado", resultado);
                cmdHistorial.ExecuteNonQuery();
            }
        }
    }

    FormJuego formJuego = new FormJuego();

    formJuego.CartaClickeada += (indice) =>
    {
        Jugador jugadorEnTurno = jugadores[turnoActual];

        if (indice < 0 || indice >= jugadorEnTurno.Mano.Count) return;

        Carta cartaElegida = jugadorEnTurno.Mano[indice];

        if (!cartaElegida.EsValidaSobre(cartaActual))
        {
            formJuego.MostrarMensaje("❌ Esa carta no es válida.");
            return;
        }

        jugadorEnTurno.Mano.RemoveAt(indice);

        if (cartaElegida.Color == "Ninguno")
        {
            using (var colorForm = new FormColorPicker())
            {
                colorForm.ShowDialog();
                cartaElegida.Color = colorForm.ColorElegido ?? "Rojo";
            }
            formJuego.MostrarUltimaAccion($"{jugadorEnTurno.Nombre} eligió el color {cartaElegida.Color}");
            formJuego.Refresh();
        }

        cartaActual = cartaElegida;
        RegistrarLog(jugadorEnTurno.Id, $"Tiró {cartaElegida}");

        if (jugadorEnTurno.Mano.Count == 0)
        {
            RegistrarLog(jugadorEnTurno.Id, "Ganó la partida");
            GuardarHistorial(jugadorEnTurno);
            formJuego.MostrarGanador(jugadorEnTurno.Nombre);
            formJuego.Close();
            return;
        }

        int siguienteTurno = (turnoActual + direccion + jugadores.Count) % jugadores.Count;

        switch (cartaElegida.Tipo)
        {
            case "Salta":
                siguienteTurno = (siguienteTurno + direccion + jugadores.Count) % jugadores.Count;
                break;

            case "Reversa":
                direccion *= -1;
                siguienteTurno = (turnoActual + direccion + jugadores.Count) % jugadores.Count;
                break;

            case "Mas2":
                Jugador victima2 = jugadores[siguienteTurno];
                for (int k = 0; k < 2; k++) victima2.Mano.Add(mazo.RobarCarta());
                RegistrarLog(victima2.Id, "Robó 2 cartas (por +2)");
                siguienteTurno = (siguienteTurno + direccion + jugadores.Count) % jugadores.Count;
                break;

            case "Mas4":
                Jugador victima4 = jugadores[siguienteTurno];
                for (int k = 0; k < 4; k++) victima4.Mano.Add(mazo.RobarCarta());
                RegistrarLog(victima4.Id, "Robó 4 cartas (por +4)");
                siguienteTurno = (siguienteTurno + direccion + jugadores.Count) % jugadores.Count;
                break;
        }

        turnoActual = siguienteTurno;
        formJuego.ActualizarPantalla(jugadores, turnoActual, cartaActual);
    };

    formJuego.RobarClickeado += () =>
    {
        Jugador jugadorEnTurno = jugadores[turnoActual];
        Carta nueva = mazo.RobarCarta();
        jugadorEnTurno.Mano.Add(nueva);
        RegistrarLog(jugadorEnTurno.Id, "Robó una carta");

        turnoActual = (turnoActual + direccion + jugadores.Count) % jugadores.Count;
        formJuego.ActualizarPantalla(jugadores, turnoActual, cartaActual);
    };

    formJuego.ActualizarPantalla(jugadores, turnoActual, cartaActual);
    Application.Run(formJuego);
}
catch (Exception ex)
{
    MessageBox.Show("Error: " + ex.Message);
}

