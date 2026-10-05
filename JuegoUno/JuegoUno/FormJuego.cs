using JuegoUno;
using System.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class FormJuego : Form
{
    private System.Windows.Forms.Label lblTurno;
    private System.Windows.Forms.Label lblUltimaAccion;
    private FlowLayoutPanel panelOtrosJugadores;
    private Panel panelCartaActual;
    private FlowLayoutPanel panelMano;
    private PictureBox picMazo;

    public event Action<int> CartaClickeada; 
    public event Action RobarClickeado;

    public FormJuego()
    {
        this.Text = "UNO - Juego";
        this.Width = 1100;
        this.Height = 700;
        this.BackColor = ColoresJuego.FondoVentana;

        lblTurno = new System.Windows.Forms.Label();
        lblTurno.Text = "Turno de: ...";
        lblTurno.ForeColor = Color.White;
        lblTurno.Font = new Font("Arial", 14, FontStyle.Bold);
        lblTurno.Location = new Point(20, 20);
        lblTurno.AutoSize = true;
        this.Controls.Add(lblTurno);

        lblUltimaAccion = new System.Windows.Forms.Label();
        lblUltimaAccion.Text = "";
        lblUltimaAccion.ForeColor = Color.Gold;
        lblUltimaAccion.Font = new Font("Arial", 11, FontStyle.Italic);
        lblUltimaAccion.Location = new Point(20, 180);
        lblUltimaAccion.AutoSize = true;
        this.Controls.Add(lblUltimaAccion);

        panelOtrosJugadores = new FlowLayoutPanel();
        panelOtrosJugadores.Location = new Point(130, 60);
        panelOtrosJugadores.Width = 840;
        panelOtrosJugadores.Height = 100;
        panelOtrosJugadores.BackColor = ColoresJuego.FondoPanel;
        this.Controls.Add(panelOtrosJugadores);

        panelCartaActual = new Panel();
        panelCartaActual.Location = new Point(400, 200);
        panelCartaActual.Width = 90;
        panelCartaActual.Height = 130;
        this.Controls.Add(panelCartaActual);

        picMazo = new PictureBox();
        picMazo.Width = 80;
        picMazo.Height = 120;
        picMazo.SizeMode = PictureBoxSizeMode.StretchImage;
        picMazo.Location = new Point(620, 200); // junto a la carta central, ajusta si quieres

        string rutaReverso = Path.Combine(Application.StartupPath, "Cartas", "Reverso.png");
        if (File.Exists(rutaReverso))
        {
            picMazo.Image = Image.FromFile(rutaReverso);
        }

        picMazo.Cursor = Cursors.Hand;
        picMazo.Click += (s, e) => RobarClickeado?.Invoke();
        this.Controls.Add(picMazo);

        panelMano = new FlowLayoutPanel();
        panelMano.Location = new Point(130, 500);
        panelMano.Width = 840;
        panelMano.Height = 150;
        panelMano.BackColor = ColoresJuego.FondoPanel;
        this.Controls.Add(panelMano);
    }

    private PictureBox CrearCartaVisual(Carta carta, EventHandler onClick)
    {
        PictureBox pic = new PictureBox();
        pic.Width = 80;
        pic.Height = 120;
        pic.SizeMode = PictureBoxSizeMode.StretchImage;
        pic.Margin = new Padding(5);

        string rutaImagen = Path.Combine(Application.StartupPath, "Cartas", carta.NombreImagen());
        if (File.Exists(rutaImagen))
        {
            pic.Image = Image.FromFile(rutaImagen);
        }

        if (onClick != null)
        {
            pic.Cursor = Cursors.Hand;
            pic.Click += onClick;
        }

        return pic;
    }

    public void ActualizarPantalla(List<Jugador> jugadores, int turnoActual, Carta cartaActual)
    {
        Jugador jugadorEnTurno = jugadores[turnoActual];

        lblTurno.Text = $"Turno de: {jugadorEnTurno.Nombre}";

        panelCartaActual.Controls.Clear();
        PictureBox cartaVisual = CrearCartaVisual(cartaActual, null);
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
            int indice = i;
            PictureBox picCarta = CrearCartaVisual(jugadorEnTurno.Mano[i], (s, e) => CartaClickeada?.Invoke(indice));
            panelMano.Controls.Add(picCarta);
        }
    }

    public void MostrarUltimaAccion(string texto)
    {
        lblUltimaAccion.Text = texto;
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