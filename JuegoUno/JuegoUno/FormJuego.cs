using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JuegoUno;

public class FormJuego : Form
{
    private System.Windows.Forms.Label lblTurno;
    private FlowLayoutPanel panelJugadoresArriba; 
    private System.Windows.Forms.Label lblNombreTurno; 
    private Panel panelCartaActual;
    private PictureBox picMazo;
    private PictureBox picUno;
    private FlowLayoutPanel panelMano; 

    public event Action<int> CartaClickeada;
    public event Action RobarClickeado;
    public event Action UnoClickeado;

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
        lblTurno.Location = new Point(20, 15);
        lblTurno.AutoSize = true;
        this.Controls.Add(lblTurno);

        panelJugadoresArriba = new FlowLayoutPanel();
        panelJugadoresArriba.FlowDirection = FlowDirection.TopDown;
        panelJugadoresArriba.WrapContents = false;
        panelJugadoresArriba.AutoScroll = true;
        panelJugadoresArriba.Location = new Point(20, 50);
        panelJugadoresArriba.Width = 1050;
        panelJugadoresArriba.Height = 200;
        panelJugadoresArriba.BackColor = ColoresJuego.FondoVentana;
        this.Controls.Add(panelJugadoresArriba);

        panelCartaActual = new Panel();
        panelCartaActual.Location = new Point(450, 280);
        panelCartaActual.Width = 90;
        panelCartaActual.Height = 130;
        this.Controls.Add(panelCartaActual);

        picMazo = new PictureBox();
        picMazo.Width = 80;
        picMazo.Height = 120;
        picMazo.SizeMode = PictureBoxSizeMode.StretchImage;
        picMazo.Location = new Point(600, 285);
        string rutaReverso = Path.Combine(Application.StartupPath, "Cartas", "Reverso.png");
        if (File.Exists(rutaReverso)) picMazo.Image = Image.FromFile(rutaReverso);
        picMazo.Cursor = Cursors.Hand;
        picMazo.Click += (s, e) => RobarClickeado?.Invoke();
        this.Controls.Add(picMazo);

        picUno = new PictureBox();
        picUno.Width = 90;
        picUno.Height = 90;
        picUno.SizeMode = PictureBoxSizeMode.Zoom;
        picUno.Location = new Point(750, 290);
        string rutaUno = Path.Combine(Application.StartupPath, "Cartas", "BotonUno.png");
        if (File.Exists(rutaUno)) picUno.Image = Image.FromFile(rutaUno);
        picUno.Cursor = Cursors.Hand;
        picUno.Click += (s, e) => UnoClickeado?.Invoke();
        this.Controls.Add(picUno);

        lblNombreTurno = new System.Windows.Forms.Label();
        lblNombreTurno.Text = "cartas ...";
        lblNombreTurno.Font = new Font("Arial", 12, FontStyle.Bold);
        lblNombreTurno.BackColor = Color.LightPink;
        lblNombreTurno.AutoSize = true;
        lblNombreTurno.Location = new Point(20, 440);
        this.Controls.Add(lblNombreTurno);

        panelMano = new FlowLayoutPanel();
        panelMano.Location = new Point(20, 475);
        panelMano.Width = 1050;
        panelMano.Height = 180;
        panelMano.BackColor = ColoresJuego.FondoVentana;
        panelMano.AutoScroll = true;
        this.Controls.Add(panelMano);
    }

    private PictureBox CrearCartaVisual(Carta carta, EventHandler onClick, int ancho = 80, int alto = 120)
    {
        PictureBox pic = new PictureBox();
        pic.Width = ancho;
        pic.Height = alto;
        pic.SizeMode = PictureBoxSizeMode.StretchImage;
        pic.Margin = new Padding(5);

        string rutaImagen = Path.Combine(Application.StartupPath, "Cartas", carta.NombreImagen());
        if (File.Exists(rutaImagen))
            pic.Image = Image.FromFile(rutaImagen);

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

        panelJugadoresArriba.Controls.Clear();
        foreach (var j in jugadores)
        {
            if (j.Id == jugadorEnTurno.Id) continue;

            System.Windows.Forms.Label lblNombre = new System.Windows.Forms.Label();
            lblNombre.Text = $"cartas {j.Nombre}";
            lblNombre.Font = new Font("Arial", 10, FontStyle.Bold);
            lblNombre.BackColor = Color.LightPink;
            lblNombre.AutoSize = true;
            lblNombre.Margin = new Padding(0, 5, 0, 2);
            panelJugadoresArriba.Controls.Add(lblNombre);

            FlowLayoutPanel filaCartas = new FlowLayoutPanel();
            filaCartas.AutoSize = true;
            filaCartas.WrapContents = false;
            filaCartas.Margin = new Padding(0, 0, 0, 10);

            foreach (var carta in j.Mano)
            {
                PictureBox picCartaAjena = CrearCartaVisual(carta, null, 55, 80); // más pequeñas
                filaCartas.Controls.Add(picCartaAjena);
            }

            panelJugadoresArriba.Controls.Add(filaCartas);
        }

        lblNombreTurno.Text = $"cartas {jugadorEnTurno.Nombre}";

        panelMano.Controls.Clear();
        for (int i = 0; i < jugadorEnTurno.Mano.Count; i++)
        {
            int indice = i;
            PictureBox picCarta = CrearCartaVisual(jugadorEnTurno.Mano[i], (s, e) => CartaClickeada?.Invoke(indice));
            panelMano.Controls.Add(picCarta);
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

    public void MostrarUltimaAccion(string texto)
    {
        MostrarMensaje(texto);
    }

    public void ForzarRepintado()
    {
        this.SuspendLayout();
        this.ResumeLayout(true);
        this.Invalidate(true);
        this.Update();
    }
}