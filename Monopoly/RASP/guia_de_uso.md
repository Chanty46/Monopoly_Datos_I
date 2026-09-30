# Guía de uso — Taller 1 (Raspberry Pi Pico + C#)

Comunicación serial USB multiplataforma (Windows / Arch Linux / Fedora / macOS)
más el soporte del display de 7 segmentos de 2 dígitos.

---

## 1. Qué cambió respecto al proyecto original

| Archivo | Cambio |
|---|---|
| `controlador.cs` | Ya no usa `"COM3"` fijo. Detecta los puertos con `SerialPort.GetPortNames()`, elige automático si hay uno solo, pide elegir si hay varios, y permite reintentar si falla la apertura. |
| `main.py` (Pico) | LED corregido de `GP21` a **`GP20`**. Se agregó el comando `DISPLAY NN` y la lógica del display de 7 segmentos multiplexado (2 dígitos). Código reorganizado en secciones, con **todos los pines en un solo bloque de configuración** en `config_pines.py`. RFID usa GP1, GP4, GP5, GP18, GP19 (SCK/MOSI movidos por pines dañados en la placa) y display en GP6–GP14 (sin cambios). |

Ningún comando existente (`ISDARK`, `READID`, `LEDON`, `LEDOFF`, `SALIR`) se eliminó ni cambió su formato de respuesta.

---

## 2. Protocolo final

| Comando enviado | Respuesta de la Pico |
|---|---|
| `ISDARK` | `DARK` o `LIGHT` |
| `READID` | UID en hexadecimal, o `TIMEOUT` tras 10s |
| `LEDON` | `OK` (enciende GP20) |
| `LEDOFF` | `OK` (apaga GP20) |
| `DISPLAY NN` (NN entre `00` y `99`) | `OK` |
| `DISPLAY` con valor fuera de 0–99, no numérico, o mal formado | `ERROR_DISPLAY_INVALIDO` |
| Comando no reconocido | `ERROR_COMANDO_DESCONOCIDO` |
| `SALIR` | Se procesa solo del lado de C# (cierra la app); no se envía a la Pico |

Baudrate 115200, `NewLine = "\n"`, timeout de lectura 15000 ms (sin cambios).

---

## 3. Mapa de pines (RFID ajustado por pines dañados, sin tocar el display)

**GP2 y GP3 resultaron dañados / no funcionales en esta placa Pico en particular.** El display ya estaba cableado en GP6–GP14, así que SCK y MOSI del RFID se movieron a otras opciones válidas de SPI0 que no chocan con esos pines: **GP18 y GP19**. Todo se edita en un único archivo, `config_pines.py`; el código verifica al arrancar que no haya pines repetidos.

| Componente | GPIO | Pin físico | Función |
|---|---|---|---|
| **RFID-RC522** | GP1 | 2 | SDA / CS |
| | GP18 | 24 | SCK *(antes GP2, dañado)* |
| | GP19 | 25 | MOSI *(antes GP3, dañado)* |
| | GP4 | 6 | MISO |
| | — | — | IRQ (sin conectar, no se usa) |
| | GND | 3 | GND del módulo |
| | GP5 | 7 | RST |
| | 3V3(OUT) | 36 | VCC del módulo |
| **Display 7 seg** | GP6 | 9 | Segmento a |
| | GP7 | 10 | Segmento b |
| | GP8 | 11 | Segmento c |
| | GP9 | 12 | Segmento d |
| | GP10 | 14 | Segmento e |
| | GP11 | 15 | Segmento f |
| | GP12 | 16 | Segmento g |
| | GP13 | 17 | Común decenas (vía transistor Q1) |
| | GP14 | 19 | Común unidades (vía transistor Q2) |
| **LED** | GP20 | 26 | LED (vía 1 kΩ) |
| **LDR** | GP27 | 32 | Entrada analógica (divisor con 10 kΩ) |

Pines GND útiles: 3, 8, 13, 18, 23, 28, 33, 38.
Pines evitados a propósito: GP2 y GP3 (no funcionales en esta placa), GP21 (no es el LED), GP23/24/25/29 (uso interno de la placa).

**Por qué GP18/GP19 y no GP6/GP7:** MISO/SCK/MOSI son un periférico de hardware (SPI0) y solo aceptan un conjunto fijo de pines:
- SCK: GP2 *(no funciona en esta placa)*, GP6 *(ya lo usa el display)*, GP18 o GP22
- MOSI: GP3 *(no funciona en esta placa)*, GP7 *(ya lo usa el display)*, GP19 o GP23
- MISO: GP0, GP4, GP16 o GP20

