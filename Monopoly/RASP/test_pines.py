"""
Prueba rapida de TODOS los pines usados en el proyecto, con un solo LED
de sonda (no hace falta tener el circuito completo armado).

Como usarlo:
  1. Arma un LED con una resistencia (ej. 220 ohm) en serie.
  2. Conecta una pata (via la resistencia) a GND de la Pico. Dejala fija ahi.
  3. La otra pata es la "sonda": andala tocando cada pin de la Pico a
     medida que la consola de Thonny dice cual pin esta probando.
  4. Si el LED parpadea al tocar el pin correcto, ese pin y su cable estan
     bien. Si no prende en ningun pin, revisa la resistencia/LED/GND.

Recorre los pines en el mismo orden logico del proyecto (RFID, display,
LED, LDR), usando los nombres definidos en config_pines.py, para que si se
cambia el mapa de pines esta prueba se actualice sola.
"""

from machine import Pin
import time
from config_pines import (PIN_RFID_CS, PIN_RFID_SCK, PIN_RFID_MOSI,
                           PIN_RFID_MISO, PIN_RFID_RST, PINES_POR_SEGMENTO,
                           PIN_DP, PIN_COMUN_DECENAS, PIN_COMUN_UNIDADES,
                           PIN_LED, PIN_LDR)

SEGUNDOS_POR_PIN = 3       # tiempo para mover la sonda antes de pasar al siguiente
PARPADEOS_POR_PIN = 6      # cuantas veces prende/apaga en ese tiempo

# Lista de (nombre a mostrar, numero de GPIO), en el mismo orden del
# proyecto: RFID (en el orden de su header), display, LED y LDR. Los
# segmentos se recorren en orden a-g logico, aunque el cableado fisico real
# (PINES_POR_SEGMENTO) no siga ese mismo orden de GPIO.
PINES = [
    ("RFID - SDA/CS", PIN_RFID_CS),
    ("RFID - SCK", PIN_RFID_SCK),
    ("RFID - MOSI", PIN_RFID_MOSI),
    ("RFID - MISO", PIN_RFID_MISO),
    ("RFID - RST", PIN_RFID_RST),
]
for _letra in ("a", "b", "c", "d", "e", "f", "g"):
    PINES.append(("Display - segmento " + _letra, PINES_POR_SEGMENTO[_letra]))
PINES.append(("Display - punto decimal (DP, sin usar)", PIN_DP))
PINES.append(("Display - comun decenas", PIN_COMUN_DECENAS))
PINES.append(("Display - comun unidades", PIN_COMUN_UNIDADES))
PINES.append(("LED", PIN_LED))
PINES.append(("LDR (prueba electrica, no la funcion analogica)", PIN_LDR))


def probar_pin(nombre, gp):
    print("--- {} -> GP{} ---".format(nombre, gp))
    pin = Pin(gp, Pin.OUT)
    intervalo = SEGUNDOS_POR_PIN / (PARPADEOS_POR_PIN * 2)
    for _ in range(PARPADEOS_POR_PIN):
        pin.value(1)
        time.sleep(intervalo)
        pin.value(0)
        time.sleep(intervalo)


def main():
    print("=== Prueba de pines con LED de sonda ===")
    print("LED+resistencia: una pata a GND, la otra pata la vas moviendo")
    print("al pin que se anuncia en cada paso.\n")
    print("Total de pines a probar:", len(PINES))
    print("Presiona STOP en Thonny para terminar en cualquier momento.\n")

    for nombre, gp in PINES:
        probar_pin(nombre, gp)

    print("\nPrueba terminada. Si algun pin no encendio el LED, revisa ese cable.")


if __name__ == "__main__":
    main()
