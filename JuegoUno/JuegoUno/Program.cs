using System;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();
Application.Run(new FormJuego());

/*using JuegoUno;
using MySqlConnector;
using System.Collections.Generic;

string connectionString = File.ReadAllText("config.txt").Trim();

using var connection = new MySqlConnection(connectionString);

try
{
    connection.Open();
    Console.WriteLine("¡Conexión exitosa a juego_uno! 🎉\n");

    //Numero de jugadores
    Console.Write("Numero de jugadores: ");
    int numJugadores = int.Parse(Console.ReadLine());

    List<int> idsJugadores = new List<int>();
    List<string> nombresJugadores = new List<string>();

    for (int i=1; i<=numJugadores; i++)
    {
        Console.Write($"Nombre del jugador {i}: ");
        string nombreJugador = Console.ReadLine();

        //Si el jugador existe
        int idJugador;
        string checkQuery = "SELECT id FROM Jugadores WHERE nombre = @nombre";
        using (var checkCmd = new MySqlCommand(checkQuery, connection))
        {
            checkCmd.Parameters.AddWithValue("@nombre", nombreJugador);
            object resultado = checkCmd.ExecuteScalar();

            if(resultado!=null)
            {
                //Si ya existe tomamos su id
                idJugador = Convert.ToInt32(resultado);
                Console.WriteLine($"  Jugador '{nombreJugador}' ya existía (ID: {idJugador}).\n");
            }
            else
            {
                // Si no existe lo insertamos
                string insertQuery = "INSERT INTO Jugadores (nombre) VALUES (@nombre)";
                using (var insertCmd = new MySqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@nombre", nombreJugador);
                    insertCmd.ExecuteNonQuery();
                }

                using (var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", connection))
                {
                    idJugador = Convert.ToInt32(idCmd.ExecuteScalar());
                }

                Console.WriteLine($"  Jugador '{nombreJugador}' agregado (ID: {idJugador}).\n");
            }
        }

        idsJugadores.Add(idJugador);
        nombresJugadores.Add(nombreJugador);
    }

    // Crear y barajar el mazo
    Mazo mazo = new Mazo();
    mazo.Barajar();

    // Crear la lista de jugadores con sus cartas
    List<Jugador> jugadores = new List<Jugador>();
    for (int i = 0; i < idsJugadores.Count; i++)
    {
        Jugador j = new Jugador
        {
            Id = idsJugadores[i],
            Nombre = nombresJugadores[i]
        };

        // Repartir 7 cartas a cada jugador 
        for (int c = 0; c < 7; c++)
        {
            j.Mano.Add(mazo.RobarCarta());
        }

        jugadores.Add(j);
    }

    // Sacar la primera carta para iniciar la pila de descarte
    Carta cartaActual = mazo.RobarCarta();

    // Si la primera carta es un comodín, el primer jugador elige el color
    if (cartaActual.Color == "Ninguno")
    {
        Console.WriteLine($"\nLa carta inicial es un comodín: {cartaActual}");
        Console.Write($"{jugadores[0].Nombre}, elige el color inicial (Rojo, Amarillo, Verde, Azul): ");
        string colorInicial = Console.ReadLine();
        cartaActual.Color = colorInicial;
    }

    Console.WriteLine($"\nCarta inicial: {cartaActual}\n");

    // Mostrar la mano de cada jugador 
    foreach (var j in jugadores)
    {
        j.MostrarMano();
        Console.WriteLine();
    }


    // Registrar una partida nueva
    string insertPartida = "INSERT INTO Partidas (fecha) VALUES (NOW())";
    using (var cmdPartida = new MySqlCommand(insertPartida, connection))
    {
        cmdPartida.ExecuteNonQuery();
    }

    // Obtener el id de esa partida recién creada

    ulong idPartida;
    using (var cmdId = new MySqlCommand("SELECT LAST_INSERT_ID()", connection))
    {
        idPartida = (ulong)cmdId.ExecuteScalar();
    }

    //Turno
    int turnoActual = 0;
    int direccion = 1; // 1 = sentido normal, -1 = sentido invertido

    // Función para registrar un movimiento en el log
    void RegistrarLog(ulong idPartidaLog, int idJugadorLog, string movimiento)
    {
        string logQuery = "INSERT INTO LogJuego (id_partida, id_jugador, movimiento) VALUES (@partida, @jugador, @movimiento)";
        using (var logCmd = new MySqlCommand(logQuery, connection))
        {
            logCmd.Parameters.AddWithValue("@partida", idPartidaLog);
            logCmd.Parameters.AddWithValue("@jugador", idJugadorLog);
            logCmd.Parameters.AddWithValue("@movimiento", movimiento);
            logCmd.ExecuteNonQuery();
        }
    }

    while (true)
    {
        Jugador jugadorEnTurno = jugadores[turnoActual];
        Console.WriteLine($"Turno de {jugadorEnTurno.Nombre}");
        Console.WriteLine($"Carta actual: {cartaActual}");
        jugadorEnTurno.MostrarMano();

        Console.Write("Elige el número de la carta a tirar (o -1 para tomar una nueva carta): ");
        int eleccion = int.Parse(Console.ReadLine());

        if (eleccion == -1)
        {
            Carta nueva = mazo.RobarCarta();
            jugadorEnTurno.Mano.Add(nueva);
            Console.WriteLine($"{jugadorEnTurno.Nombre} tomo una carta.\n");
            RegistrarLog(idPartida, jugadorEnTurno.Id, "Robó una carta");

            turnoActual = (turnoActual + direccion + jugadores.Count) % jugadores.Count;
            continue;
        }
        else if (eleccion >= 0 && eleccion < jugadorEnTurno.Mano.Count)
        {
            Carta cartaElegida = jugadorEnTurno.Mano[eleccion];

            if (cartaElegida.EsValidaSobre(cartaActual))
            {
                jugadorEnTurno.Mano.RemoveAt(eleccion);

                // Si es comodín (Comodin o Mas4), preguntar de qué color sigue
                if (cartaElegida.Color == "Ninguno")
                {
                    Console.Write("Elige el nuevo color (Rojo, Amarillo, Verde, Azul): ");
                    string nuevoColor = Console.ReadLine();
                    cartaElegida.Color = nuevoColor;
                }

                cartaActual = cartaElegida;
                Console.WriteLine($"{jugadorEnTurno.Nombre} tiró {cartaElegida}\n");

                RegistrarLog(idPartida, jugadorEnTurno.Id, $"Tiró {cartaElegida}");

                if (jugadorEnTurno.Mano.Count == 0)
                {
                    Console.WriteLine($" ¡{jugadorEnTurno.Nombre} ganó la partida! ");
                    RegistrarLog(idPartida, jugadorEnTurno.Id, "Ganó la partida");

                    // Guardar resultado de cada jugador en HistorialPartidas
                    foreach (var j in jugadores)
                    {
                        string resultado = (j.Id == jugadorEnTurno.Id) ? "Ganada" : "Perdida";
                        string insertHistorial = "INSERT INTO HistorialPartidas (id_partida, id_jugador, resultado) VALUES (@partida, @jugador, @resultado)";
                        using (var cmdHistorial = new MySqlCommand(insertHistorial, connection))
                        {
                            cmdHistorial.Parameters.AddWithValue("@partida", idPartida);
                            cmdHistorial.Parameters.AddWithValue("@jugador", j.Id);
                            cmdHistorial.Parameters.AddWithValue("@resultado", resultado);
                            cmdHistorial.ExecuteNonQuery();
                        }
                    }

                    break;
                }

                // Calcular quién sigue (antes de aplicar efectos)
                int siguienteTurno = (turnoActual + direccion + jugadores.Count) % jugadores.Count;

                // Aplicar efectos especiales
                switch (cartaElegida.Tipo)
                {
                    case "Salta":
                        Console.WriteLine($"{jugadores[siguienteTurno].Nombre} pierde su turno.\n");
                        siguienteTurno = (siguienteTurno + direccion + jugadores.Count) % jugadores.Count;
                        break;

                    case "Reversa":
                        direccion *= -1;
                        Console.WriteLine("Se invierte el sentido del juego.\n");
                        siguienteTurno = (turnoActual + direccion + jugadores.Count) % jugadores.Count;
                        break;

                    case "Mas2":
                        Jugador victimaMas2 = jugadores[siguienteTurno];
                        for (int k = 0; k < 2; k++) victimaMas2.Mano.Add(mazo.RobarCarta());
                        Console.WriteLine($"{victimaMas2.Nombre} toma 2 cartas y pierde su turno.\n");
                        siguienteTurno = (siguienteTurno + direccion + jugadores.Count) % jugadores.Count;
                        break;

                    case "Mas4":
                        Jugador victimaMas4 = jugadores[siguienteTurno];
                        for (int k = 0; k < 4; k++) victimaMas4.Mano.Add(mazo.RobarCarta());
                        Console.WriteLine($"{victimaMas4.Nombre} toma 4 cartas y pierde su turno.\n");
                        siguienteTurno = (siguienteTurno + direccion + jugadores.Count) % jugadores.Count;
                        break;
                }

                turnoActual = siguienteTurno;
                continue; // ya avanzamos el turno manualmente, saltamos el avance normal de abajo
            }
            else
            {
                Console.WriteLine("Carta no válida, intenta de nuevo.\n");
                continue; // no avanza el turno, vuelve a preguntar
            }
        }
        else
        {
            Console.WriteLine("Opción inválida.\n");
            continue;
        }
    }

    
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}

Console.WriteLine("\nPresiona una tecla para salir...");
Console.ReadKey();*/