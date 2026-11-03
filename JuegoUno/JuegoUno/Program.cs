using JuegoUno;
using System;
using System.Text.Json;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();

try
{
    // 1) Menú principal (Jugar / Salir)
    using (FormSetup menu = new FormSetup())
    {
        if (menu.ShowDialog() != DialogResult.OK)
        {
            return;
        }
    }

    // 2) Conectarse al servidor
    ClienteSocket conexion = new ClienteSocket();

    using (FormConexion formConexion = new FormConexion(conexion))
    {
        if (formConexion.ShowDialog() != DialogResult.OK)
        {
            return;
        }
    }

    // 3) Sala de espera
    int miId;

    using (FormSalaEspera sala = new FormSalaEspera(conexion))
    {
        if (sala.ShowDialog() != DialogResult.OK)
        {
            return;
        }
        miId = sala.MiId;
    }

    // 4) Juego: el servidor manda el estado y esta pantalla solo lo dibuja
    FormJuego formJuego = new FormJuego();

    conexion.MensajeRecibido += (mensaje) =>
    {
        try
        {
            string tipo = mensaje.GetProperty("tipo").GetString();

            if (tipo == "estado")
            {
                EstadoJuego estado = EstadoJuego.DesdeJson(mensaje, miId);
                formJuego.EnPantalla(() => formJuego.MostrarEstado(estado));
            }
            else if (tipo == "error")
            {
                string texto = mensaje.GetProperty("mensaje").GetString();
                formJuego.EnPantalla(() => formJuego.MostrarAviso("⚠️ " + texto));
            }
        }
        catch (Exception ex)
        {
            formJuego.EnPantalla(() => formJuego.MostrarAviso("Error al leer mensaje: " + ex.Message));
        }
    };

    conexion.Desconectado += () =>
        formJuego.EnPantalla(() => formJuego.MostrarAviso(" Se perdió la conexión con el servidor."));

    // Por ahora solo se muestra la partida. Tirar, robar y UNO se programan en los siguientes commits.
    formJuego.CartaClickeada += (indice) => formJuego.MostrarAviso("Tirar cartas se activa en el siguiente commit ");
    formJuego.RobarClickeado += () => formJuego.MostrarAviso("Robar se activa en un siguiente commit ");
    formJuego.UnoClickeado += () => formJuego.MostrarAviso("El botón UNO se activa en un siguiente commit ");

    // Cuando la ventana ya está abierta, le pide al servidor su estado
    formJuego.Shown += async (s, e) => await conexion.EnviarAsync(new { tipo = "listo" });

    Application.Run(formJuego);
    conexion.Cerrar();
}
catch (Exception ex)
{
    MessageBox.Show("Error: " + ex.Message);
}