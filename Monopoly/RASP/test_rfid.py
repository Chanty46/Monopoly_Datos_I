"""
Prueba rapida de comunicacion con el modulo RFID-RC522, sin necesitar
ninguna tarjeta. Sirve para confirmar que el cableado SPI (SCK/MOSI/MISO/
CS/RST) esta bien conectado ANTES de correr main.py completo.

Como es "universal": no repite los numeros de pin a mano, los importa
directamente de config_pines.py (compartido con main.py), asi que si algun
dia se cambia el cableado, esta prueba se actualiza sola sin tocar nada aqui.

Uso: subir este archivo junto con main.py, config_pines.py y mfrc522.py a
la Pico, y correrlo desde Thonny (NO como main.py, para no chocar con el
bucle principal). Si main.py ya esta corriendo en la Pico, hay que
detenerlo primero (boton Stop en Thonny) antes de correr esta prueba,
porque ambos usan el mismo periferico SPI0.
"""

import time
from mfrc522 import MFRC522

# Se importan los pines de config_pines.py, NO de main.py. config_pines.py
# solo define numeros, no crea ningun objeto de hardware, asi que no hay
# riesgo de inicializar el SPI0 dos veces (como pasaba al importar main.py,
# que ya crea su propio objeto MFRC522/SPI0 a nivel de modulo).
from config_pines import (PIN_RFID_SCK, PIN_RFID_MOSI, PIN_RFID_MISO,
                           PIN_RFID_RST, PIN_RFID_CS)

VERSION_REG = 0x37  # registro VersionReg del chip MFRC522

# Valores tipicos que devuelve un MFRC522 genuino en VersionReg.
VERSIONES_CONOCIDAS = {
    0x88: "clon (version 0.0)",
    0x90: "version 0.0",
    0x91: "version 1.0",
    0x92: "version 2.0",
}


def main():
    print("Pines usados: SCK={} MOSI={} MISO={} RST={} CS={}".format(
        PIN_RFID_SCK, PIN_RFID_MOSI, PIN_RFID_MISO, PIN_RFID_RST, PIN_RFID_CS))

    rfid = MFRC522(sck=PIN_RFID_SCK, mosi=PIN_RFID_MOSI, miso=PIN_RFID_MISO,
                    rst=PIN_RFID_RST, cs=PIN_RFID_CS, spi_id=0)

    # 1) Leer el registro de version: si el SPI esta bien cableado, el chip
    #    responde con un valor fijo y conocido (no 0x00 ni 0xFF).
    version = rfid._rreg(VERSION_REG)
    print("VersionReg = {} ({})".format(
        hex(version), VERSIONES_CONOCIDAS.get(version, "valor no reconocido")))

    if version in (0x00, 0xFF):
        print("FALLA: el modulo no responde (0x00/0xFF).")
        print("Revisar: alimentacion 3.3V, GND comun, y el orden SCK/MOSI/MISO/CS/RST.")
        return
    elif version not in VERSIONES_CONOCIDAS:
        print("AVISO: el chip responde, pero con un valor no tipico.")
        print("Puede ser un clon; si mas abajo detecta una tarjeta, igual sirve.")
    else:
        print("OK: el modulo RFID responde correctamente por SPI.")

    # 2) Sin necesitar tarjeta: solo escucha 10 segundos por si acercan una,
    #    para confirmar tambien la antena. No es obligatorio para validar el SPI.
    print("Acerque una tarjeta/llavero (10s, opcional para esta prueba)...")
    for _ in range(30):
        (estado, tipo) = rfid.request(rfid.REQIDL)
        if estado == rfid.OK:
            (estado2, uid) = rfid.SelectTagSN()
            if estado2 == rfid.OK:
                uid_str = "".join("{:02X}".format(b) for b in uid)
                print("UID detectado:", uid_str)
                return
        time.sleep(0.3)

    print("Sin tarjeta detectada (no es un error si el SPI ya dio OK arriba).")


main()
