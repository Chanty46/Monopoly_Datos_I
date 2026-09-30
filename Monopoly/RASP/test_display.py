"""
Prueba del Display de 7 segmentos de 2 digitos (multiplexado).

Valores criticos de prueba (spec):
  00, 01, 02, 09, 10, 12, 13, 20, 31, 42, 50, 69, 99

IMPORTANTE: tras la correccion del cableado (PIN_COMUN_DECENAS=GP14,
PIN_COMUN_UNIDADES=GP13), el valor logico 31 debe mostrarse fisicamente
como "31" (3 a la izquierda, 1 a la derecha), NO como "13".

Uso: Subir a la Raspberry Pi Pico y ejecutar desde Thonny o terminal serial.
"""

from machine import Pin, Timer
import time
from config_pines import (PINES_POR_SEGMENTO, PIN_COMUN_DECENAS, PIN_COMUN_UNIDADES,
                           PIN_DP, DISPLAY_ANODO_COMUN, FRECUENCIA_REFRESCO_HZ)

# --- Hardware ----------------------------------------------------------
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
_NIVEL_APAGADO   = 1 if DISPLAY_ANODO_COMUN else 0
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
    # Corrección lógica por software: GP14 controla el display izquierdo (decenas)
    # y GP13 controla el display derecho (unidades).
    global _fase
    if _fase == 0:
        _dibujar(_digito_decenas, _pin_comun_unidades, _pin_comun_decenas)
    else:
        _dibujar(_digito_unidades, _pin_comun_decenas, _pin_comun_unidades)
    _fase = 1 - _fase


def mostrar(numero):
    """Muestra un numero 0-99 en el display y devuelve True si es valido."""
    global _digito_decenas, _digito_unidades
    if not (0 <= numero <= 99):
        return False
    _digito_decenas = numero // 10
    _digito_unidades = numero % 10
    return True


# --- Inicio del timer de refresco --------------------------------------
timer = Timer()
timer.init(freq=FRECUENCIA_REFRESCO_HZ, mode=Timer.PERIODIC, callback=_refrescar)

# --- Prueba de valores criticos del spec -------------------------------
VALORES_CRITICOS = [0, 1, 2, 9, 10, 11, 12, 13, 20, 21, 31, 42, 50, 69, 90, 99]

print("=" * 48)
print("PRUEBA DISPLAY 7 SEGMENTOS — VALORES CRITICOS")
print(f"  PIN_COMUN_DECENAS  = GP{PIN_COMUN_DECENAS} (Fisicamente comun derecho / unidades)")
print(f"  PIN_COMUN_UNIDADES = GP{PIN_COMUN_UNIDADES} (Fisicamente comun izquierdo / decenas)")
print("=" * 48)

errores = 0
for n in VALORES_CRITICOS:
    ok = mostrar(n)
    estado = "OK" if ok else "ERROR"
    print(f"  Valor {n:02d}  -> Display debe mostrar '{n:02d}'  [{estado}]")
    time.sleep_ms(800)   # 0.8 s por valor: tiempo suficiente para leerlo

print()
print("Contador final 00..25 (confirmacion de secuencia):")
for n in range(26):
    mostrar(n)
    print(f"  {n:02d}", end="  " if (n + 1) % 5 else "\n")
    time.sleep_ms(300)

print()
print("Prueba completada.")
print("Si 31 se leyo como '31' (3 izq, 1 der), la correccion es CORRECTA.")
