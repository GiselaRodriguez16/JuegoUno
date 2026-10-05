using System.Collections.Generic;
using System.Drawing;

public static class ColoresJuego
{
    public static readonly Dictionary<string, Color> Cartas = new Dictionary<string, Color>
    {
        { "Rojo", ColorTranslator.FromHtml("#D91A4D") },
        { "Amarillo", ColorTranslator.FromHtml("#F2DA63") },
        { "Verde", ColorTranslator.FromHtml("#80F2B2") },
        { "Azul", ColorTranslator.FromHtml("#99AFF2") },
        { "Ninguno", Color.Black }
    };

    public static readonly Color FondoVentana = ColorTranslator.FromHtml("#F2006B");
    public static readonly Color FondoPanel = ColorTranslator.FromHtml("#26FA56");
}
