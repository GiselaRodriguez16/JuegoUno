using MySqlConnector;

string connectionString = File.ReadAllText("config.txt").Trim();

using var connection = new MySqlConnection(connectionString);

try
{
    connection.Open();
    Console.WriteLine("¡Conexión exitosa a juego_uno! 🎉");
}
catch (Exception ex)
{
    Console.WriteLine("Error al conectar: " + ex.Message);
}