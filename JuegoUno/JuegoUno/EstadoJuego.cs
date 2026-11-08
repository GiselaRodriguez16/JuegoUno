using System.Collections.Generic;
using System.Text.Json;

namespace JuegoUno
{
    // Una jugadora vista desde la mesa: de las demás solo se sabe cuántas cartas tienen
    public class JugadoraEstado
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int NumCartas { get; set; }
    }

    // Todo lo que el servidor le manda a ESTA laptop
    public class EstadoJuego
    {
        public int MiId { get; set; }
        public List<Carta> MiMano { get; set; } = new List<Carta>();
        public Carta CartaActual { get; set; }
        public List<JugadoraEstado> Jugadoras { get; set; } = new List<JugadoraEstado>();
        public int Turno { get; set; }        // Id de la jugadora en turno
        public int Direccion { get; set; }
        public string UltimaAccion { get; set; }   // Ej: "Gis tiró Rojo 5"
        public int CartasEnMazo { get; set; }
        public int? UnoPendiente { get; set; }   // Id de quien debe cantar UNO, o null
        public int UnoSegundos { get; set; }      // Segundos que le quedan


        private static readonly JsonSerializerOptions opciones = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true   // "miMano" del servidor = MiMano aquí
        };

        public static EstadoJuego DesdeJson(JsonElement mensaje, int miId)
        {
            EstadoJuego estado = mensaje.Deserialize<EstadoJuego>(opciones);
            estado.MiId = miId;
            return estado;
        }
    }
}