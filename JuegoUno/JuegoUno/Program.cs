using MySqlConnector;

string connectionString = File.ReadAllText("config.txt").Trim();

using var connection = new MySqlConnection(connectionString);

try
{
    connection.Open();
    Console.WriteLine("¡Conexión exitosa a juego_uno! 🎉\n");

    //Insertar un jugador de prueba
    string nombreJugador = "Ale";

    string insertQuery = "INSERT INTO Jugadores (nombre) VALUES (@nombre)";
    using (var insertCmd = new MySqlCommand(insertQuery, connection))
    {
        insertCmd.Parameters.AddWithValue("@nombre", nombreJugador);
        insertCmd.ExecuteNonQuery();
        Console.WriteLine($"Jugador '{nombreJugador}' agregado correctamente.\n");
    }

    //Leer los jugadores
    string selectQuery = "SELECT id, nombre FROM Jugadores";
    using (var selectCmd = new MySqlCommand(selectQuery, connection))
    using (var reader = selectCmd.ExecuteReader())
    {
        Console.WriteLine("Lista de jugadores registrados:");
        while (reader.Read())
        {
            int id = reader.GetInt32("id");
            string nombre = reader.GetString("nombre");
            Console.WriteLine($"  ID: {id} - Nombre: {nombre}");
        }
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