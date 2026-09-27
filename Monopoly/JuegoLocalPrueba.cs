namespace Monopoly;

// ============================================================================
// MODULO DE PRUEBA LOCAL INTERACTIVO PARA MONOPOLY
// Permite simular una partida completa por consola mediante entrada de usuario (inputs),
// probando toda la logica del Banco, Tablero, Jugadores y Propiedades antes de conectar TCP.
// ============================================================================
public static class JuegoLocalPrueba
{
    public static void IniciarJuego()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("================================================================================");
        Console.WriteLine("                BIENVENIDO A MONOPOLY - MODO LOCAL DE PRUEBA                   ");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        Banco banco = new Banco();

        // 1. Registro de jugadores
        Console.Write("\n¿Cuantos jugadores participaran? (2 a 4): ");
        string? inputCantidad = Console.ReadLine();
        if (!int.TryParse(inputCantidad, out int cantidadJugadores) || cantidadJugadores < 2 || cantidadJugadores > 4)
        {
            cantidadJugadores = 2;
            Console.WriteLine("Valor invalido. Se configuraran 2 jugadores por defecto.");
        }

        for (int i = 1; i <= cantidadJugadores; i++)
        {
            Console.Write($"Ingrese el nombre del Jugador {i}: ");
            string? nombre = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombre = $"Jugador {i}";
            }
            Jugador nuevo = new Jugador(i, nombre, banco.getTableroJuego().getHead());
            nuevo.setSaldo(1500); // Saldo inicial estandar de Monopoly
            banco.registrarJugador(nuevo);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n¡Partida inicializada con exito! Todos los jugadores inician en Salida con $1500.\n");
        Console.ResetColor();

        bool juegoActivo = true;

        // Bucle principal de turnos
        while (juegoActivo)
        {
            Jugador jugadorActual = banco.getJugadorActual();

            if (jugadorActual == null || !jugadorActual.isActivo())
            {
                banco.ProcesarFinTurno();
                continue;
            }

            // Verificar si queda solo un jugador con vida (ganador)
            Jugador? ganador = banco.verificarGanador();
            if (ganador != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n================================================================================");
                Console.WriteLine($"         ¡¡¡EL JUEGO HA TERMINADO!!! GANADOR: {ganador.getNombre()}            ");
                Console.WriteLine($"         Patrimonio final en efectivo: ${ganador.getSaldo()}                    ");
                Console.WriteLine("================================================================================\n");
                Console.ResetColor();
                break;
            }

            bool turnoTerminado = false;
            bool yaTiroDados = false;

            while (!turnoTerminado && jugadorActual.isActivo())
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine($" TURNO DE: {jugadorActual.getNombre()} (ID: {jugadorActual.getID()}) | Saldo: ${jugadorActual.getSaldo()} | Ubicacion: {jugadorActual.getNodoActual().getCasilla().getNombre()} (ID: {jugadorActual.getNodoActual().getCasilla().getIdCasilla()})");
                if (jugadorActual.getEstaEncarcelado())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" >> ESTAS ENCARCELADO (Turnos de sancion restantes: {jugadorActual.getTurnosPerdidos()}) <<");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.ResetColor();

                Console.WriteLine("1. Tirar Dados y Mover");
                Console.WriteLine("2. Comprar Propiedad Actual (si esta disponible)");
                Console.WriteLine("3. Construir Casa / Hotel");
                Console.WriteLine("4. Vender Casa / Hotel");
                Console.WriteLine("5. Hipotecar Propiedad");
                Console.WriteLine("6. Deshipotecar Propiedad");
                Console.WriteLine("7. Intercambiar (Trade) con otro Jugador");
                Console.WriteLine("8. Ver Mis Propiedades");
                Console.WriteLine("9. Ver Estado General del Tablero");
                Console.WriteLine("10. Terminar Turno");
                Console.WriteLine("0. Salir de la Partida");
                Console.Write("\nSeleccione una opcion: ");

                string? opcion = Console.ReadLine();
                Console.WriteLine();

                switch (opcion)
                {
                    case "1":
                        if (yaTiroDados)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Ya has lanzado los dados en este turno. Termina tu turno o realiza compras/mejoras.");
                            Console.ResetColor();
                            break;
                        }