De las opciones que quedan tras descartar las dañadas y las que ya ocupa el display, GP18 y GP19 son las más bajas disponibles.

**Si algún otro pin del RFID falla más adelante**, las alternativas válidas para SCK/MOSI son GP22/GP23; RST y CS (GPIO comunes) pueden ir en cualquier pin libre. `test_pines_rfid.py` sirve para detectar cuál pin específico falló antes de decidir el reemplazo.

**Cambios de cableado respecto a la versión anterior de este documento:**

| Componente | Antes | Ahora |
|---|---|---|
| RFID SCK | GP6 | **GP18** (para no chocar con el display, que ya usaba GP6) |
| RFID MOSI | GP7 | **GP19** (para no chocar con el display, que ya usaba GP7) |
| Display | GP8–GP16 | **GP6–GP14** (vuelve al cableado original que ya tenías hecho) |

LED (GP20) y LDR (GP27) no cambiaron.

**Pendiente de confirmar:** el tipo de display (ánodo común o cátodo común) no se pudo determinar de los archivos del proyecto. Queda configurado con la bandera:

```python
DISPLAY_ANODO_COMUN = False  # False = catodo comun (por defecto) | True = anodo comun
```

Cambiar a `True` si el display real es de ánodo común, antes de conectarlo.

**Resistencias del display:** una de ~220 Ω por cada segmento (7 en total, compartidas por los dos dígitos), y una de 1 kΩ en la base de cada transistor NPN de los comunes. Valores típicos: confirmar con la hoja de datos del display.

---

## 4. Instrucciones de instalación y ejecución

### Windows
1. Instalar el SDK de .NET 8 si no está instalado.
2. Conectar la Pico por USB (con `main.py` ya cargado y ejecutándose).
3. Abrir una terminal en la carpeta del proyecto (donde está el `.csproj`) y ejecutar:
   ```
   dotnet run
   ```
4. El programa detecta el puerto automáticamente (o lo lista si hay varios).

### Arch Linux
1. Instalar el SDK: `sudo pacman -S dotnet-sdk` (o usar el paquete correspondiente del AUR si tu versión de Arch lo requiere).
2. **Permisos del puerto serial:** si al conectar aparece `Permission denied` / `Acceso denegado`, agregar el usuario al grupo `uucp`:
   ```
   sudo usermod -aG uucp $USER
   ```
   Cerrar sesión y volver a iniciarla para que el cambio de grupo tenga efecto.
3. Ejecutar `dotnet run` en la carpeta del proyecto.

### Fedora
1. Instalar el SDK: `sudo dnf install dotnet-sdk-8.0`.
2. **Permisos del puerto serial:** si aparece error de permisos, agregar el usuario al grupo `dialout`:
   ```
   sudo usermod -aG dialout $USER
   ```
   Cerrar sesión y volver a iniciarla.
3. Ejecutar `dotnet run` en la carpeta del proyecto.

> La app **nunca** ejecuta `sudo` ni cambia permisos por sí misma: estos pasos son manuales, una sola vez por máquina.

### macOS
El código no depende de ningún nombre de puerto fijo, por lo que debería reconocer puertos como `/dev/tty.usbmodemXXXX` sin cambios. No se pudo probar en hardware real macOS en este entorno; si `SerialPort` no detecta el puerto, verificar que el SDK de .NET para macOS esté instalado correctamente.

### Cargar el firmware en la Pico
1. Abrir Thonny, conectar la Pico.
2. Subir `main.py` a la raíz de la Pico y `mfrc522.py` a la carpeta `lib/`.
3. Ejecutar `main.py` desde Thonny para probarlo, y luego usar **Run → Disconnect** (o desconectar y reconectar el USB) para liberar el puerto antes de correr la app de C#.

---

## 5. Segunda ronda: pruebas reales en Linux y mejoras aplicadas

En una revisión posterior se consiguió instalar un **SDK de .NET 8 real** dentro de un contenedor Linux (Ubuntu 24.04) y compilar `controlador.cs` con el compilador real de Roslyn (no Mono). Como el paquete NuGet `System.IO.Ports` no se pudo descargar (sin acceso a `nuget.org` en este entorno), se compiló y ejecutó contra un **stub local** que replica exactamente la forma de la API usada (`GetPortNames`, `Open`, `WriteLine`, `ReadLine`, `Close`), configurable por variables de entorno para simular distintos escenarios. Esto permitió correr el binario real, no solo revisar el código.

