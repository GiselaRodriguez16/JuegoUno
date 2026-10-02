using System;
using System.Drawing;
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
        this.BackColor = Color.DarkGreen;

        string[] colores = { "Rojo", "Rosa", "Azul", "Morado" };
        int x = 20;

        foreach (string color in colores)
        {
            Button btn = new Button();
            btn.Text = color;
            btn.Width = 60;
            btn.Height = 60;
            btn.Location = new Point(x, 40);
            btn.Click += (s, e) =>
            {
                ColorElegido = color;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btn);
            x += 65;
        }
    }
}