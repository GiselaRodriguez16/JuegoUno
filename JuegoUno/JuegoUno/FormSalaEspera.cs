using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using JuegoUno;

namespace JuegoUno
{
    // Una jugadora tal como la manda el servidor
    public class JugadoraSala
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }
}

public class FormSalaEspera : Form
{
    private TextBox txtNombre;
    private Button btnUnirme;
    private Button btnEmpezar;
    private ListBox lstJugadoras;
    private System.Windows.Forms.Label lblEstado;

    private readonly ClienteSocket conexion;
    private bool registrada = false;

    public int MiId { get; private set; }
    public List<JugadoraSala> Jugadoras { get; private set; } = new List<JugadoraSala>();

    public FormSalaEspera(ClienteSocket conexion)
    {
        this.conexion = conexion;

        this.Text = "UNO - Sala de espera";
        this.Width = 500;
        this.Height = 480;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ColoresJuego.FondoVentana;

        System.Windows.Forms.Label lblTitulo = new System.Windows.Forms.Label();
        lblTitulo.Text = "Sala de espera";
        lblTitulo.ForeColor = Color.White;
        lblTitulo.Font = new Font("Arial", 16, FontStyle.Bold);
        lblTitulo.Location = new Point(30, 20);
        lblTitulo.AutoSize = true;
        this.Controls.Add(lblTitulo);

        System.Windows.Forms.Label lblNombre = new System.Windows.Forms.Label();
        lblNombre.Text = "Tu nombre:";
        lblNombre.ForeColor = Color.White;
        lblNombre.Font = new Font("Arial", 11, FontStyle.Bold);
        lblNombre.Location = new Point(30, 70);
        lblNombre.AutoSize = true;
        this.Controls.Add(lblNombre);

        txtNombre = new TextBox();
        txtNombre.Font = new Font("Arial", 12);
        txtNombre.Location = new Point(30, 100);
        txtNombre.Width = 270;
        this.Controls.Add(txtNombre);

        btnUnirme = new Button();
        btnUnirme.Text = "Unirme";
        btnUnirme.Font = new Font("Arial", 10, FontStyle.Bold);
        btnUnirme.BackColor = Color.White;
        btnUnirme.Location = new Point(315, 98);
        btnUnirme.Width = 140;
        btnUnirme.Height = 32;
        btnUnirme.Click += BtnUnirme_Click;
        this.Controls.Add(btnUnirme);
        this.AcceptButton = btnUnirme; // Enter = Unirme

        System.Windows.Forms.Label lblLista = new System.Windows.Forms.Label();
        lblLista.Text = "Jugadoras en la sala:";
        lblLista.ForeColor = Color.White;
        lblLista.Font = new Font("Arial", 11, FontStyle.Bold);
        lblLista.Location = new Point(30, 150);
        lblLista.AutoSize = true;
        this.Controls.Add(lblLista);

        lstJugadoras = new ListBox();
        lstJugadoras.Font = new Font("Arial", 12);
        lstJugadoras.Location = new Point(30, 180);
        lstJugadoras.Width = 425;
        lstJugadoras.Height = 150;
        this.Controls.Add(lstJugadoras);

        lblEstado = new System.Windows.Forms.Label();
        lblEstado.Text = "Escribe tu nombre y presiona Unirme.";
        lblEstado.ForeColor = Color.White;
        lblEstado.Font = new Font("Arial", 10, FontStyle.Bold);
        lblEstado.Location = new Point(30, 345);
        lblEstado.AutoSize = true;
        lblEstado.MaximumSize = new Size(425, 0);
        this.Controls.Add(lblEstado);

        btnEmpezar = new Button();
        btnEmpezar.Text = "Empezar";
        btnEmpezar.Font = new Font("Arial", 11, FontStyle.Bold);
        btnEmpezar.BackColor = Color.White;
        btnEmpezar.Location = new Point(315, 385);
        btnEmpezar.Width = 140;
        btnEmpezar.Height = 38;
        btnEmpezar.Enabled = false;
        btnEmpezar.Click += BtnEmpezar_Click;
        this.Controls.Add(btnEmpezar);

        conexion.MensajeRecibido += AlRecibirMensaje;
        conexion.Desconectado += AlDesconectarse;

        // Al abrir la ventana, pide la lista de quién ya está en la sala
        this.Shown += async (s, e) => await EnviarSeguro(new { tipo = "pedir_sala" });

        this.FormClosed += (s, e) =>
        {
            conexion.MensajeRecibido -= AlRecibirMensaje;
            conexion.Desconectado -= AlDesconectarse;
        };
    }

