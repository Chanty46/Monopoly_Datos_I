"""
Prueba rapida de los 5 pines del RFID, uno a la vez, encendidos FIJOS
(no parpadean) para poder comprobarlos con un LED de sonda o un
multimetro con calma.

Como usarlo:
  1. LED+resistencia (~220 ohm): una pata fija a GND de la Pico.
  2. Corre este archivo. La consola de Thonny anuncia el pin actual y lo
     deja encendido en 3.3V durante varios segundos.
  3. Toca ese pin con la otra pata del LED (o mide con el multimetro).
     Si prende / marca 3.3V, ese pin y su cable estan bien.
  4. Pasados los segundos, apaga ese pin y prende el siguiente.

Usa los pines de config_pines.py (no los repite a mano), en el mismo
orden del header fisico del modulo: SDA/CS, SCK, MOSI, MISO, RST.
"""

from machine import Pin
import time
from config_pines import (PIN_RFID_CS, PIN_RFID_SCK, PIN_RFID_MOSI,
                           PIN_RFID_MISO, PIN_RFID_RST)

SEGUNDOS_POR_PIN = 5  # tiempo que queda encendido cada pin

PINES = [
    ("RFID - SDA/CS", PIN_RFID_CS),
    ("RFID - SCK", PIN_RFID_SCK),
    ("RFID - MOSI", PIN_RFID_MOSI),
    ("RFID - MISO", PIN_RFID_MISO),
    ("RFID - RST", PIN_RFID_RST),
]


def main():
    print("=== Prueba de pines del RFID (encendido fijo) ===")
    print("LED+resistencia: una pata a GND, la otra la vas tocando en cada")
    print("pin mientras queda encendido. O medi con el multimetro.\n")

    for nombre, gp in PINES:
        pin = Pin(gp, Pin.OUT)
        pin.value(1)
        print("--- {} -> GP{} : ENCENDIDO ({}s) ---".format(nombre, gp, SEGUNDOS_POR_PIN))
        time.sleep(SEGUNDOS_POR_PIN)
        pin.value(0)

    print("\nPrueba terminada. Si algun pin no encendio el LED, revisa ese cable.")


if __name__ == "__main__":
    main()
