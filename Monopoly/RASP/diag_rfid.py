"""
DIAGNOSTICO RFID - correr en Thonny REPL (no como main.py)
============================================================
1. Detener main.py (boton Stop en Thonny)
2. En la Shell/REPL pegar: exec(open('diag_rfid.py').read())
   o ir a Run -> Run current script con este archivo abierto

El script NO modifica nada; solo lee registros y reporta.
"""

import time
from machine import Pin, SPI
from config_pines import (PIN_RFID_SCK, PIN_RFID_MOSI, PIN_RFID_MISO,
                           PIN_RFID_RST, PIN_RFID_CS)

# ---------------------------------------------------------------------------
# 1) COMUNICACION SPI BASICA - sin usar la clase MFRC522
#    Si el SPI no funciona, los registros devuelven 0x00 o 0xFF.
# ---------------------------------------------------------------------------
print("=" * 50)
print("PASO 1: Verificando comunicacion SPI")
print("=" * 50)

sck  = Pin(PIN_RFID_SCK,  Pin.OUT)
mosi = Pin(PIN_RFID_MOSI, Pin.OUT)
miso = Pin(PIN_RFID_MISO)
rst  = Pin(PIN_RFID_RST,  Pin.OUT)
cs   = Pin(PIN_RFID_CS,   Pin.OUT)

rst.value(0)
cs.value(1)
time.sleep_ms(10)

spi = SPI(0, baudrate=1_000_000, sck=sck, mosi=mosi, miso=miso)

rst.value(1)
time.sleep_ms(50)  # espera reset hardware

# Funcion de lectura directa de registro
def read_reg(reg):
    cs.value(0)
    spi.write(bytes([((reg << 1) & 0x7E) | 0x80]))
    val = spi.read(1)
    cs.value(1)
    return val[0]

def write_reg(reg, val):
    cs.value(0)
    spi.write(bytes([(reg << 1) & 0x7E, val & 0xFF]))
    cs.value(1)

# Soft reset
write_reg(0x01, 0x0F)
time.sleep_ms(50)

# Leer VersionReg (0x37) - identifica el chip
version = read_reg(0x37)
VERSIONES = {0x88: "Clon v0.0", 0x90: "v0.0", 0x91: "v1.0", 0x92: "v2.0"}

print(f"VersionReg = 0x{version:02X} -> {VERSIONES.get(version, 'DESCONOCIDO')}")

if version in (0x00, 0xFF):
    print()
    print(">>> FALLA SPI: el chip no responde (devuelve 0x{:02X}).".format(version))
    print("    Posibles causas:")
    print("    - SCK, MOSI, MISO o CS desconectados o cruzados")
    print("    - Alimentacion 3.3V ausente o GND no comun")
    print("    - Pines en config_pines.py incorrectos")
    print(f"    Config actual: SCK=GP{PIN_RFID_SCK} MOSI=GP{PIN_RFID_MOSI}")
    print(f"                   MISO=GP{PIN_RFID_MISO} CS=GP{PIN_RFID_CS} RST=GP{PIN_RFID_RST}")
    raise SystemExit

print("OK - SPI funciona, chip identificado.\n")

# ---------------------------------------------------------------------------
# 2) INICIALIZAR CHIP Y ANTENA
# ---------------------------------------------------------------------------
print("=" * 50)
print("PASO 2: Inicializando chip y antena")
print("=" * 50)

write_reg(0x2A, 0x8D)
write_reg(0x2B, 0x3E)
write_reg(0x2D, 30)
write_reg(0x2C, 0)
write_reg(0x15, 0x40)
write_reg(0x11, 0x3D)

# Encender antena (TxControlReg bits 0 y 1)
tx_ctrl = read_reg(0x14)
write_reg(0x14, tx_ctrl | 0x03)
tx_ctrl_post = read_reg(0x14)

print(f"TxControlReg antes: 0x{tx_ctrl:02X}  despues: 0x{tx_ctrl_post:02X}")
if (tx_ctrl_post & 0x03) == 0x03:
    print("OK - Antena encendida.\n")
else:
    print("AVISO - Los bits de antena no se encendieron. El modulo puede estar danado.\n")

# ---------------------------------------------------------------------------
# 3) ESPERAR TARJETA - bucle de 10 segundos sin rfid.init() en cada vuelta
# ---------------------------------------------------------------------------
print("=" * 50)
print("PASO 3: Buscando tarjeta (10 segundos)")
print("Acerca el llavero/tarjeta ahora...")
print("=" * 50)

REQIDL = 0x26

def _tocard(cmd, send):
    recv = []
    bits = 0
    irq_en = wait_irq = 0

    if cmd == 0x0C:
        irq_en   = 0x77
        wait_irq = 0x30

    write_reg(0x02, irq_en | 0x80)

    v = read_reg(0x04); write_reg(0x04, v & ~0x80)   # clear IRQ
    v = read_reg(0x0A); write_reg(0x0A, v |  0x80)   # flush FIFO
    write_reg(0x01, 0x00)                              # IDLE

    for c in send:
        write_reg(0x09, c)
    write_reg(0x01, cmd)

    if cmd == 0x0C:
        v = read_reg(0x0D); write_reg(0x0D, v | 0x80)  # StartSend

    i = 2000
    while True:
        n = read_reg(0x04)
        i -= 1
        if not ((i != 0) and not (n & 0x01) and not (n & wait_irq)):
            break

    v = read_reg(0x0D); write_reg(0x0D, v & ~0x80)   # stop send

    stat = 2  # ERR
    if i:
        if (read_reg(0x06) & 0x1B) == 0x00:
            stat = 0  # OK
            if n & irq_en & 0x01:
                stat = 1  # NOTAGERR
            elif cmd == 0x0C:
                n    = read_reg(0x0A)
                lbits = read_reg(0x0C) & 0x07
                bits = (n - 1) * 8 + lbits if lbits else n * 8
                n = max(1, min(n, 16))
                for _ in range(n):
                    recv.append(read_reg(0x09))

    return stat, recv, bits

intentos = 0
inicio = time.ticks_ms()
while time.ticks_diff(time.ticks_ms(), inicio) < 10000:
    write_reg(0x0D, 0x07)
    stat, recv, bits = _tocard(0x0C, [REQIDL])
    intentos += 1

    if stat == 0 and bits == 0x10:
        print(f"SEÑAL DE TARJETA detectada (intento {intentos}). Leyendo UID...")

        # Anticollision
        write_reg(0x0D, 0x00)
        stat2, uid_raw, _ = _tocard(0x0C, [0x93, 0x20])
        if stat2 == 0 and len(uid_raw) == 5:
            uid = uid_raw[:4]
            uid_str = "".join("{:02X}".format(b) for b in uid)
            print(f">>> UID LEIDO: {uid_str}  ({uid})")
            break
        else:
            print(f"Anticollision fallo (stat={stat2} len={len(uid_raw)})")
    else:
        if intentos % 20 == 0:
            elapsed = time.ticks_diff(time.ticks_ms(), inicio) // 1000
            print(f"  {elapsed}s... ({intentos} intentos, sin tarjeta)")

    time.sleep_ms(50)
else:
    print(f"\nTIMEOUT - No se detecto tarjeta en 10 segundos ({intentos} intentos).")
    print("Si el PASO 1 y 2 fueron OK, el problema es fisico:")
    print("  - Acerca mas la tarjeta (debe estar a menos de 3 cm)")
    print("  - Prueba con otra tarjeta o llavero MIFARE")
    print("  - Verifica que el modulo RC522 tenga la antena (bobina) intacta")

print("\nDiagnostico terminado.")
