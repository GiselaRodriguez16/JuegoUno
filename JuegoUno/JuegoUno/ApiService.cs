using System.Net.Http;
using System.Net.Http.Json;

namespace JuegoUno
{
    public class ApiService
    {
        private readonly HttpClient cliente;

        public ApiService()
        {
            cliente = new HttpClient();
            cliente.BaseAddress = new Uri("http://127.0.0.1:8000/");
        }

        public async Task<Jugador> CrearJugador(string nombre)
        {
            var datos = new
            {
                nombre = nombre
            };

            var respuesta = await cliente.PostAsJsonAsync("jugadores", datos);

            respuesta.EnsureSuccessStatusCode();

            return await respuesta.Content.ReadFromJsonAsync<Jugador>();
        }


        public async Task<int> CrearPartida()
        {
            var respuesta = await cliente.PostAsync("partidas", null);

            respuesta.EnsureSuccessStatusCode();

            var datos = await respuesta.Content.ReadFromJsonAsync<PartidaRespuesta>();

            return datos.Id;
        }


        public async Task RegistrarLog(
            int idPartida,
            int idJugador,
            string movimiento)
        {
            var datos = new
            {
                id_partida = idPartida,
                id_jugador = idJugador,
                movimiento = movimiento
            };

            var respuesta = await cliente.PostAsJsonAsync("log", datos);

            respuesta.EnsureSuccessStatusCode();
        }


        public async Task GuardarHistorial(
            int idPartida,
            int idJugador,
            string resultado)
        {
            var datos = new
            {
                id_partida = idPartida,
                id_jugador = idJugador,
                resultado = resultado
            };

            var respuesta = await cliente.PostAsJsonAsync("historial", datos);

            respuesta.EnsureSuccessStatusCode();
        }
    }


    public class PartidaRespuesta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
    }
}