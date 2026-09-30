using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuegoUno
{
    public class Mazo
    {
        public List<Carta> Cartas { get; set; } = new List<Carta>();

        public Mazo()
        {
            string[] colores = { "Rojo", "Amarillo", "Verde", "Azul" };

            foreach (string color in colores)
            {
                // Un solo 0 por color
                Cartas.Add(new Carta { Color = color, Tipo = "Numero", Numero = 0 });

                // Dos de cada número del 1 al 9
                for (int n = 1; n <= 9; n++)
                {
                    Cartas.Add(new Carta { Color = color, Tipo = "Numero", Numero = n });
                    Cartas.Add(new Carta { Color = color, Tipo = "Numero", Numero = n });
                }

                // Dos de cada carta especial por color
                for (int i = 0; i < 2; i++)
                {
                    Cartas.Add(new Carta { Color = color, Tipo = "Salta" });
                    Cartas.Add(new Carta { Color = color, Tipo = "Reversa" });
                    Cartas.Add(new Carta { Color = color, Tipo = "Mas2" });
                }
            }

            // Comodines (sin color, 4 de cada tipo)
            for (int i = 0; i < 4; i++)
            {
                Cartas.Add(new Carta { Color = "Ninguno", Tipo = "Comodin" });
                Cartas.Add(new Carta { Color = "Ninguno", Tipo = "Mas4" });
            }
        }

        public void Barajar()
        {
            Random rnd = new Random();
            Cartas = Cartas.OrderBy(c => rnd.Next()).ToList();
        }

        public Carta RobarCarta()
        {
            if (Cartas.Count == 0)
                throw new InvalidOperationException("El mazo está vacío");

            Carta carta = Cartas[0];
            Cartas.RemoveAt(0);
            return carta;
        }
    }
}