    private async void BtnUnirme_Click(object sender, EventArgs e)
    {
        string nombre = txtNombre.Text.Trim();
        if (nombre == "")
        {
            MostrarEstado("Escribe tu nombre.", Color.Yellow);
            return;
        }

        btnUnirme.Enabled = false;
        await EnviarSeguro(new { tipo = "unirse", nombre = nombre });
    }

    private async void BtnEmpezar_Click(object sender, EventArgs e)
    {
        btnEmpezar.Enabled = false;
        await EnviarSeguro(new { tipo = "empezar" });
    }

    private async System.Threading.Tasks.Task EnviarSeguro(object datos)
    {
        try
        {
            await conexion.EnviarAsync(datos);
        }
        catch
        {
            MostrarEstado("❌ Se perdió la conexión con el servidor.", Color.Yellow);
        }
    }

    // Los mensajes llegan en otro hilo; BeginInvoke los pasa al hilo de la ventana
    private void AlRecibirMensaje(JsonElement mensaje)
    {
        if (this.IsDisposed || !this.IsHandleCreated) return;
        try
        {
            this.BeginInvoke((Action)(() => ProcesarMensaje(mensaje)));
        }
        catch (InvalidOperationException) { }
    }

    private void ProcesarMensaje(JsonElement mensaje)
    {
        string tipo = mensaje.GetProperty("tipo").GetString();

        switch (tipo)
        {
            case "registrado":
                registrada = true;
                MiId = mensaje.GetProperty("id").GetInt32();
                txtNombre.Enabled = false;
                MostrarEstado($"✅ Estás en la sala como {mensaje.GetProperty("nombre").GetString()}. " +
                              "Cuando estén todas, presionen Empezar.", Color.LightGreen);
                ActualizarBotonEmpezar();
                break;

            case "sala":
                Jugadoras = LeerJugadoras(mensaje);
                lstJugadoras.Items.Clear();
                foreach (var j in Jugadoras)
                {
                    string texto = (registrada && j.Id == MiId) ? $"{j.Nombre} (tú)" : j.Nombre;
                    lstJugadoras.Items.Add(texto);
                }
                ActualizarBotonEmpezar();
                break;

            case "error":
                MostrarEstado("⚠️ " + mensaje.GetProperty("mensaje").GetString(), Color.Yellow);
                if (!registrada) btnUnirme.Enabled = true;
                ActualizarBotonEmpezar();
                break;

            case "iniciar":
                if (!registrada)
                {
                    MostrarEstado("La partida empezó sin ti 😢", Color.Yellow);
                    return;
                }
                Jugadoras = LeerJugadoras(mensaje);
                this.DialogResult = DialogResult.OK;
                this.Close();
                break;
        }
    }

    private List<JugadoraSala> LeerJugadoras(JsonElement mensaje)
    {
        List<JugadoraSala> lista = new List<JugadoraSala>();
        foreach (JsonElement item in mensaje.GetProperty("jugadoras").EnumerateArray())
        {
            lista.Add(new JugadoraSala
            {
                Id = item.GetProperty("id").GetInt32(),
                Nombre = item.GetProperty("nombre").GetString()
            });
        }
        return lista;
    }

    private void ActualizarBotonEmpezar()
    {
        btnEmpezar.Enabled = registrada && Jugadoras.Count >= 2;
    }

    private void AlDesconectarse()
    {
        if (this.IsDisposed || !this.IsHandleCreated) return;
        try
        {
            this.BeginInvoke((Action)(() =>
            {
                MostrarEstado("❌ Se perdió la conexión con el servidor.", Color.Yellow);
                btnUnirme.Enabled = false;
                btnEmpezar.Enabled = false;
            }));
        }
        catch (InvalidOperationException) { }
    }

    private void MostrarEstado(string texto, Color color)
    {
        lblEstado.Text = texto;
        lblEstado.ForeColor = color;
    }
}