**Resultado:** compila sin errores ni advertencias (ni siquiera de nullable). Pero al simular un Linux típico apareció un **bug real**:

### Bug encontrado: puertos "fantasma" en Linux
En Linux, `SerialPort.GetPortNames()` casi siempre devuelve también `/dev/ttyS0`, `/dev/ttyS1`, etc. — los puertos serie heredados de la placa base, que existen aunque no haya ningún cable RS-232 conectado. Con la versión anterior del código, esto significaba que la regla "si hay un solo puerto, lo selecciona automático" **casi nunca se cumplía en Linux**, aunque la Pico fuera el único dispositivo USB conectado: el estudiante siempre tenía que elegir manualmente entre una lista con 4-5 puertos, la mayoría inexistentes.

**Solución aplicada:** `SeleccionarPuerto()` ahora filtra los puertos heredados típicos (`/dev/ttyS<n>` en Linux, `COM1`/`COM2` en Windows, que en la práctica casi nunca son dispositivos USB) antes de decidir si hay "uno solo" o "varios". Si el filtro dejara la lista vacía (por ejemplo, si por alguna razón el único puerto disponible fuera uno de esos), se usa la lista completa sin filtrar como respaldo, para no perder nunca la posibilidad de conectar. Se probó con el binario real:

| Escenario simulado | Resultado |
|---|---|
| `ttyS0..ttyS3` (fantasma) + `ttyACM0` (Pico real) | Antes: pedía elegir entre 5. Ahora: detecta `ttyACM0` solo. ✅ |
| Solo puertos fantasma, sin Pico conectada | Cae al respaldo (lista completa), no se rompe. ✅ |
| `COM1`/`COM2` (fantasma Windows) + `COM3` (real) | Detecta `COM3` solo. ✅ |
| Dos dispositivos USB reales, sin fantasmas | Sigue mostrando la lista y dejando elegir, sin cambios. ✅ |
| Un solo puerto real (macOS `tty.usbmodem...`) | Sigue funcionando igual que antes. ✅ |

### Segunda mejora: mensajes de error más precisos
Antes, cualquier error al abrir el puerto en Linux mostraba el mismo aviso de "revise permisos", incluso cuando el problema real era otro (por ejemplo, el puerto ya abierto por Thonny). Ahora el mensaje se elige según el texto real de la excepción:

| Causa real del error | Mensaje mostrado |
|---|---|
| Sin permisos (`denied`/`permission`) | Sugiere agregar el usuario al grupo `dialout`/`uucp` |
| Puerto ocupado (`busy`/`in use`/`sharing`) | Sugiere cerrar Thonny u otro programa que tenga el puerto abierto |

Ambos casos se probaron con el binario real, simulando cada tipo de excepción — los dos mensajes salen correctamente diferenciados.

---

## 6. Pruebas realizadas (simulación exhaustiva, primera ronda)

Antes de conseguir el SDK real, se había validado de dos formas alternativas:

**a) Lógica de selección de puerto** — se tradujo `SeleccionarPuerto()` línea por línea a un script equivalente y se probaron 10 escenarios:

| Escenario | Resultado |
|---|---|
| Sin puertos disponibles | Mensaje "Conecte la Raspberry Pi Pico..." ✅ |
| Un solo puerto (`COM3`, `/dev/ttyACM0`, `/dev/ttyUSB0`, `/dev/tty.usbmodem14201`) | Selección automática ✅ (los 4 formatos) |
| Varios puertos, elige uno válido | Selecciona el puerto correcto por índice ✅ |
| Varios puertos, índice fuera de rango | "Opcion invalida." ✅ |
| Varios puertos, entrada no numérica o vacía | "Opcion invalida." ✅ |

**b) Firmware de la Pico** (`main.py`) — se ejecutó el archivo real con módulos `machine`/`mfrc522`/`time` simulados (sin hardware físico) y se probó cada comando:

