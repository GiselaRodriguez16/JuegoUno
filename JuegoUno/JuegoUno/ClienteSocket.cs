using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JuegoUno
{
    public class ClienteSocket
    {
        private TcpClient cliente;
        private StreamReader lector;
        private StreamWriter escritor;

        public bool Conectado => cliente != null && cliente.Connected;

        // Se dispara cada vez que llega un mensaje del servidor (desde un hilo secundario)
        public event Action<JsonElement> MensajeRecibido;
        public event Action Desconectado;

        public async Task ConectarAsync(string ip, int puerto)
        {
            cliente = new TcpClient();

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await cliente.ConnectAsync(ip, puerto, cts.Token);
            }

            NetworkStream stream = cliente.GetStream();
            lector = new StreamReader(stream, Encoding.UTF8);
            escritor = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            // Escucha mensajes del servidor en segundo plano
            _ = Task.Run(EscucharAsync);
        }

        private async Task EscucharAsync()
        {
            try
            {
                string linea;
                while ((linea = await lector.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(linea);
                        MensajeRecibido?.Invoke(doc.RootElement.Clone());
                    }
                    catch (JsonException)
                    {
                        // Ignora mensajes que no sean JSON válido
                    }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }

            Desconectado?.Invoke();
        }

        public async Task EnviarAsync(object datos)
        {
            string json = JsonSerializer.Serialize(datos);
            await escritor.WriteLineAsync(json);
        }

        public void Cerrar()
        {
            cliente?.Close();
        }
    }
}