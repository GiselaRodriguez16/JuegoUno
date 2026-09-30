using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuegoUno
{
    public class Carta
    {
        public string Color { get; set; }      // "Rojo", "Amarillo", "Verde", "Azul", o "Ninguno" (comodines)
        public string Tipo { get; set; }       // "Numero", "Salta", "Reversa", "Mas2", "ComodÃ­n", "Mas4"
        public int? Numero { get; set; }       // Solo aplica si Tipo == "Numero" (0-9)

        public override string ToString()
        {
            if (Tipo == "Numero")
                return $"{Color} {Numero}";
            else
                return $"{Color} {Tipo}";
        }
    }
}
