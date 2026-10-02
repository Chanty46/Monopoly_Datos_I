# Monopoly Distribuido — Algoritmos y Estructuras de Datos I

Proyecto de Monopoly distribuido desarrollado para el curso **Algoritmos y Estructuras de Datos 1** (Semestre 2 2026) en el **Tecnológico de Costa Rica (TEC)**.

El sistema funciona con arquitectura **Cliente-Servidor (TCP)** centralizada, incluye una **interfaz gráfica Web interactiva**, cuenta con **estructuras de datos lineales propias** (sin usar las librerías de colecciones de .NET) y se integra opcionalmente con hardware real (**Raspberry Pi Pico** con lector RFID y display de 7 segmentos).

---

## 🏛️ Arquitectura del Sistema

```
                      ┌───────────────────────────────────────────┐
                      │             SERVIDOR MONOPOLY             │
                      │           (Autoridad Central)             │
                      │  • Tablero (Lista Circular Doble)         │
                      │  • Turnos (Cola / Lista Circular Simple)  │
                      │  • Transacciones (Lista Lineal Doble)     │
                      │  • Mazo de Eventos (Cola Circular)        │
                      │  • Banco & Dados Electrónicos             │
                      └─────────────┬───────────────────────┬─────┘
                                    │                       │
                      TCP / Sockets │         HTTP / REST   │ USB Serial / Fallback
                        Puerto 5000 │         Puerto 8080   │ 115200 baud
                                    │                       │
      ┌─────────────────────────────┼───────────────┐       │
      │                             │               │       ▼
┌─────▼───────┐               ┌─────▼───────┐ ┌─────▼───────┴─────┐
│  CLIENTES   │               │   WEB GUI   │ │ControladorHardware│
│ Consola CLI │               │  Navegador  │ │(Auto-reconexión y │
│(Jugadores 1-4)              │(Tablero 24c)│ │fallback si no hay)│
└─────────────┘               └─────────────┘ └─────┬─────────────┘
                                                    │
                                          ┌─────────▼─────────┐
                                          │ Raspberry Pi Pico │
                                          │  • Lector RC522   │
                                          │  • 7-Seg Multiplex│
                                          └───────────────────┘
```

---

## ⚡ Guía Rápida de Inicio

### 1. Compilar
```bash
dotnet build Monopoly/Monopoly.csproj
```

### 2. Iniciar el Servidor (con Interfaz Web)
Abre una terminal y ejecuta:
```bash
dotnet run --project Monopoly/Monopoly.csproj -- --server
```
Listo. El servidor estará escuchando en el puerto TCP `5000` y levantará automáticamente el **tablero interactivo en vivo** en:
👉 **`http://localhost:8080`**

Desde esa página puedes ver el tablero de 24 casillas, mover fichas, comprar propiedades, ver el display de 7 segmentos virtual y revisar el historial de transacciones.

### 3. Conectar Jugadores (Consola CLI)
En otra terminal (o desde otra computadora en la misma red cambiando la IP):
```bash
dotnet run --project Monopoly/Monopoly.csproj -- --client 127.0.0.1 5000 TuNombre
```
*Soporta hasta 4 jugadores simultáneos.*

### 4. Menú Interactivo
Si prefieres elegir qué iniciar desde un menú en consola:
```bash
dotnet run --project Monopoly/Monopoly.csproj
```

---

## 🧪 Pruebas Automatizadas

* **Batería de Pruebas Unitarias (123 pruebas):**  
  Verifica cada estructura de datos, enlaces circulares, reglas de compra, hipotecas y fallbacks:
  ```bash
  dotnet run --project Monopoly/Monopoly.csproj -- --unit-tests
  ```

* **Prueba de Demostración Rápida:**  
  Simula una partida completa de 2 jugadores con tiradas, compras, eventos y persistencia en segundos:
  ```bash
  dotnet run --project Monopoly/Monopoly.csproj -- --integration-test
  ```

---

## 📦 Estructuras de Datos Utilizadas

Cumpliendo con los requisitos de la cátedra, **no se usaron listas ni colas genéricas de .NET (`List`, `LinkedList`, `Queue`)** para la lógica del juego. Todas las estructuras fueron programadas desde cero:

1. **Tablero (`Tablero.cs`):**  
   **Lista circular doblemente enlazada** de 24 casillas. Cada nodo conoce su casilla anterior y siguiente. Permite recorrer el tablero cíclicamente y detectar cuándo un jugador cruza la Salida para cobrar sus ₡200.
2. **Turnos (`Turnos.cs`):**  
   **Cola / Lista circular simple**. Administra la rotación fluida de turnos entre los 4 jugadores. Si un jugador cae en bancarrota, se omite automáticamente sin romper el ciclo.
3. **Propiedades del Jugador (`ListaDePropiedades.cs`):**  
   **Lista lineal simplemente enlazada**. Cada jugador almacena aquí sus casillas adquiridas para calcular su patrimonio y gestionar hipotecas.
4. **Historial de Transacciones (`Transaccion.cs`):**  
   **Lista lineal doblemente enlazada**. Registra cada movimiento de dinero (compras, alquileres, paso por inicio). Permite recorridos bidireccionales (de la más antigua a la más reciente y viceversa), búsquedas por jugador o tipo, y se guarda automáticamente en `transacciones.txt`.
5. **Mazo de Eventos (`Eventos.cs`):**  
   **Cola circular enlazada**. Cada vez que un jugador saca una carta de suerte, esta pasa al final del mazo para reutilizarse de forma infinita.

---

## 🔌 Integración con Hardware (Raspberry Pi Pico)

El proyecto incluye soporte para un cajero físico en protoboard:
* **Lector RFID RC522:** Identifica la tarjeta de cada jugador (billetera electrónica).
* **Display de 7 segmentos de 2 dígitos:** Muestra el resultado de los dados, las casillas y los turnos con multiplexado corregido por software.
* **Tolerancia a fallos:** Si la Raspberry no está conectada o falla el lector RFID, **el juego nunca se detiene**. El servidor aplica un fallback automático usando el ID de cada jugador para que la partida continúe con total normalidad.

Los scripts en MicroPython y la guía de pines están ubicados en la carpeta [`Monopoly/RASP/`](Monopoly/RASP/).

---

## 📄 Documentación y Entregables

* 📊 **Diagrama de Clases UML y Protocolo TCP:** Consulta [UML_DIAGRAMA.md](UML_DIAGRAMA.md) para ver el diagrama formal en Mermaid, la descripción detallada de mensajes de red y la arquitectura.
* 🧾 **Registro de Transacciones:** Consulta [transacciones.txt](transacciones.txt) para revisar el historial generado por las partidas.
