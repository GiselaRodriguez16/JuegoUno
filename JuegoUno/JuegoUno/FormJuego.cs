using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using JuegoUno;

public class FormJuego : Form
{
    private System.Windows.Forms.Label lblTurno;
    private System.Windows.Forms.Label lblAviso;
    private FlowLayoutPanel panelJugadoresArriba;
    private System.Windows.Forms.Label lblMisCartas;
    private Panel panelCartaActual;
    private PictureBox picMazo;
    private PictureBox picUno;
    private FlowLayoutPanel panelMano;
    private System.Windows.Forms.Label lblMazo;
    private System.Windows.Forms.Label lblSentido;
    private System.Windows.Forms.Label lblColorElegido;

    private System.Windows.Forms.Label lblUno;
    private System.Windows.Forms.Timer timerUno;   // Solo para mostrar el contador; el tiempo real lo lleva el servidor
    private int segundosUno;
    private string textoUno = "";

    // Guarda las imágenes ya cargadas para no abrir el archivo cada vez que se redibuja
    private readonly Dictionary<string, Image> cacheImagenes = new Dictionary<string, Image>();

    public event Action<int> CartaClickeada;
    public event Action RobarClickeado;
    public event Action UnoClickeado;

    public FormJuego()
    {
        this.Text = "UNO - Juego";
        this.Width = 1100;
        this.Height = 700;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = ColoresJuego.FondoVentana;

        lblTurno = new System.Windows.Forms.Label();
        lblTurno.Text = "Esperando al servidor...";
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

        // Mensajes cortos sin ventanas emergentes (ej. "No es tu turno")
        lblAviso = new System.Windows.Forms.Label();
        lblAviso.Text = "";
        lblAviso.ForeColor = Color.White;
        lblAviso.Font = new Font("Arial", 11, FontStyle.Bold);
        lblAviso.Location = new Point(20, 290);
        lblAviso.AutoSize = true;
        lblAviso.MaximumSize = new Size(400, 0);
        this.Controls.Add(lblAviso);

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
        picMazo.Image = CargarImagen("Reverso.png");
        picMazo.Cursor = Cursors.Hand;
        picMazo.Click += (s, e) => RobarClickeado?.Invoke();
        this.Controls.Add(picMazo);

        picUno = new PictureBox();
        picUno.Width = 90;
        picUno.Height = 90;
        picUno.SizeMode = PictureBoxSizeMode.Zoom;
        picUno.Location = new Point(750, 290);
        picUno.Image = CargarImagen("BotonUno.png");
        picUno.Cursor = Cursors.Hand;
        picUno.Click += (s, e) => UnoClickeado?.Invoke();
        this.Controls.Add(picUno);

        // Cuántas cartas quedan en el mazo
        lblMazo = new System.Windows.Forms.Label();
        lblMazo.Text = "";
        lblMazo.ForeColor = Color.White;
        lblMazo.Font = new Font("Arial", 10, FontStyle.Bold);
        lblMazo.Location = new Point(600, 412);
        lblMazo.AutoSize = true;
        this.Controls.Add(lblMazo);

        // Sentido del juego
        lblSentido = new System.Windows.Forms.Label();
        lblSentido.Text = "";
        lblSentido.ForeColor = Color.White;
        lblSentido.Font = new Font("Arial", 12, FontStyle.Bold);
        lblSentido.Location = new Point(750, 390);
        lblSentido.AutoSize = true;
        this.Controls.Add(lblSentido);

        // Color elegido cuando en el centro hay un comodín (la imagen es negra)
        lblColorElegido = new System.Windows.Forms.Label();
        lblColorElegido.Text = "";
        lblColorElegido.Font = new Font("Arial", 10, FontStyle.Bold);
        lblColorElegido.Location = new Point(450, 415);
        lblColorElegido.AutoSize = true;
        lblColorElegido.Padding = new Padding(4, 2, 4, 2);
        lblColorElegido.Visible = false;
        this.Controls.Add(lblColorElegido);


        // Contador del UNO (a la derecha del botón)
        lblUno = new System.Windows.Forms.Label();
        lblUno.Text = "";
        lblUno.ForeColor = Color.Yellow;
        lblUno.Font = new Font("Arial", 13, FontStyle.Bold);
        lblUno.Location = new Point(850, 315);
        lblUno.AutoSize = true;
        lblUno.MaximumSize = new Size(220, 0);
        lblUno.Visible = false;
        this.Controls.Add(lblUno);

        timerUno = new System.Windows.Forms.Timer();
        timerUno.Interval = 1000;   // Cada segundo
        timerUno.Tick += (s, e) =>
        {
            if (segundosUno > 0) segundosUno--;
            lblUno.Text = $"{textoUno} {segundosUno}";
            if (segundosUno <= 0) timerUno.Stop();
        };
        this.FormClosed += (s, e) => timerUno.Stop();

        lblMisCartas = new System.Windows.Forms.Label();
        lblMisCartas.Text = "Tus cartas";
        lblMisCartas.Font = new Font("Arial", 12, FontStyle.Bold);
        lblMisCartas.BackColor = Color.LightPink;
        lblMisCartas.AutoSize = true;
        lblMisCartas.Location = new Point(20, 440);
        this.Controls.Add(lblMisCartas);

        panelMano = new FlowLayoutPanel();
        panelMano.Location = new Point(20, 475);
        panelMano.Width = 1050;
        panelMano.Height = 180;
        panelMano.BackColor = ColoresJuego.FondoVentana;
        panelMano.AutoScroll = true;
        this.Controls.Add(panelMano);

        this.FormClosed += (s, e) =>
        {
            foreach (Image img in cacheImagenes.Values) img?.Dispose();
        };
    }

