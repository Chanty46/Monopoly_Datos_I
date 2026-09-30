"""

Comandos soportados:
  ISDARK        -> lee la fotoresistencia (LDR) y responde DARK o LIGHT
  READID        -> espera hasta 10s una tarjeta/llavero RFID y responde el UID (o TIMEOUT)
  LEDON         -> enciende el LED verde, responde OK
  LEDOFF        -> apaga el LED verde, responde OK
  DISPLAY NN    -> muestra el numero NN (00-99) en el display de 7 segmentos, responde OK
                   (si NN no es un numero entre 0 y 99, responde ERROR_DISPLAY_INVALIDO)

Requiere el archivo mfrc522.py (fork de danjperron/micropython-mfrc522,
el que SI soporta la plataforma 'rp2') subido junto a este archivo en la Pico.
Fuente: https://github.com/danjperron/micropython-mfrc522/blob/master/mfrc522.py
"""

import sys
from machine import Pin, ADC, Timer
import time
from mfrc522 import MFRC522
from config_pines import *


# =============================================================================
# 3. INICIALIZACION DEL HARDWARE
# =============================================================================
ldr = ADC(Pin(PIN_LDR))
led = Pin(PIN_LED, Pin.OUT)

rfid = MFRC522(sck=PIN_RFID_SCK, mosi=PIN_RFID_MOSI, miso=PIN_RFID_MISO,
               rst=PIN_RFID_RST, cs=PIN_RFID_CS, spi_id=0)

# La lista se arma en el orden logico a,b,c,d,e,f,g a partir del
# diccionario PINES_POR_SEGMENTO (config_pines.py), que es el que de
# verdad refleja el cableado fisico real -- asi no importa que el orden
# de los GPIO en la placa no coincida con el orden logico de los segmentos.
_ORDEN_SEGMENTOS = ("a", "b", "c", "d", "e", "f", "g")
_pines_segmentos = [Pin(PINES_POR_SEGMENTO[letra], Pin.OUT) for letra in _ORDEN_SEGMENTOS]
_pin_comun_decenas = Pin(PIN_COMUN_DECENAS, Pin.OUT)
_pin_comun_unidades = Pin(PIN_COMUN_UNIDADES, Pin.OUT)

# El punto decimal (DP) esta cableado pero no lo usa este proyecto: se deja
# siempre apagado.
_pin_dp = Pin(PIN_DP, Pin.OUT)


# =============================================================================
# 4. DISPLAY DE 7 SEGMENTOS (multiplexado)
# =============================================================================
# Segmentos que deben encenderse (a,b,c,d,e,f,g) para cada digito 0-9.
_TABLA_SEGMENTOS = {
    0: (1, 1, 1, 1, 1, 1, 0),
    1: (0, 1, 1, 0, 0, 0, 0),
    2: (1, 1, 0, 1, 1, 0, 1),
    3: (1, 1, 1, 1, 0, 0, 1),
    4: (0, 1, 1, 0, 0, 1, 1),
    5: (1, 0, 1, 1, 0, 1, 1),
    6: (1, 0, 1, 1, 1, 1, 1),
    7: (1, 1, 1, 0, 0, 0, 0),
    8: (1, 1, 1, 1, 1, 1, 1),
    9: (1, 1, 1, 1, 0, 1, 1),
}

# Nivel logico para "encendido" / "apagado", segun el tipo de display.
_NIVEL_ENCENDIDO = 0 if DISPLAY_ANODO_COMUN else 1
_NIVEL_APAGADO = 1 if DISPLAY_ANODO_COMUN else 0

_pin_dp.value(_NIVEL_APAGADO)  # punto decimal siempre apagado

_digito_decenas = 0
_digito_unidades = 0
_fase_multiplexado = 0  # alterna entre 0 (decenas) y 1 (unidades)


def _dibujar_digito(valor, pin_comun_activo, pin_comun_inactivo):
    patron = _TABLA_SEGMENTOS[valor]
    for pin, encendido in zip(_pines_segmentos, patron):
        pin.value(_NIVEL_ENCENDIDO if encendido else _NIVEL_APAGADO)
    pin_comun_inactivo.value(_NIVEL_APAGADO)
    pin_comun_activo.value(_NIVEL_ENCENDIDO)


def _refrescar_display(temporizador):
    # Se llama periodicamente (via Timer) para alternar rapido entre los dos
    # digitos y que, a simple vista, parezcan estar encendidos al mismo tiempo.
    global _fase_multiplexado
    if _fase_multiplexado == 0:
        _dibujar_digito(_digito_decenas, _pin_comun_decenas, _pin_comun_unidades)
    else:
        _dibujar_digito(_digito_unidades, _pin_comun_unidades, _pin_comun_decenas)
    _fase_multiplexado = 1 - _fase_multiplexado


# Refresco periodico en segundo plano; no bloquea la espera de comandos.
_timer_display = Timer()
_timer_display.init(freq=FRECUENCIA_REFRESCO_HZ, mode=Timer.PERIODIC,
                    callback=_refrescar_display)


def manejar_display(argumento):
    global _digito_decenas, _digito_unidades

    if not argumento.isdigit():
        print("ERROR_DISPLAY_INVALIDO")
        return

    numero = int(argumento)
    if numero < 0 or numero > 99:
        print("ERROR_DISPLAY_INVALIDO")
        return

    _digito_decenas = numero // 10
    _digito_unidades = numero % 10
    print("OK")


# =============================================================================
# 5. COMANDOS EXISTENTES (sin cambios de comportamiento)
# =============================================================================
def manejar_isdark():
    valor = ldr.read_u16()  # rango 0-65535 en RP2040
    oscuro = valor < UMBRAL_OSCURIDAD
    print("DARK" if oscuro else "LIGHT")


def manejar_readid():
    inicio = time.ticks_ms()
    while time.ticks_diff(time.ticks_ms(), inicio) < TIMEOUT_READID_MS:
        rfid.init()
        (estado, tipo_tarjeta) = rfid.request(rfid.REQIDL)
        if estado == rfid.OK:
            (estado, uid) = rfid.SelectTagSN()
            if estado == rfid.OK:
                uid_str = "".join("{:02X}".format(b) for b in uid)
                print(uid_str)
                return
        time.sleep_ms(100)
    print("TIMEOUT")


def manejar_ledon():
    led.on()
    print("OK")


def manejar_ledoff():
    led.off()
    print("OK")


# =============================================================================
# 6. BUCLE PRINCIPAL
# =============================================================================
def main():
    while True:
        comando = sys.stdin.readline().strip()

        if comando == "ISDARK":
            manejar_isdark()
        elif comando == "READID":
            manejar_readid()
        elif comando == "LEDON":
            manejar_ledon()
        elif comando == "LEDOFF":
            manejar_ledoff()
        elif comando == "TIRAR_DADOS" or comando == "DADOS":
            import urandom
            d1 = (urandom.getrandbits(16) % 6) + 1
            d2 = (urandom.getrandbits(16) % 6) + 1
            print(f"DADOS:{d1},{d2}")
        elif comando.startswith("DISPLAY"):
            partes = comando.split()
            if len(partes) == 2:
                manejar_display(partes[1])
            else:
                print("ERROR_DISPLAY_INVALIDO")
        elif comando:
            print("ERROR_COMANDO_DESCONOCIDO")


# El guard evita que main() se dispare solo al importar este archivo como
# modulo (por ejemplo, desde test_rfid.py para reutilizar los PIN_*), y no
# cambia nada cuando se ejecuta directamente en la Pico como programa principal.
if __name__ == "__main__":
    main()
