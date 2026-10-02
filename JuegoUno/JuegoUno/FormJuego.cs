using System;
using System.Drawing;
using System.Reflection.Emit;
using System.Windows.Forms;

public class FormJuego : Form
{
    private System.Windows.Forms.Label lblCartaActual;
    private System.Windows.Forms.Label lblTurno;
    private FlowLayoutPanel panelMano;
    private Button btnRobar;

    public FormJuego()
    {
        // Configuración básica de la ventana
        this.Text = "UNO - Juego";
        this.Width = 900;
        this.Height = 600;
        this.BackColor = Color.DarkGreen;

        // Label que muestra de quién es el turno
        lblTurno = new System.Windows.Forms.Label();
        lblTurno.Text = "Turno de: ...";
        lblTurno.ForeColor = Color.White;
        lblTurno.Font = new Font("Arial", 14, FontStyle.Bold);
        lblTurno.Location = new Point(20, 20);
        lblTurno.AutoSize = true;
        this.Controls.Add(lblTurno);

        // Label que muestra la carta actual en el centro
        lblCartaActual = new System.Windows.Forms.Label();
        lblCartaActual.Text = "Carta actual: ...";
        lblCartaActual.ForeColor = Color.White;
        lblCartaActual.Font = new Font("Arial", 16, FontStyle.Bold);
        lblCartaActual.Location = new Point(350, 200);
        lblCartaActual.AutoSize = true;
        this.Controls.Add(lblCartaActual);

        // Botón para robar carta
        btnRobar = new Button();
        btnRobar.Text = "Robar carta";
        btnRobar.Location = new Point(380, 260);
        btnRobar.Width = 120;
        this.Controls.Add(btnRobar);

        // Panel donde se van a mostrar las cartas de la mano (como botones)
        panelMano = new FlowLayoutPanel();
        panelMano.Location = new Point(20, 420);
        panelMano.Width = 840;
        panelMano.Height = 120;
        panelMano.BackColor = Color.ForestGreen;
        this.Controls.Add(panelMano);
    }
}