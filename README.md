# Monopoly Distribuido — Algoritmos y Estructuras de Datos I

Sistema de juego Monopoly distribuido con arquitectura Cliente-Servidor (TCP Sockets), autoridad centralizada en el servidor, estructuras de datos dinámicas enlazadas propias y soporte tolerante a fallos para hardware embebido (Raspberry Pi Pico con lector RFID RC522 y Display multiplexado de 7 segmentos de 2 dígitos).

---

## 1. Arquitectura del Sistema

```
                          ┌─────────────────────────────────────┐
                          │         SERVIDOR MONOPOLY           │
                          │        (Autoridad Oficial)          │
                          │ - Tablero (Lista Circular Doble)    │
                          │ - Turnos (Lista Circular Simple)    │
                          │ - Transacciones (Lista Enlazada)    │
                          │ - Eventos (Mazo Circular)           │
                          │ - Archivo 'transacciones.txt'       │
                          └───────┬────────────────────┬────────┘
                                  │                    │
                    TCP / Sockets │                    │ USB Serial / Mock
                      Puerto 5000 │                    │ 115200 baud
                                  │                    │
                ┌─────────────────┴───────────────┐ ┌──┴───────────────────────┐
                │                                 │ │  ControladorHardware.cs  │
         ┌──────▼──────┐                   ┌──────▼─▼┐ - Detección no bloqueante│
         │  CLIENTE 1  │                   │ CLIENTE │ - Reconexión background │
         │ (Jugador 1) │                   │ (Jugador│ - RFID con fallback     │
         │ Consola CLI │                   │ Consola │ - 7 segmentos garantiz. │
         └─────────────┘                   └─────────┘└─────────┬──────────────┘
                                                                │
                                                       ┌────────▼────────┐
                                                       │  Raspberry Pi   │
                                                       │      Pico       │
                                                       │ - RFID RC522    │
                                                       │ - Display 7 Seg │
                                                       │ - LED estado    │
                                                       └─────────────────┘
```

### Principios Fundamentales
* **Servidor como Autoridad Central**: El cliente **nunca** modifica saldos, posiciones ni turnos directamente. Envía solicitudes (`TIRAR_DADOS`, `COMPRAR_PROPIEDAD`, etc.) y el servidor valida las reglas oficiales antes de aplicar cambios y difundirlos.
* **El Juego NO depende de la Raspberry**: Si la Raspberry Pi Pico no está conectada, se desconecta durante la partida o el puerto está ocupado (ej. por Thonny), el juego continúa de forma fluida e ininterrumpida.
* **Fallback Automático de RFID**: Se intenta leer la tarjeta RFID preferentemente (timeout de 2.5s). Si no hay tarjeta o falla el hardware, el servidor utiliza automáticamente el jugador en turno sin bloquear el sistema.
* **Display de 7 Segmentos**: Toda tirada de dados, posición en el tablero e ID de turno se envía al display de 7 segmentos de 2 dígitos (rango 00-99) sin comprometer el hilo de ejecución si el hardware no responde.

---

## 2. Estructuras de Datos Propias (Requisitos de Cátedra)

Todas las estructuras de datos dinámicas principales fueron implementadas manualmente sin depender de colecciones genéricas de .NET:

1. **Tablero (`Tablero.cs`, `NodoCasilla`)**:
   - **Lista Circular Doblemente Enlazada**.
   - Cada nodo apunta a `Siguiente` y `Anterior`. `Tail.Siguiente = Head` y `Head.Anterior = Tail`.
   - Permite movimiento paso a paso y detección del paso por la casilla de salida (+₡200).
2. **Rotación de Turnos (`Turnos.cs`, `NodoTurno`)**:
   - **Lista Circular Simplemente Enlazada**.
   - Administra el avance de turnos infinito entre los jugadores activos, omitiendo jugadores en bancarrota.
3. **Propiedades del Jugador (`ListaDePropiedades.cs`, `NodoPropiedad`)**:
   - **Lista Lineal Simplemente Enlazada**.
   - Almacena las casillas adquiridas por cada jugador, cálculo de valor patrimonial total y gestión de hipotecas.
4. **Historial de Transacciones (`Transaccion.cs`, `NodoTransaccion`)**:
   - **Lista Lineal Simplemente Enlazada**.
   - Registra ID único, fecha/hora, turno, tipo, origen, destino, monto y detalle.
   - Persistencia automática en disco en `transacciones.txt`.
