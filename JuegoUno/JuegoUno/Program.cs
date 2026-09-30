using JuegoUno;
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
    List<string> nombresJugadores = new List<string>();

    for (int i=1; i<=numJugadores; i++)
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
        nombresJugadores.Add(nombreJugador);
    }

    // Crear y barajar el mazo
    Mazo mazo = new Mazo();
    mazo.Barajar();

    // Crear la lista de jugadores (con sus cartas)
    List<Jugador> jugadores = new List<Jugador>();
    for (int i = 0; i < idsJugadores.Count; i++)
    {
        Jugador j = new Jugador
        {
            Id = idsJugadores[i],
            Nombre = nombresJugadores[i]
        };

        // Repartir 7 cartas a cada jugador (regla oficial del UNO)
        for (int c = 0; c < 7; c++)
        {
            j.Mano.Add(mazo.RobarCarta());
        }

        jugadores.Add(j);
    }

    // Sacar la primera carta para iniciar la pila de descarte
    Carta cartaActual = mazo.RobarCarta();
    Console.WriteLine($"\nCarta inicial: {cartaActual}\n");

    // Mostrar la mano de cada jugador (para probar que funciona)
    foreach (var j in jugadores)
    {
        j.MostrarMano();
        Console.WriteLine();
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