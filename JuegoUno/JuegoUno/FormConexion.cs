using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using JuegoUno;

public class FormConexion : Form
{
    private TextBox txtIp;
    private Button btnConectar;
    private Button btnContinuar;
    private System.Windows.Forms.Label lblEstado;
    private System.Windows.Forms.Label lblJugadoras;

    private readonly ClienteSocket conexion;
    private readonly string rutaIpGuardada = Path.Combine(Application.StartupPath, "servidor.txt");

    public string IpServidor { get; private set; }

    public FormConexion(ClienteSocket conexion)
    {
        this.conexion = conexion;

        this.Text = "UNO - Conectar al servidor";
        this.Width = 480;
        this.Height = 300;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ColoresJuego.FondoVentana;

        System.Windows.Forms.Label lblIp = new System.Windows.Forms.Label();
        lblIp.Text = "IP del servidor:";
        lblIp.ForeColor = Color.White;
        lblIp.Font = new Font("Arial", 12, FontStyle.Bold);
        lblIp.Location = new Point(30, 30);
        lblIp.AutoSize = true;
        this.Controls.Add(lblIp);

        txtIp = new TextBox();
        txtIp.Font = new Font("Arial", 12);
        txtIp.Location = new Point(30, 65);
        txtIp.Width = 250;
        txtIp.Text = LeerIpGuardada();
        this.Controls.Add(txtIp);

        btnConectar = new Button();
        btnConectar.Text = "Conectar";
        btnConectar.Font = new Font("Arial", 10, FontStyle.Bold);
        btnConectar.BackColor = Color.White;
        btnConectar.Location = new Point(295, 63);
        btnConectar.Width = 130;
        btnConectar.Height = 32;
        btnConectar.Click += BtnConectar_Click;
        this.Controls.Add(btnConectar);
        this.AcceptButton = btnConectar; // Enter = Conectar

        lblEstado = new System.Windows.Forms.Label();
        lblEstado.Text = "Escribe la IP y presiona Conectar.";
        lblEstado.ForeColor = Color.White;
        lblEstado.Font = new Font("Arial", 11, FontStyle.Bold);
        lblEstado.Location = new Point(30, 115);
        lblEstado.AutoSize = true;
        lblEstado.MaximumSize = new Size(410, 0);
        this.Controls.Add(lblEstado);

        lblJugadoras = new System.Windows.Forms.Label();
        lblJugadoras.Text = "";
        lblJugadoras.ForeColor = Color.White;
        lblJugadoras.Font = new Font("Arial", 10);
        lblJugadoras.Location = new Point(30, 165);
        lblJugadoras.AutoSize = true;
        this.Controls.Add(lblJugadoras);

        btnContinuar = new Button();
        btnContinuar.Text = "Continuar";
        btnContinuar.Font = new Font("Arial", 10, FontStyle.Bold);
        btnContinuar.BackColor = Color.White;
        btnContinuar.Location = new Point(295, 200);
        btnContinuar.Width = 130;
        btnContinuar.Height = 35;
        btnContinuar.Enabled = false;
        btnContinuar.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        };
        this.Controls.Add(btnContinuar);

        conexion.MensajeRecibido += AlRecibirMensaje;
        this.FormClosed += (s, e) => conexion.MensajeRecibido -= AlRecibirMensaje;
    }

    private async void BtnConectar_Click(object sender, EventArgs e)
    {
        string ip = txtIp.Text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            MostrarEstado("Escribe la IP del servidor.", Color.Yellow);
            return;
        }

        btnConectar.Enabled = false;
        txtIp.Enabled = false;
        MostrarEstado("Conectando...", Color.White);

        try
        {
            // 1) Probar la API (puerto 8000)
            ApiService api = new ApiService(ip);
            if (!await api.ProbarConexion())
                throw new Exception("la API no respondió (puerto 8000).");

            // 2) Conectar el socket (puerto 5000)
            await conexion.ConectarAsync(ip, 5000);

            IpServidor = ip;
            GuardarIp(ip);
            // El "Conectado" se muestra cuando llega el mensaje de bienvenida del servidor
        }
        catch (Exception ex)
        {
            MostrarEstado("No se pudo conectar: " + ex.Message, Color.Yellow);
            btnConectar.Enabled = true;
            txtIp.Enabled = true;
        }
    }

    private void AlRecibirMensaje(JsonElement mensaje)
    {
        if (this.IsDisposed) return;

        // Los mensajes llegan en otro hilo; BeginInvoke los pasa al hilo de la ventana
        this.BeginInvoke((Action)(() =>
        {
            string tipo = mensaje.GetProperty("tipo").GetString();
            string texto = mensaje.TryGetProperty("mensaje", out var m) ? m.GetString() : "";

            if (tipo == "bienvenida")
            {
                MostrarEstado("✅ " + texto, Color.LightGreen);
                btnContinuar.Enabled = true;
            }
            else if (tipo == "info")
            {
                lblJugadoras.Text = texto;
            }
        }));
    }

    private void MostrarEstado(string texto, Color color)
    {
        lblEstado.Text = texto;
        lblEstado.ForeColor = color;
    }

    private string LeerIpGuardada()
    {
        try
        {
            if (File.Exists(rutaIpGuardada))
                return File.ReadAllText(rutaIpGuardada).Trim();
        }
        catch { }
        return "127.0.0.1";
    }

    private void GuardarIp(string ip)
    {
        try { File.WriteAllText(rutaIpGuardada, ip); } catch { }
    }
}