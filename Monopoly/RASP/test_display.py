"""
Prueba rapida del Display de 7 segmentos de 2 digitos (multiplexado).
Muestra una cuenta regresiva del 99 al 00 para confirmar el multiplexado
y que todos los segmentos (a-g) y comunes (decenas y unidades) encienden.

Uso: Subir a la Raspberry Pi Pico y ejecutar desde Thonny o terminal serial.
"""

from machine import Pin, Timer
import time
from config_pines import (PINES_POR_SEGMENTO, PIN_COMUN_DECENAS, PIN_COMUN_UNIDADES,
                           PIN_DP, DISPLAY_ANODO_COMUN, FRECUENCIA_REFRESCO_HZ)

_ORDEN_SEGMENTOS = ("a", "b", "c", "d", "e", "f", "g")
_pines_segmentos = [Pin(PINES_POR_SEGMENTO[letra], Pin.OUT) for letra in _ORDEN_SEGMENTOS]
_pin_comun_decenas = Pin(PIN_COMUN_DECENAS, Pin.OUT)
_pin_comun_unidades = Pin(PIN_COMUN_UNIDADES, Pin.OUT)
_pin_dp = Pin(PIN_DP, Pin.OUT)

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

_NIVEL_ENCENDIDO = 0 if DISPLAY_ANODO_COMUN else 1
_NIVEL_APAGADO = 1 if DISPLAY_ANODO_COMUN else 0
_pin_dp.value(_NIVEL_APAGADO)

_digito_decenas = 0
_digito_unidades = 0
_fase = 0

def _dibujar(valor, comun_on, comun_off):
    patron = _TABLA_SEGMENTOS[valor]
    for pin, enc in zip(_pines_segmentos, patron):
        pin.value(_NIVEL_ENCENDIDO if enc else _NIVEL_APAGADO)
    comun_off.value(_NIVEL_APAGADO)
    comun_on.value(_NIVEL_ENCENDIDO)

def _refrescar(timer):
    global _fase
    if _fase == 0:
        _dibujar(_digito_decenas, _pin_comun_decenas, _pin_comun_unidades)
    else:
        _dibujar(_digito_unidades, _pin_comun_unidades, _pin_comun_decenas)
    _fase = 1 - _fase

timer = Timer()
timer.init(freq=FRECUENCIA_REFRESCO_HZ, mode=Timer.PERIODIC, callback=_refrescar)

print("Iniciando prueba del display 7 segmentos (contador 0 a 25)...")
for n in range(26):
    _digito_decenas = n // 10
    _digito_unidades = n % 10
    print(f"Display mostrando: {n:02d}")
    time.sleep_ms(300)

print("Prueba completada.")