5. **Mazo de Cartas de Suerte / Evento (`Eventos.cs`, `NodoEvento`)**:
   - **Cola Circular / Mazo Enlazado**.
   - Rotación infinita de cartas positivas y negativas al caer en casillas de evento.

---

## 3. Instrucciones de Compilación y Ejecución

El proyecto está configurado para .NET 10 (o .NET 8) y compila limpiamente en Linux, Windows y macOS.

### Compilar el proyecto
```bash
dotnet build Monopoly/Monopoly.csproj
```

### Ejecutar la Prueba Crítica de Demostración (Recomendada para la defensa)
Demuestra automáticamente el arranque sin hardware, conexión TCP de 2 clientes, tirada de dados, compra de propiedades, cobro de alquiler, fallback de RFID, actualización al 7 segmentos y archivo de transacciones:
```bash
dotnet run --project Monopoly/Monopoly.csproj -- --test
```

### Ejecutar Batería de Pruebas Unitarias (48/48 Pruebas de Algoritmos y Estructuras)
Valida enlaces dobles del tablero, enlaces simples de turnos, mazo circular, eliminación de propiedades, persistencia y polimorfismo:
```bash
dotnet run --project Monopoly/Monopoly.csproj -- --unit-tests
```

### Iniciar el Servidor de Juego (con Tablero Gráfico Web en vivo)
```bash
dotnet run --project Monopoly/Monopoly.csproj -- --server 5000
```
* Una vez iniciado el servidor, abra su navegador web en: **`http://localhost:8080`** (o `http://<IP_SERVIDOR>:8080`).
* Podrá ver el **tablero de 24 casillas en tiempo real**, las fichas de los jugadores moviéndose, las compras de propiedades, el display de 7 segmentos de la Pico y el feed de transacciones.

### Iniciar Clientes (desde la misma máquina o computadoras distintas en red)
```bash
dotnet run --project Monopoly/Monopoly.csproj -- --client 127.0.0.1 5000 Jugador1
```
*(Reemplazar `127.0.0.1` por la IP local del servidor si se juega desde otra PC).*

### Menú Interactivo (si se ejecuta sin parámetros)
```bash
dotnet run --project Monopoly/Monopoly.csproj
```

---

## 4. Protocolo de Comunicación TCP

Los mensajes viajan sobre sockets TCP como líneas de texto UTF-8 delimitadas por `\n` y campos separados por `|`:

| Comando (Cliente $\rightarrow$ Servidor) | Parámetros | Descripción |
|---|---|---|
| `CONECTAR` | `<nombre>` | Registra un nuevo jugador y le asigna saldo inicial (₡1500). |
| `TIRAR_DADOS` | — | Solicita RFID (con fallback), tira los 2 dados y avanza la posición. |
| `COMPRAR_PROPIEDAD` | — | Compra la propiedad de la casilla actual si está libre y hay saldo. |
| `NO_COMPRAR` | — | Rechaza la compra de la propiedad actual. |
| `TERMINAR_TURNO` | — | Concluye el turno actual y lo rota al siguiente jugador activo. |
| `CONSULTAR_ESTADO` | — | Obtiene el estado consolidado de la partida (jugadores, saldos, posiciones). |
| `CONSULTAR_TRANSACCIONES` | — | Consulta las últimas operaciones económicas oficiales. |
| `HIPOTECAR` | `<idCasilla>` | Hipoteca la propiedad y entrega el 50% de su valor al dueño. |
| `DESHIPOTECAR` | `<idCasilla>` | Cancela la hipoteca pagando el 50% + 10% de interés. |

---

## 5. Módulo de Hardware (Raspberry Pi Pico)

* **Firmware**: MicroPython ubicado en `Monopoly/RASP/main.py` y `config_pines.py`.
* **Conexión Serial**: Puerto USB CDC-ACM (115200 baudios, `/dev/ttyACM*` en Linux o `COM*` en Windows).
* **Comandos Seriales**:
  - `READID` $\rightarrow$ Devuelve UID hexadecimal de la tarjeta RFID o `TIMEOUT`.
  - `DISPLAY NN` $\rightarrow$ Muestra el número `NN` (00-99) en el display multiplexado.
  - `LEDON` / `LEDOFF` $\rightarrow$ Control del LED de estado en GP20.
  - `ISDARK` $\rightarrow$ Lectura del sensor de luz LDR.
* **Herramienta PLUS Independiente**:
  Existe un proyecto de consola aislado para probar el puerto serial directamente con la Pico sin levantar el servidor:
  ```bash
  dotnet run --project Monopoly/ControladorSerial_PLUS/controlador.csproj
  ```
