using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class FormSetup : Form
{
    private System.Windows.Forms.Label lblCantidad;
    private NumericUpDown numCantidad;
    private Button btnContinuar;
    private Panel panelNombres;
    private List<TextBox> inputsNombres = new List<TextBox>();

    public List<string> NombresJugadores { get; private set; } = new List<string>();

    public FormSetup()
    {
        this.Text = "Configurar jugadores";
        this.Width = 400;
        this.Height = 450;
        this.BackColor = Color.DarkGreen;
        this.StartPosition = FormStartPosition.CenterScreen;

        lblCantidad = new System.Windows.Forms.Label();
        lblCantidad.Text = "¿Cuántos jugadores?";
        lblCantidad.ForeColor = Color.White;
        lblCantidad.Font = new Font("Arial", 12, FontStyle.Bold);
        lblCantidad.Location = new Point(20, 20);
        lblCantidad.AutoSize = true;
        this.Controls.Add(lblCantidad);

        numCantidad = new NumericUpDown();
        numCantidad.Minimum = 2;
        numCantidad.Maximum = 8;
        numCantidad.Value = 2;
        numCantidad.Location = new Point(220, 18);
        numCantidad.Width = 60;
        numCantidad.ValueChanged += NumCantidad_ValueChanged;
        this.Controls.Add(numCantidad);

        panelNombres = new Panel();
        panelNombres.Location = new Point(20, 60);
        panelNombres.Width = 340;
        panelNombres.Height = 300;
        panelNombres.AutoScroll = true;
        this.Controls.Add(panelNombres);

        btnContinuar = new Button();
        btnContinuar.Text = "Empezar juego";
        btnContinuar.Location = new Point(130, 370);
        btnContinuar.Width = 140;
        btnContinuar.Click += BtnContinuar_Click;
        this.Controls.Add(btnContinuar);

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
            lbl.Location = new Point(0, i * 35);
            lbl.AutoSize = true;
            panelNombres.Controls.Add(lbl);

            TextBox txt = new TextBox();
            txt.Location = new Point(100, i * 35);
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