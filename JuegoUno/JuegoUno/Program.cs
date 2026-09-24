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
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}

Console.WriteLine("\nPresiona una tecla para salir...");
Console.ReadKey();