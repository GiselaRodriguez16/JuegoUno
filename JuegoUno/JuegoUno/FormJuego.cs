using JuegoUno;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class FormJuego : Form
{
    private System.Windows.Forms.Label lblTurno;
    private FlowLayoutPanel panelOtrosJugadores;
    private Panel panelCartaActual;
    private FlowLayoutPanel panelMano;
    private Button btnRobar;

    // Eventos para que Program.cs reaccione a los clicks
    public event Action<int> CartaClickeada; // manda el índice de la carta en la mano
    public event Action RobarClickeado;

    private static readonly Dictionary<string, Color> ColoresCartas = new Dictionary<string, Color>
    {
        { "Rojo", ColorTranslator.FromHtml("#660C2C") },
        { "Amarillo", ColorTranslator.FromHtml("#FF98BB") },
        { "Verde", ColorTranslator.FromHtml("#7ABBEA") },
        { "Azul", ColorTranslator.FromHtml("#D78FCF") },
        { "Ninguno", Color.Black }
    };

    public FormJuego()
    {
        this.Text = "UNO - Juego";
        this.Width = 900;
        this.Height = 600;
        this.BackColor = Color.DarkGreen;

        lblTurno = new System.Windows.Forms.Label();
        lblTurno.Text = "Turno de: ...";
        lblTurno.ForeColor = Color.White;
        lblTurno.Font = new Font("Arial", 14, FontStyle.Bold);
        lblTurno.Location = new Point(20, 20);
        lblTurno.AutoSize = true;
        this.Controls.Add(lblTurno);

        panelOtrosJugadores = new FlowLayoutPanel();
        panelOtrosJugadores.Location = new Point(20, 60);
        panelOtrosJugadores.Width = 840;
        panelOtrosJugadores.Height = 100;
        panelOtrosJugadores.BackColor = Color.DarkGreen;
        this.Controls.Add(panelOtrosJugadores);

        panelCartaActual = new Panel();
        panelCartaActual.Location = new Point(400, 200);
        panelCartaActual.Width = 80;
        panelCartaActual.Height = 110;
        this.Controls.Add(panelCartaActual);

        btnRobar = new Button();
        btnRobar.Text = "Robar carta";
        btnRobar.Location = new Point(380, 330);
        btnRobar.Width = 120;
        btnRobar.Click += (s, e) => RobarClickeado?.Invoke();
        this.Controls.Add(btnRobar);

        panelMano = new FlowLayoutPanel();
        panelMano.Location = new Point(20, 420);
        panelMano.Width = 840;
        panelMano.Height = 120;
        panelMano.BackColor = Color.ForestGreen;
        this.Controls.Add(panelMano);
    }

    private Button CrearCartaVisual(Carta carta, EventHandler onClick)
    {
        Button btn = new Button();
        btn.Width = 70;
        btn.Height = 100;
        btn.BackColor = ColoresCartas[carta.Color];
        btn.ForeColor = Color.White;
        btn.Font = new Font("Arial", 14, FontStyle.Bold);
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = Color.White;
        btn.FlatAppearance.BorderSize = 2;

        btn.Text = carta.Tipo == "Numero" ? carta.Numero.ToString() : carta.Tipo switch
        {
            "Salta" => "🚫",
            "Reversa" => "🔄",
            "Mas2" => "+2",
            "Mas4" => "+4",
            "Comodin" => "🎨",
            _ => "?"
        };

        if (onClick != null)
            btn.Click += onClick;

        return btn;
    }

    public void ActualizarPantalla(List<Jugador> jugadores, int turnoActual, Carta cartaActual)
    {
        Jugador jugadorEnTurno = jugadores[turnoActual];

        lblTurno.Text = $"Turno de: {jugadorEnTurno.Nombre}";

        panelCartaActual.Controls.Clear();
        Button cartaVisual = CrearCartaVisual(cartaActual, null);
        cartaVisual.Enabled = false;
        panelCartaActual.Controls.Add(cartaVisual);

        panelOtrosJugadores.Controls.Clear();
        foreach (var j in jugadores)
        {
            if (j.Id != jugadorEnTurno.Id)
            {
                System.Windows.Forms.Label lblOtro = new System.Windows.Forms.Label();
                lblOtro.Text = $"{j.Nombre}: {j.Mano.Count} cartas";
                lblOtro.ForeColor = Color.White;
                lblOtro.Font = new Font("Arial", 10);
                lblOtro.AutoSize = true;
                lblOtro.Margin = new Padding(15, 10, 15, 10);
                panelOtrosJugadores.Controls.Add(lblOtro);
            }
        }

        panelMano.Controls.Clear();
        for (int i = 0; i < jugadorEnTurno.Mano.Count; i++)
        {
            int indice = i; // importante: copia local para que el closure no se confunda
            Button btnCarta = CrearCartaVisual(jugadorEnTurno.Mano[i], (s, e) => CartaClickeada?.Invoke(indice));
            panelMano.Controls.Add(btnCarta);
        }
    }

    public void MostrarGanador(string nombreGanador)
    {
        MessageBox.Show($"🎉 ¡{nombreGanador} ganó la partida! 🎉", "Fin del juego");
    }

    public void MostrarMensaje(string mensaje)
    {
        MessageBox.Show(mensaje);
    }
}