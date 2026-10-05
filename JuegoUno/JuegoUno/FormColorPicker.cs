using System;
using System.Windows.Forms;

public class FormColorPicker : Form
{
    public string ColorElegido { get; private set; }

    public FormColorPicker()
    {
        this.Text = "Elige un color";
        this.Width = 300;
        this.Height = 180;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = ColoresJuego.FondoVentana;

        string[] colores = { "Rojo", "Amarillo", "Verde", "Azul" };
        int x = 20;

        foreach (string color in colores)
        {
            Button btn = new Button();
            btn.Width = 60;
            btn.Height = 60;
            btn.Location = new System.Drawing.Point(x, 40);
            btn.BackColor = ColoresJuego.Cartas[color]; 
            btn.FlatStyle = FlatStyle.Flat;
            btn.Text = ""; 

            string colorCapturado = color; 
            btn.Click += (s, e) =>
            {
                ColorElegido = colorCapturado;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btn);
            x += 65;
        }
    }
}