| Comando enviado | Respuesta obtenida | Estado |
|---|---|---|
| `LEDON` | `OK` (se confirmó que activa **GP20**, no GP21) | ✅ |
| `LEDOFF` | `OK` (desactiva GP20) | ✅ |
| `DISPLAY 25` | `OK` | ✅ |
| `DISPLAY 00` | `OK` | ✅ |
| `DISPLAY 99` | `OK` | ✅ |
| `DISPLAY 100` | `ERROR_DISPLAY_INVALIDO` | ✅ |
| `DISPLAY -5` | `ERROR_DISPLAY_INVALIDO` | ✅ |
| `DISPLAY ab` | `ERROR_DISPLAY_INVALIDO` | ✅ |
| `DISPLAY` (sin número) | `ERROR_DISPLAY_INVALIDO` | ✅ |
| `DISPLAY 25 30` (dos números) | `ERROR_DISPLAY_INVALIDO` | ✅ |
| `ISDARK` con LDR simulado en luz | `LIGHT` | ✅ |
| `ISDARK` con LDR simulado en oscuridad | `DARK` | ✅ |
| `READID` con tarjeta simulada | UID devuelto correctamente (`C23B0307`) | ✅ |
| `READID` sin tarjeta | `TIMEOUT` (probado con timeout reducido para no esperar 10s reales) | ✅ |

**c) Patrones de segmentos y multiplexado** — se inspeccionaron directamente los niveles de pin generados al mostrar `DISPLAY 25`:
- Dígito de las decenas (2): segmentos a,b,d,e,g encendidos, c y f apagados → patrón correcto de un "2".
- Dígito de las unidades (5): segmentos a,c,d,f,g encendidos, b y e apagados → patrón correcto de un "5".
- Se confirmó que el común activo alterna correctamente entre GP13 y GP14 (comunes de decenas y unidades) en cada fase del multiplexado.
- Se probó también con `DISPLAY_ANODO_COMUN = True`: los niveles se invierten correctamente (segmentos activos en `0` en vez de `1`), confirmando que el cambio de tipo de display funciona sin tocar el resto del código.

**Actualización:** en la segunda ronda (sección 5) sí se consiguió compilar y ejecutar el código con el SDK real de .NET 8 en Linux, lo que confirmó y corrigió el bug de los puertos fantasma descrito arriba. Aun así, sigue pendiente probarlo con un `dotnet build` normal (con `System.IO.Ports` restaurado desde NuGet, no el stub) y con la Pico física conectada de verdad.

---

## 6b. Verificación del mapa de pines (automatizada)

Se comprobó sobre el `main.py` final: sin pines repetidos entre componentes; RFID en GP1, GP4, GP5, GP18, GP19 (SCK/MOSI reasignados) y display en GP6–GP14 consecutivos; segmentos a–g en orden GP6…GP12; ningún pin reservado (GP23/24/25/29); MISO/SCK/MOSI válidos para SPI0; LDR en pin con ADC; LED en GP20 y GP21 sin usar. El módulo RFID recibe `cs=1`, `sck=18`, `mosi=19`, `miso=4`, `rst=5` (SCK/MOSI reasignados a GP18/GP19 para no chocar con el display, que ya usaba GP6/GP7). Si se edita un pin y se repite con otro, el firmware se detiene al arrancar con el error `Pin repetido en la configuracion de pines`.

---

## 7. Prueba manual sugerida (con hardware real)

1. Conectar la Pico (con `main.py` corriendo, Thonny cerrado).
2. Ejecutar `dotnet run`. Confirmar que detecta/lista el puerto correctamente.
3. `LEDON` → el LED debe encender (en **GP20**). `LEDOFF` → debe apagar.
4. Conectar el display de 7 segmentos según la tabla de GPIO de la sección 3, confirmando primero si es ánodo o cátodo común.
5. `DISPLAY 25` → el display debe mostrar "25" de forma estable (sin parpadeo notorio, gracias al refresco a 200 Hz).
6. `READID` → acercar una tarjeta/llavero y confirmar que el UID coincide con el impreso en la tarjeta.
7. `ISDARK` → tapar la LDR con la mano y comparar la respuesta antes/después.
8. `SALIR` → debe cerrar la app y mostrar "Conexion cerrada."
9. Repetir los pasos 2–8 en Arch Linux y en Fedora, usando el mismo binario/código (solo cambia el nombre del puerto que detecta automáticamente).

---

## 8. Pendientes de tu parte

- Confirmar si el display es de ánodo o cátodo común y ajustar `DISPLAY_ANODO_COMUN` en `main.py`.
- Correr `dotnet build`/`dotnet run` una vez en una máquina con el SDK de .NET 8 instalado, para la verificación final de compilación.
- Si quieres, puedo aplicar el mismo cambio de detección de puerto a `final/Program.cs` (la versión con las clases de los 5 retos), que hoy también tiene `"COM3"` fijo.