                        (int d1, int d2) = banco.TirarDadosDetallado();
                        int sumaDados = d1 + d2;
                        Console.WriteLine($"Lanzaste los dados: [{d1}] + [{d2}] = {sumaDados} pasos.");

                        int saldoAntes = jugadorActual.getSaldo();
                        bool resultadoTirada = banco.ProcesarTirada(jugadorActual, sumaDados);
                        yaTiroDados = true;

                        if (jugadorActual.getSaldo() > saldoAntes && jugadorActual.getNodoActual().getCasilla().getIdCasilla() != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("¡Cruzaste por la Casilla de Salida! Cobraste +$200 del Banco.");
                            Console.ResetColor();
                        }

                        Casilla casillaDestino = jugadorActual.getNodoActual().getCasilla();
                        Console.WriteLine($"Avanzaste hasta: {casillaDestino.getNombre()} (ID: {casillaDestino.getIdCasilla()})");

                        if (!resultadoTirada)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("¡No pudiste pagar la deuda de esta casilla y has caido en BANCARROTA!");
                            Console.ResetColor();
                            turnoTerminado = true;
                            break;
                        }

                        // Notificar si cayo en propiedad disponible
                        if (casillaDestino is Propiedad prop && !prop.tieneDuenio())
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"La propiedad '{prop.getNombre()}' no tiene duenio. Precio: ${prop.getPrecioDeCompra()}.");
                            Console.WriteLine("Puedes usar la opcion 2 para comprarla ahora mismo.");
                            Console.ResetColor();
                        }
                        break;

                    case "2":
                        Casilla cActual = jugadorActual.getNodoActual().getCasilla();
                        if (cActual is Propiedad propiedadAComprar)
                        {
                            if (propiedadAComprar.tieneDuenio())
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Esta propiedad ya le pertenece a: {propiedadAComprar.getDuenio()?.getNombre()}");
                                Console.ResetColor();
                            }
                            else if (jugadorActual.getSaldo() < propiedadAComprar.getPrecioDeCompra())
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Saldo insuficiente. Necesitas ${propiedadAComprar.getPrecioDeCompra()} pero tienes ${jugadorActual.getSaldo()}.");
                                Console.ResetColor();
                            }
                            else
                            {
                                bool exito = banco.ProcesarCompra(jugadorActual);
                                if (exito)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"¡Felicidades! Has adquirido '{propiedadAComprar.getNombre()}' por ${propiedadAComprar.getPrecioDeCompra()}.");
                                    Console.ResetColor();
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("La casilla actual no es una propiedad comprable.");
                        }
                        break;

                    case "3":
                        ConstruirCasaMenu(banco, jugadorActual);
                        break;

                    case "4":
                        VenderCasaMenu(banco, jugadorActual);
                        break;

                    case "5":
                        HipotecarMenu(banco, jugadorActual);
                        break;

                    case "6":
                        DeshipotecarMenu(banco, jugadorActual);
                        break;

                    case "7":
                        IntercambioMenu(banco, jugadorActual);
                        break;

                    case "8":
                        MostrarPropiedadesJugador(jugadorActual);
                        break;

                    case "9":
                        Console.WriteLine(banco.ObtenerEstado());
                        break;

                    case "10":
                        if (!yaTiroDados && !jugadorActual.getEstaEncarcelado())
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Debes lanzar los dados antes de terminar tu turno.");
                            Console.ResetColor();
                        }
                        else
                        {
                            banco.ProcesarFinTurno();
                            turnoTerminado = true;
                            Console.WriteLine("Turno finalizado.");
                        }
                        break;

                    case "0":
                        Console.WriteLine("Saliendo de la partida...");
                        juegoActivo = false;
                        turnoTerminado = true;
                        break;

                    default:
                        Console.WriteLine("Opcion no valida.");
                        break;
                }

                Console.WriteLine("\nPresione cualquier tecla para continuar...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    // Submenu para construccion de casas u hoteles
    private static void ConstruirCasaMenu(Banco banco, Jugador jugador)
    {
        Console.WriteLine("=== CONSTRUIR CASAS / HOTELES ===");
        NodoPropiedad? actual = jugador.getPropiedades().getHead();
        if (actual == null)
        {
            Console.WriteLine("No posees propiedades.");
            return;
        }

        int index = 1;
        while (actual != null)
        {
            Propiedad p = actual.getPropiedad();
            Console.WriteLine($"{index}. {p.getNombre()} (Grupo: {p.getGrupo()}) | Casas: {p.getCasasPuestas()} | Hotel: {p.getTieneHotel()} | Costo de mejora: ${p.getCostoCasa()}");
            actual = actual.getSiguiente();
            index++;
        }

        Console.Write("\nIngrese el numero de propiedad a mejorar (0 para cancelar): ");
        if (int.TryParse(Console.ReadLine(), out int seleccion) && seleccion > 0 && seleccion < index)
        {
            actual = jugador.getPropiedades().getHead();
            for (int i = 1; i < seleccion; i++)
            {
                actual = actual!.getSiguiente();
            }

            if (actual != null)
            {
                bool exito = banco.ProcesarConstruirCasa(jugador, actual.getPropiedad());
                if (exito)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"¡Mejora construida con exito en {actual.getPropiedad().getNombre()}!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No se pudo construir la mejora. Verifica si tienes el grupo completo, saldo suficiente o si no se cumple la regla uniforme.");
                    Console.ResetColor();
                }
            }
        }
    }

    // Submenu para vender casas
    private static void VenderCasaMenu(Banco banco, Jugador jugador)
    {
        Console.WriteLine("=== VENDER CASAS / HOTELES ===");
        NodoPropiedad? actual = jugador.getPropiedades().getHead();
        if (actual == null)
        {
            Console.WriteLine("No posees propiedades.");
            return;
        }

        int index = 1;
        while (actual != null)
        {
            Propiedad p = actual.getPropiedad();
            Console.WriteLine($"{index}. {p.getNombre()} | Casas: {p.getCasasPuestas()} | Hotel: {p.getTieneHotel()} | Reembolso por venta: ${p.getCostoCasa() / 2}");
            actual = actual.getSiguiente();
            index++;
        }

        Console.Write("\nIngrese el numero de propiedad donde vender edificacion (0 para cancelar): ");
        if (int.TryParse(Console.ReadLine(), out int seleccion) && seleccion > 0 && seleccion < index)
        {
            actual = jugador.getPropiedades().getHead();
            for (int i = 1; i < seleccion; i++)
            {
                actual = actual!.getSiguiente();
            }

            if (actual != null)
            {
                bool exito = banco.ProcesarVenderCasa(jugador, actual.getPropiedad());
                if (exito)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Edificacion vendida con exito. Saldo actual: ${jugador.getSaldo()}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No se pudo vender la edificacion en esta propiedad.");
                    Console.ResetColor();
                }
            }
        }
    }

    // Submenu para hipotecas
    private static void HipotecarMenu(Banco banco, Jugador jugador)
    {
        Console.WriteLine("=== HIPOTECAR PROPIEDADES ===");
        NodoPropiedad? actual = jugador.getPropiedades().getHead();
        if (actual == null)
        {
            Console.WriteLine("No posees propiedades.");
            return;
        }

        int index = 1;
        while (actual != null)
        {
            Propiedad p = actual.getPropiedad();
            Console.WriteLine($"{index}. {p.getNombre()} | Hipotecada: {p.getEstaHipotecada()} | Valor hipoteca: +${p.getPrecioDeCompra() / 2}");
            actual = actual.getSiguiente();
            index++;
        }

        Console.Write("\nSeleccione la propiedad a hipotecar (0 para cancelar): ");
        if (int.TryParse(Console.ReadLine(), out int seleccion) && seleccion > 0 && seleccion < index)
        {
            actual = jugador.getPropiedades().getHead();
            for (int i = 1; i < seleccion; i++)
            {
                actual = actual!.getSiguiente();
            }

            if (actual != null)
            {
                bool exito = banco.ProcesarHipotecar(jugador, actual.getPropiedad());
                if (exito)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Propiedad hipotecada con exito. Recibiste +${actual.getPropiedad().getPrecioDeCompra() / 2}. Saldo actual: ${jugador.getSaldo()}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No se puede hipotecar (ya esta hipotecada o tiene casas construidas).");
                    Console.ResetColor();
                }
            }
        }
    }

    // Submenu para deshipotecar
    private static void DeshipotecarMenu(Banco banco, Jugador jugador)
    {
        Console.WriteLine("=== DESHIPOTECAR PROPIEDADES ===");
        NodoPropiedad? actual = jugador.getPropiedades().getHead();
        if (actual == null)
        {
            Console.WriteLine("No posees propiedades.");
            return;
        }

        int index = 1;
        while (actual != null)
        {
            Propiedad p = actual.getPropiedad();
            int costoDeshipoteca = (p.getPrecioDeCompra() / 2) + (p.getPrecioDeCompra() / 10);
            Console.WriteLine($"{index}. {p.getNombre()} | Hipotecada: {p.getEstaHipotecada()} | Costo para deshipotecar: -${costoDeshipoteca}");
            actual = actual.getSiguiente();
            index++;
        }

        Console.Write("\nSeleccione la propiedad a deshipotecar (0 para cancelar): ");
        if (int.TryParse(Console.ReadLine(), out int seleccion) && seleccion > 0 && seleccion < index)
        {
            actual = jugador.getPropiedades().getHead();
            for (int i = 1; i < seleccion; i++)
            {
                actual = actual!.getSiguiente();
            }

            if (actual != null)
            {
                bool exito = banco.ProcesarDeshipotecar(jugador, actual.getPropiedad());
                if (exito)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"¡Propiedad deshipotecada con exito! Ahora vuelve a cobrar alquiler.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No se pudo deshipotecar. Verifica si esta hipotecada y si posees saldo suficiente.");
                    Console.ResetColor();
                }
            }
        }
    }

    // Listado de propiedades del jugador
    private static void MostrarPropiedadesJugador(Jugador jugador)
    {
        Console.WriteLine($"=== PROPIEDADES DE {jugador.getNombre()} ===");
        NodoPropiedad? actual = jugador.getPropiedades().getHead();
        if (actual == null)
        {
            Console.WriteLine("No posees propiedades en este momento.");
            return;
        }

        while (actual != null)
        {
            Propiedad p = actual.getPropiedad();
            Console.WriteLine($"- {p.getNombre()} | Grupo: {p.getGrupo()} | Casas: {p.getCasasPuestas()} | Hotel: {p.getTieneHotel()} | Hipotecada: {p.getEstaHipotecada()} | Alquiler actual: ${p.getAlquiler()}");
            actual = actual.getSiguiente();
        }
        Console.WriteLine($"Valor total del patrimonio en propiedades: ${jugador.getPropiedades().calcularValorTotal()}");
    }

    // Submenu para realizar intercambios entre jugadores utilizando instancias temporales de ListaPropiedades
    private static void IntercambioMenu(Banco banco, Jugador jugadorActual)
    {
        Console.WriteLine("=== INTERCAMBIO DE PROPIEDADES Y DINERO (TRADE) ===");

        // 1. Seleccionar el jugador rival con quien intercambiar
        Console.WriteLine("\nJugadores disponibles para negociar:");
        // Recorremos la cola circular de turnos para listar a los rivales
        ListaTurnos cola = banco.getTurnosJugadores();
        int totalJugadores = cola.getCantidadJugadores();

        if (totalJugadores <= 1)
        {
            Console.WriteLine("No hay suficientes jugadores para realizar un intercambio.");
            return;
        }

        // Buscar al rival avanzando en la cola
        Jugador? rival = null;
        for (int i = 0; i < totalJugadores; i++)
        {
            Jugador j = cola.getTurnoActual();
            if (j != jugadorActual && j.isActivo())
            {
                Console.WriteLine($"- Jugador ID {j.getID()}: {j.getNombre()} (Saldo: ${j.getSaldo()}, Propiedades: {j.getPropiedades().getSize()})");
            }
            cola.avanzarTurno();
        }

        Console.Write("\nIngrese el ID del jugador con quien desea comerciar (0 para cancelar): ");
        if (!int.TryParse(Console.ReadLine(), out int idRival) || idRival <= 0)
        {
            return;
        }

        for (int i = 0; i < totalJugadores; i++)
        {
            Jugador j = cola.getTurnoActual();
            if (j.getID() == idRival && j != jugadorActual && j.isActivo())
            {
                rival = j;
            }
            cola.avanzarTurno();
        }

        if (rival == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Jugador rival no encontrado o no valido.");
            Console.ResetColor();
            return;
        }

        // 2. Llenar la ListaPropiedades temporal del Jugador A
        ListaPropiedades ofrecidasA = new ListaPropiedades();
        Console.WriteLine($"\n--- PROPIEDADES QUE OFRECE {jugadorActual.getNombre()} ---");
        NodoPropiedad? actualA = jugadorActual.getPropiedades().getHead();
        if (actualA == null)
        {
            Console.WriteLine("(No posees propiedades para ofrecer)");
        }
        else
        {
            while (actualA != null)
            {
                Propiedad p = actualA.getPropiedad();
                Console.Write($"¿Deseas incluir '{p.getNombre()}' (${p.getPrecioDeCompra()}) en la oferta? (S/N): ");
                string? resp = Console.ReadLine();
                if (resp != null && (resp.Trim().ToUpper() == "S" || resp.Trim().ToUpper() == "SI"))
                {
                    ofrecidasA.agregarPropiedad(p);
                }
                actualA = actualA.getSiguiente();
            }
        }

        Console.Write($"\nIngrese la cantidad de dinero que ofrece {jugadorActual.getNombre()} (Tienes ${jugadorActual.getSaldo()}): $");
        if (!int.TryParse(Console.ReadLine(), out int dineroA) || dineroA < 0) dineroA = 0;

        // 3. Llenar la ListaPropiedades temporal del Jugador B (Rival)
        ListaPropiedades ofrecidasB = new ListaPropiedades();
        Console.WriteLine($"\n--- PROPIEDADES QUE PIDES DE {rival.getNombre()} ---");
        NodoPropiedad? actualB = rival.getPropiedades().getHead();
        if (actualB == null)
        {
            Console.WriteLine($"(El jugador {rival.getNombre()} no posee propiedades)");
        }
        else
        {
            while (actualB != null)
            {
                Propiedad p = actualB.getPropiedad();
                Console.Write($"¿Deseas pedir '{p.getNombre()}' (${p.getPrecioDeCompra()})? (S/N): ");
                string? resp = Console.ReadLine();
                if (resp != null && (resp.Trim().ToUpper() == "S" || resp.Trim().ToUpper() == "SI"))
                {
                    ofrecidasB.agregarPropiedad(p);
                }
                actualB = actualB.getSiguiente();
            }
        }

        Console.Write($"\nIngrese la cantidad de dinero que solicita a {rival.getNombre()} (Tiene ${rival.getSaldo()}): $");
        if (!int.TryParse(Console.ReadLine(), out int dineroB) || dineroB < 0) dineroB = 0;

        // 4. Confirmacion de la propuesta por parte del rival
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n==================================================");
        Console.WriteLine($"PROPUESTA DE INTERCAMBIO PARA: {rival.getNombre()}");
        Console.WriteLine($"- Recibes de {jugadorActual.getNombre()}: {ofrecidasA.getSize()} propiedades y ${dineroA}");
        Console.WriteLine($"- Entregas a {jugadorActual.getNombre()}: {ofrecidasB.getSize()} propiedades y ${dineroB}");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.Write($"\n{rival.getNombre()}, ¿Aceptas este intercambio? (S/N): ");
        string? confirmacion = Console.ReadLine();
        if (confirmacion == null || (confirmacion.Trim().ToUpper() != "S" && confirmacion.Trim().ToUpper() != "SI"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("El intercambio fue rechazado por el otro jugador.");
            Console.ResetColor();
            return;
        }

        // 5. Ejecutar intercambio a traves del Banco
        bool exito = banco.ProcesarIntercambio(jugadorActual, ofrecidasA, dineroA, rival, ofrecidasB, dineroB);
        if (exito)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n¡¡¡INTERCAMBIO REALIZADO CON EXITO!!!");
            Console.WriteLine($"Nuevo saldo de {jugadorActual.getNombre()}: ${jugadorActual.getSaldo()}");
            Console.WriteLine($"Nuevo saldo de {rival.getNombre()}: ${rival.getSaldo()}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("El intercambio no pudo completarse. Verifica que ninguna de las propiedades tenga casas/hoteles construidos o que los fondos sean validos.");
            Console.ResetColor();
        }
    }
}

