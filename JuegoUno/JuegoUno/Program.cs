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

    EstadoJuego estadoActual = null;   // El último estado que mandó el servidor
    bool finMostrado = false;          // Para mostrar la pantalla final una sola vez

    conexion.MensajeRecibido += (mensaje) =>
    {
        try
        {
            string tipo = mensaje.GetProperty("tipo").GetString();

            if (tipo == "estado")
            {
                EstadoJuego estado = EstadoJuego.DesdeJson(mensaje, miId);
                formJuego.EnPantalla(() =>
                {
                    estadoActual = estado;
                    formJuego.MostrarEstado(estado);

                    // Fin de la partida: se muestra la pantalla final y se cierra el juego
                    if (estado.Ganadora.HasValue && !finMostrado)
                    {
                        finMostrado = true;
                        using (FormFinPartida fin = new FormFinPartida(estado))
                        {
                            fin.ShowDialog(formJuego);
                        }
                        formJuego.Close();
                    }
                });
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

    // Si se cae el servidor, se avisa y se cierra el juego
    conexion.Desconectado += () =>
        formJuego.EnPantalla(() =>
        {
            if (finMostrado) return;
            MessageBox.Show("Se perdió la conexión con el servidor. El juego se cerrará.", "UNO");
            formJuego.Close();
        });

    // Por ahora solo se muestra la partida. Tirar, robar y UNO se programan en los siguientes commits.
    // Tirar carta: se manda al servidor y él decide si es válida
    formJuego.CartaClickeada += async (indice) =>
    {
        if (estadoActual == null) return;

        if (estadoActual.Turno != estadoActual.MiId)
        {
            formJuego.MostrarAviso("⏳ No es tu turno.");
            return;
        }

        if (indice < 0 || indice >= estadoActual.MiMano.Count) return;
        Carta carta = estadoActual.MiMano[indice];

        // Si es Comodín o +4, primero se elige el color
        string color = null;
        if (carta.Color == "Ninguno")
        {
            using (FormColorPicker selector = new FormColorPicker())
            {
                selector.ShowDialog(formJuego);
                color = selector.ColorElegido;
            }

            if (color == null)
            {
                formJuego.MostrarAviso("Elige un color para tirar el comodín.");
                return;
            }
        }

        try
        {
            await conexion.EnviarAsync(new { tipo = "tirar", indice = indice, color = color });
        }
        catch
        {
            formJuego.MostrarAviso("❌ No se pudo enviar la jugada.");
        }
    };

    // Robar carta: se pide al servidor
    formJuego.RobarClickeado += async () =>
    {
        if (estadoActual == null) return;

        if (estadoActual.Turno != estadoActual.MiId)
        {
            formJuego.MostrarAviso("⏳ No es tu turno.");
            return;
        }

        try
        {
            await conexion.EnviarAsync(new { tipo = "robar" });
        }
        catch
        {
            formJuego.MostrarAviso("❌ No se pudo enviar la jugada.");
        }
    };

    // Cantar UNO: se avisa al servidor y él decide si cuenta
    formJuego.UnoClickeado += async () =>
    {
        if (estadoActual == null) return;

        if (estadoActual.UnoPendiente != estadoActual.MiId)
        {
            formJuego.MostrarAviso("Todavía no tienes que cantar UNO.");
            return;
        }

        try
        {
            await conexion.EnviarAsync(new { tipo = "uno" });
        }
        catch
        {
            formJuego.MostrarAviso("❌ No se pudo enviar.");
        }
    };

    // Cuando la ventana ya está abierta, le pide al servidor su estado
    formJuego.Shown += async (s, e) => await conexion.EnviarAsync(new { tipo = "listo" });

    Application.Run(formJuego);
    conexion.Cerrar();
}
catch (Exception ex)
{
    MessageBox.Show("Error: " + ex.Message);
}