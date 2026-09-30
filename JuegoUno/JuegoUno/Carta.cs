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
            // Los comodines siempre se pueden tirar
            if (Color == "Ninguno")
                return true;

            // Coincide el color
            if (Color == cartaActual.Color)
                return true;

            // Coincide el número 
            if (Tipo == "Numero" && cartaActual.Tipo == "Numero" && Numero == cartaActual.Numero)
                return true;

            // Coincide el tipo especial 
            if (Tipo != "Numero" && Tipo == cartaActual.Tipo)
                return true;

            return false;
        }
    }
}
