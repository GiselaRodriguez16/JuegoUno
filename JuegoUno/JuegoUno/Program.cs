using MySqlConnector;
using System.Collections.Generic;

string connectionString = File.ReadAllText("config.txt").Trim();

using var connection = new MySqlConnection(connectionString);

try
{
    connection.Open();
    Console.WriteLine("¡Conexión exitosa a juego_uno! 🎉\n");

    //Numero de jugadores
    Console.Write("Numero de jugadores: ");
    int numJugadores = int.Parse(Console.ReadLine());

    List<int> idsJugadores = new List<int>();

    for(int i=1; i<=numJugadores; i++)
    {
        Console.Write($"Nombre del jugador {i}: ");
        string nombreJugador = Console.ReadLine();

        //Si el jugador existe
        int idJugador;
        string checkQuery = "SELECT id FROM Jugadores WHERE nombre = @nombre";
        using (var checkCmd = new MySqlCommand(checkQuery, connection))
        {
            checkCmd.Parameters.AddWithValue("@nombre", nombreJugador);
            object resultado = checkCmd.ExecuteScalar();

            if(resultado!=null)
            {
                //Si ya existe tomamos su id
                idJugador = Convert.ToInt32(resultado);
                Console.WriteLine($"  Jugador '{nombreJugador}' ya existía (ID: {idJugador}).\n");
            }
            else
            {
                // Si no existe lo insertamos
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

                Console.WriteLine($"  Jugador '{nombreJugador}' agregado (ID: {idJugador}).\n");
            }
        }

        idsJugadores.Add(idJugador);
    }

    // Registrar una partida nueva
    string insertPartida = "INSERT INTO Partidas (fecha) VALUES (NOW())";
    using (var cmdPartida = new MySqlCommand(insertPartida, connection))
    {
        cmdPartida.ExecuteNonQuery();
    }

    // Obtener el id de esa partida recién creada

    ulong idPartida;
    using (var cmdId = new MySqlCommand("SELECT LAST_INSERT_ID()", connection))
    {
        idPartida = (ulong)cmdId.ExecuteScalar();
    }

    // Registrar el resultado de un jugador en esa partida
    string insertHistorial = "INSERT INTO HistorialPartidas (id_partida, id_jugador, resultado) VALUES (@partida, @jugador, @resultado)";
    using (var cmdHistorial = new MySqlCommand(insertHistorial, connection))
    {
        cmdHistorial.Parameters.AddWithValue("@partida", idPartida);
        cmdHistorial.Parameters.AddWithValue("@jugador", 1); // el id del jugador, ej. 1
        cmdHistorial.Parameters.AddWithValue("@resultado", "Ganada");
        cmdHistorial.ExecuteNonQuery();
        Console.WriteLine($"Partida #{idPartida} registrada con resultado 'Ganada'.\n");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}

Console.WriteLine("\nPresiona una tecla para salir...");
Console.ReadKey();