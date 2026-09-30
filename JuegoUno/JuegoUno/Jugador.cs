using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuegoUno
{
    public class Jugador
    {
        public int Id { get; set; }             
        public string Nombre { get; set; }
        public List<Carta> Mano { get; set; } = new List<Carta>();

        public void MostrarMano()
        {
            Console.WriteLine($"Cartas de {Nombre}:");
            for (int i = 0; i < Mano.Count; i++)
            {
                Console.WriteLine($"  [{i}] {Mano[i]}");
            }
        }
    }
}
