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
        public string Tipo { get; set; }       // "Numero", "Salta", "Reversa", "Mas2", "Comodin", "Mas4"
        public int? Numero { get; set; }       // Solo aplica si Tipo == "Numero" (0-9)

        public override string ToString()
        {
            if (Tipo == "Numero")
                return $"{Color} {Numero}";
            else
                return $"{Color} {Tipo}";
        }

        public bool EsValidaSobre(Carta cartaActual)
        {
            if (Color == "Ninguno")
                return true;

            if (Color == cartaActual.Color)
                return true;

            if (Tipo == "Numero" && cartaActual.Tipo == "Numero" && Numero == cartaActual.Numero)
                return true;

            if (Tipo != "Numero" && Tipo == cartaActual.Tipo)
                return true;

            return false;
        }

        public string NombreImagen()
        {
            if (Tipo == "Numero")
                return $"{Color}_{Numero}.png";
            else if (Tipo == "Comodin" || Tipo == "Mas4")
                return $"Ninguno_{Tipo}.png"; 
            else
                return $"{Color}_{Tipo}.png";
        }
    }
}