    // Ejecuta algo en el hilo de la ventana (los mensajes del servidor llegan en otro hilo)
    public void EnPantalla(Action accion)
    {
        if (this.IsDisposed || !this.IsHandleCreated) return;
        try
        {
            if (this.InvokeRequired) this.BeginInvoke(accion);
            else accion();
        }
        catch (InvalidOperationException) { }
    }

    // Dibuja la mesa tal como la manda el servidor
    // Dibuja la mesa tal como la manda el servidor
    public void MostrarEstado(EstadoJuego estado)
    {
        bool unoEnEspera = estado.UnoPendiente.HasValue;
        bool terminada = estado.Ganadora.HasValue;
        bool esMiTurno = estado.Turno == estado.MiId && !unoEnEspera && !terminada;   // Mientras alguien canta UNO o si ya terminó, nadie juega
        string nombreTurno = estado.Jugadoras.FirstOrDefault(j => j.Id == estado.Turno)?.Nombre ?? "...";
        string miNombre = estado.Jugadoras.FirstOrDefault(j => j.Id == estado.MiId)?.Nombre ?? "";

        lblTurno.Text = esMiTurno ? "¡Es tu turno!" : $"Turno de: {nombreTurno}";

        if (unoEnEspera) lblTurno.Text = "⏸ Esperando a que canten UNO...";
        if (terminada) lblTurno.Text = "Fin de la partida";

        // Última jugada, la ven todas (ej. "Gis tiró Rojo 5")
        lblAviso.Text = estado.UltimaAccion ?? "";

        // Carta del centro
        LimpiarPanel(panelCartaActual);
        panelCartaActual.Controls.Add(CrearCartaVisual(estado.CartaActual.NombreImagen(), null));


        // Si en el centro hay un comodín, se muestra el color que eligieron
        string colorCentro = estado.CartaActual.Color;
        bool centroEsComodin = estado.CartaActual.Tipo == "Comodin" || estado.CartaActual.Tipo == "Mas4";

        if (centroEsComodin && ColoresJuego.Cartas.ContainsKey(colorCentro) && colorCentro != "Ninguno")
        {
            lblColorElegido.Text = $"Color: {colorCentro}";
            lblColorElegido.BackColor = ColoresJuego.Cartas[colorCentro];
            lblColorElegido.ForeColor = colorCentro == "Rojo" ? Color.White : Color.Black;
            lblColorElegido.Visible = true;
        }
        else
        {
            lblColorElegido.Visible = false;
        }

        // Las demás jugadoras: solo el reverso y cuántas cartas tienen
        LimpiarPanel(panelJugadoresArriba);
        foreach (JugadoraEstado j in estado.Jugadoras)
        {
            if (j.Id == estado.MiId) continue;

            System.Windows.Forms.Label lblNombre = new System.Windows.Forms.Label();
            lblNombre.Text = $"{j.Nombre} – {j.NumCartas} carta(s)" + (j.Id == estado.Turno ? "   ◀ su turno" : "");
            lblNombre.Font = new Font("Arial", 10, FontStyle.Bold);
            lblNombre.BackColor = Color.LightPink;
            lblNombre.AutoSize = true;
            lblNombre.Margin = new Padding(0, 5, 0, 2);
            panelJugadoresArriba.Controls.Add(lblNombre);

            FlowLayoutPanel filaCartas = new FlowLayoutPanel();
            filaCartas.AutoSize = true;
            filaCartas.WrapContents = false;
            filaCartas.Margin = new Padding(0, 0, 0, 10);

            for (int i = 0; i < j.NumCartas; i++)
            {
                filaCartas.Controls.Add(CrearCartaVisual("Reverso.png", null, 55, 80));
            }

            panelJugadoresArriba.Controls.Add(filaCartas);
        }

        // Mi mano: solo se puede dar clic si es mi turno
        if (esMiTurno)
        {
            lblMisCartas.Text = $"Tus cartas ({miNombre}) – elige una carta";
            lblMisCartas.BackColor = Color.Gold;
        }
        else
        {
            lblMisCartas.Text = $"Tus cartas ({miNombre}) – espera tu turno";
            lblMisCartas.BackColor = Color.LightPink;
        }

        LimpiarPanel(panelMano);
        for (int i = 0; i < estado.MiMano.Count; i++)
        {
            int indice = i;
            Carta carta = estado.MiMano[i];

            EventHandler clic = null;
            if (esMiTurno)
            {
                clic = (s, e) => CartaClickeada?.Invoke(indice);
            }

            PictureBox pic = CrearCartaVisual(carta.NombreImagen(), clic);

            // Borde dorado en las cartas que sí se pueden tirar
            if (esMiTurno && carta.EsValidaSobre(estado.CartaActual))
            {
                pic.BackColor = Color.Gold;
                pic.Padding = new Padding(4);
            }

            panelMano.Controls.Add(pic);
        }

        // Mazo y sentido del juego
        lblMazo.Text = $"Mazo: {estado.CartasEnMazo}";
        lblSentido.Text = estado.Direccion == 1 ? "Sentido: →" : "Sentido: ←";

        // Si es mi turno y no tengo ninguna carta válida, el mazo se ilumina
        bool puedoTirarAlgo = estado.MiMano.Any(c => c.EsValidaSobre(estado.CartaActual));

        if (esMiTurno && !puedoTirarAlgo)
        {
            picMazo.BackColor = Color.Gold;
            picMazo.Padding = new Padding(4);
            lblMazo.Text += "   ¡Roba!";
        }
        else
        {
            picMazo.BackColor = ColoresJuego.FondoVentana;
            picMazo.Padding = new Padding(0);
        }

        ActualizarUno(estado);
    }

