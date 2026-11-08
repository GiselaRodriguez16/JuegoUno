using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JuegoUno;

// Pantalla que aparece cuando alguien gana
public class FormFinPartida : Form
{
    public FormFinPartida(EstadoJuego estado)
    {
        this.Text = "UNO - Fin de la partida";
        this.Width = 460;
        this.Height = 420;
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ColoresJuego.FondoVentana;

        int idGanadora = estado.Ganadora ?? -1;
        bool gane = idGanadora == estado.MiId;
        string nombreGanadora = estado.Jugadoras.FirstOrDefault(j => j.Id == idGanadora)?.Nombre ?? "?";

        // Título grande
        System.Windows.Forms.Label lblTitulo = new System.Windows.Forms.Label();
        lblTitulo.Text = gane ? "¡Ganaste!" : $"¡{nombreGanadora} ganó!";
        lblTitulo.ForeColor = gane ? Color.Gold : Color.White;
        lblTitulo.Font = new Font("Arial", 22, FontStyle.Bold);
        lblTitulo.Location = new Point(30, 25);
        lblTitulo.AutoSize = true;
        this.Controls.Add(lblTitulo);

        // Cómo terminó (ej. "Gis tiró Azul 7 → ¡Gis ganó la partida!")
        System.Windows.Forms.Label lblDetalle = new System.Windows.Forms.Label();
        lblDetalle.Text = estado.UltimaAccion ?? "";
        lblDetalle.ForeColor = Color.White;
        lblDetalle.Font = new Font("Arial", 10);
        lblDetalle.Location = new Point(30, 75);
        lblDetalle.AutoSize = true;
        lblDetalle.MaximumSize = new Size(390, 0);
        this.Controls.Add(lblDetalle);

        System.Windows.Forms.Label lblResultados = new System.Windows.Forms.Label();
        lblResultados.Text = "Resultados:";
        lblResultados.ForeColor = Color.White;
        lblResultados.Font = new Font("Arial", 12, FontStyle.Bold);
        lblResultados.Location = new Point(30, 125);
        lblResultados.AutoSize = true;
        this.Controls.Add(lblResultados);

        // Lista ordenada: la que tiene menos cartas va primero
        ListBox lstResultados = new ListBox();
        lstResultados.Font = new Font("Arial", 12);
        lstResultados.Location = new Point(30, 155);
        lstResultados.Width = 385;
        lstResultados.Height = 150;
        int lugar = 1;
        foreach (JugadoraEstado j in estado.Jugadoras.OrderBy(j => j.NumCartas))
        {
            string texto = $"{lugar}. {j.Nombre} – {j.NumCartas} carta(s)";
            if (j.Id == idGanadora) texto += "  ★ Ganadora";
            if (j.Id == estado.MiId) texto += "  (tú)";
            lstResultados.Items.Add(texto);
            lugar++;
        }
        this.Controls.Add(lstResultados);

        Button btnSalir = new Button();
        btnSalir.Text = "Salir";
        btnSalir.Font = new Font("Arial", 11, FontStyle.Bold);
        btnSalir.BackColor = Color.White;
        btnSalir.Location = new Point(295, 320);
        btnSalir.Width = 120;
        btnSalir.Height = 38;
        btnSalir.Click += (s, e) => this.Close();
        this.Controls.Add(btnSalir);
        this.AcceptButton = btnSalir;
    }
}
