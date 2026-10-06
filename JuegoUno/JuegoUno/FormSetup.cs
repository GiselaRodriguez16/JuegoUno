using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JuegoUno;

public class FormSetup : Form
{
    private PictureBox picLogo;
    private PictureBox picJugar;
    private PictureBox picSalir;

    private System.Windows.Forms.Label lblCantidad;
    private NumericUpDown numCantidad;
    private Panel panelNombres;
    private List<TextBox> inputsNombres = new List<TextBox>();

    public List<string> NombresJugadores { get; private set; } = new List<string>();

    public FormSetup()
    {
        this.Text = "UNO";
        this.Width = 1100;
        this.Height = 700;
        this.BackColor = ColoresJuego.FondoVentana;
        this.StartPosition = FormStartPosition.CenterScreen;

        MostrarPantallaInicio();
    }

    private void MostrarPantallaInicio()
    {
        string rutaFondo = Path.Combine(Application.StartupPath, "Menu", "Fondo.png");
        if (File.Exists(rutaFondo))
        {
            this.BackgroundImage = Image.FromFile(rutaFondo);
            this.BackgroundImageLayout = ImageLayout.Stretch;
        }

        this.Controls.Clear();

        picLogo = new PictureBox();
        picLogo.Width = 300;
        picLogo.Height = 200;
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.BackColor = Color.Transparent;
        picLogo.Location = new Point((this.ClientSize.Width - 300) / 2, 120);
        string rutaLogo = Path.Combine(Application.StartupPath, "Menu", "Logo.png");
        if (File.Exists(rutaLogo)) picLogo.Image = Image.FromFile(rutaLogo);
        this.Controls.Add(picLogo);

        picJugar = new PictureBox();
        picJugar.Width = 200;
        picJugar.Height = 80;
        picJugar.SizeMode = PictureBoxSizeMode.Zoom;
        picJugar.BackColor = Color.Transparent;
        picJugar.Location = new Point((this.ClientSize.Width - 200) / 2, 350);
        picJugar.Cursor = Cursors.Hand;
        string rutaJugar = Path.Combine(Application.StartupPath, "Menu", "Jugar.png");
        if (File.Exists(rutaJugar)) picJugar.Image = Image.FromFile(rutaJugar);
        picJugar.Click += (s, e) => MostrarPantallaJugadores();
        this.Controls.Add(picJugar);

        picSalir = new PictureBox();
        picSalir.Width = 200;
        picSalir.Height = 80;
        picSalir.SizeMode = PictureBoxSizeMode.Zoom;
        picSalir.BackColor = Color.Transparent;
        picSalir.Location = new Point((this.ClientSize.Width - 200) / 2, 450);
        picSalir.Cursor = Cursors.Hand;
        string rutaSalir = Path.Combine(Application.StartupPath, "Menu", "Salir.png");
        if (File.Exists(rutaSalir)) picSalir.Image = Image.FromFile(rutaSalir);
        picSalir.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };
        this.Controls.Add(picSalir);
    }

    private void MostrarPantallaJugadores()
    {
        this.BackgroundImage = null;
        this.BackColor = ColoresJuego.FondoVentana;

        this.Controls.Clear();

        // Imagen de "Número de jugadores"
        PictureBox picTituloJugadores = new PictureBox();
        picTituloJugadores.Width = 300;
        picTituloJugadores.Height = 80;
        picTituloJugadores.SizeMode = PictureBoxSizeMode.Zoom;
        picTituloJugadores.Location = new Point((this.ClientSize.Width - 300) / 2, 40);
        string rutaTitulo = Path.Combine(Application.StartupPath, "Menu", "NumeroJugadores.png");
        if (File.Exists(rutaTitulo)) picTituloJugadores.Image = Image.FromFile(rutaTitulo);
        this.Controls.Add(picTituloJugadores);

        numCantidad = new NumericUpDown();
        numCantidad.Minimum = 2;
        numCantidad.Maximum = 8;
        numCantidad.Value = 2;
        numCantidad.Font = new Font("Century Gothic", 12);
        numCantidad.Width = 80;
        numCantidad.Location = new Point((this.ClientSize.Width - 80) / 2, 140);
        numCantidad.ValueChanged += NumCantidad_ValueChanged;
        this.Controls.Add(numCantidad);

        panelNombres = new Panel();
        panelNombres.Width = 340;
        panelNombres.Height = 300;
        panelNombres.Location = new Point((this.ClientSize.Width - 340) / 2, 200);
        panelNombres.AutoScroll = true;
        this.Controls.Add(panelNombres);

        PictureBox picContinuar = new PictureBox();
        picContinuar.Width = 200;
        picContinuar.Height = 70;
        picContinuar.SizeMode = PictureBoxSizeMode.Zoom;
        picContinuar.Location = new Point((this.ClientSize.Width - 200) / 2, 520);
        picContinuar.Cursor = Cursors.Hand;
        string rutaContinuar = Path.Combine(Application.StartupPath, "Menu", "EmpezarJuego.png");
        if (File.Exists(rutaContinuar)) picContinuar.Image = Image.FromFile(rutaContinuar);
        picContinuar.Click += BtnContinuar_Click;
        this.Controls.Add(picContinuar);

        GenerarCamposNombres((int)numCantidad.Value);
    }

    private void NumCantidad_ValueChanged(object sender, EventArgs e)
    {
        GenerarCamposNombres((int)numCantidad.Value);
    }

    private void GenerarCamposNombres(int cantidad)
    {
        panelNombres.Controls.Clear();
        inputsNombres.Clear();

        for (int i = 0; i < cantidad; i++)
        {
            System.Windows.Forms.Label lbl = new System.Windows.Forms.Label();
            lbl.Text = $"Jugador {i + 1}:";
            lbl.ForeColor = Color.White;
            lbl.Font = new Font("Ravie", 11);
            lbl.Location = new Point(0, i * 35);
            lbl.AutoSize = true;
            panelNombres.Controls.Add(lbl);

            TextBox txt = new TextBox();
            txt.Location = new Point(120, i * 35);
            txt.Width = 200;
            panelNombres.Controls.Add(txt);

            inputsNombres.Add(txt);
        }
    }

    private void BtnContinuar_Click(object sender, EventArgs e)
    {
        NombresJugadores.Clear();

        foreach (var txt in inputsNombres)
        {
            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                MessageBox.Show("Todos los jugadores deben tener nombre.");
                return;
            }
            NombresJugadores.Add(txt.Text.Trim());
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}