    // Muestra el contador y prende el botón UNO para quien debe cantarlo
    private void ActualizarUno(EstadoJuego estado)
    {
        if (estado.UnoPendiente.HasValue)
        {
            int idUno = estado.UnoPendiente.Value;
            bool soyYo = idUno == estado.MiId;
            string nombre = estado.Jugadoras.FirstOrDefault(j => j.Id == idUno)?.Nombre ?? "";

            textoUno = soyYo ? "¡Presiona UNO!" : $"{nombre} debe cantar UNO:";
            segundosUno = estado.UnoSegundos;
            lblUno.Text = $"{textoUno} {segundosUno}";
            lblUno.Visible = true;

            picUno.BackColor = soyYo ? Color.Gold : ColoresJuego.FondoVentana;
            picUno.Padding = soyYo ? new Padding(4) : new Padding(0);

            timerUno.Start();
        }
        else
        {
            timerUno.Stop();
            lblUno.Visible = false;
            picUno.BackColor = ColoresJuego.FondoVentana;
            picUno.Padding = new Padding(0);
        }
    }

    public void MostrarAviso(string texto)
    {
        lblAviso.Text = texto;
    }

    public void MostrarMensaje(string mensaje)
    {
        MessageBox.Show(mensaje);
    }

    public void MostrarGanador(string nombreGanador)
    {
        MessageBox.Show($"🎉 ¡{nombreGanador} ganó la partida! 🎉", "Fin del juego");
    }

    private PictureBox CrearCartaVisual(string nombreImagen, EventHandler onClick, int ancho = 80, int alto = 120)
    {
        PictureBox pic = new PictureBox();
        pic.Width = ancho;
        pic.Height = alto;
        pic.SizeMode = PictureBoxSizeMode.StretchImage;
        pic.Margin = new Padding(5);
        pic.Image = CargarImagen(nombreImagen);

        if (onClick != null)
        {
            pic.Cursor = Cursors.Hand;
            pic.Click += onClick;
        }

        return pic;
    }

    private Image CargarImagen(string nombreArchivo)
    {
        if (cacheImagenes.TryGetValue(nombreArchivo, out Image guardada))
            return guardada;

        string ruta = Path.Combine(Application.StartupPath, "Cartas", nombreArchivo);
        Image imagen = File.Exists(ruta) ? Image.FromFile(ruta) : null;
        cacheImagenes[nombreArchivo] = imagen;
        return imagen;
    }

    // Quita los controles de un panel y libera su memoria
    private void LimpiarPanel(Control panel)
    {
        List<Control> anteriores = panel.Controls.Cast<Control>().ToList();
        panel.Controls.Clear();
        foreach (Control c in anteriores) c.Dispose();
    }
}