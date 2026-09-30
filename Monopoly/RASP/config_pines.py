"""
Configuracion de pines, compartida por main.py y por las pruebas de
diagnostico (test_rfid.py). Este archivo NO crea ningun objeto de hardware
(ni Pin, ni SPI, ni Timer): solo define numeros de pin. Por eso se puede
importar tantas veces como haga falta sin riesgo de inicializar el mismo
periferico (por ejemplo SPI0) dos veces.
"""

# =============================================================================
# 1. CONFIGURACION DE PINES  (unico lugar donde hay que editar pines)
# =============================================================================
# Numeros GP (no numero fisico del pin). Ver guia_de_uso.md para el mapa fisico.
#
# El bloque del RFID sigue, en la medida de lo posible, el orden del
# header (SDA, SCK, MOSI, MISO, IRQ, GND, RST, VCC). GP2 y GP3 resultaron
# danados/no funcionales en esta placa fisica. El display ya estaba cableado
# en GP6-GP14, asi que SCK/MOSI se movieron a otras opciones validas de
# SPI0 que no chocan con esos pines (GP6 y GP7 quedaron libres para el
# display, no para el RFID). IRQ no se usa y GND/VCC no son GPIO.
#
#   Header RFID -> GP asignado
#     SDA (CS) -> GP1
#     SCK      -> GP18  <- antes GP2 (danado); GP6 no se uso por chocar con el display
#     MOSI     -> GP19  <- antes GP3 (danado); GP7 no se uso por chocar con el display
#     MISO     -> GP4
#     IRQ      -> (sin conectar)
#     GND      -> GND de la Pico
#     RST      -> GP5
#     VCC      -> 3V3(OUT) de la Pico
#
# Mapa de pines (ya no es un bloque perfectamente seguido, por el cambio
# de SCK/MOSI de mas arriba):
#   GP1, GP4, GP5 -> RFID (CS, MISO, RST)
#   GP6  - GP14   -> display 7 segmentos (9 pines, sin cambios)
#   GP18, GP19    -> RFID (SCK, MOSI)
#   GP20          -> LED
#   GP27          -> LDR (entrada analogica)
#
# Restriccion real de hardware (no se puede evitar): MISO/SCK/MOSI deben
# caer en un pin valido para el periferico SPI0:
#   MISO: GP0, GP4, GP16 o GP20   |   SCK: GP2, GP6, GP18 o GP22
#   MOSI: GP3, GP7, GP19 o GP23
# RST y CS son GPIO comunes (los maneja el driver por software), van en
# cualquier pin libre.

# Modulo RFID-RC522 (SPI0)
PIN_RFID_CS = 1                # GP1  (SDA/CS)
PIN_RFID_SCK = 18               # GP18 (SCK)  - pin valido de SPI0 (GP2 danado, GP6 lo usa el display)
PIN_RFID_MOSI = 19              # GP19 (MOSI) - pin valido de SPI0 (GP3 danado, GP7 lo usa el display)
PIN_RFID_MISO = 4               # GP4  (MISO) - pin valido de SPI0
PIN_RFID_RST = 5                # GP5  (RST)

# Display de 7 segmentos, 2 digitos.
#
# IMPORTANTE: el cableado fisico NO quedo en el orden logico a-g (se
# identifico cada pin con multimetro y cada GPIO resulto ir a un segmento
# distinto del esperado). Por eso se define como diccionario letra->GPIO
# en vez de una lista en orden: asi el orden fisico del cableado no le
# importa al resto del codigo, que arma la lista en orden a-g a partir de
# este diccionario.
PINES_POR_SEGMENTO = {
    "a": 8,    # GP8
    "b": 11,   # GP11
    "c": 6,    # GP6
    "d": 12,   # GP12
    "e": 16,   # GP16
    "f": 7,    # GP7
    "g": 9,    # GP9
}
PIN_DP = 10             # GP10 - punto decimal del display; no se usa, queda siempre apagado
PIN_COMUN_DECENAS = 13  # GP13 - pin comun asignado al hardware original (transistor Q1)
PIN_COMUN_UNIDADES = 14 # GP14 - pin comun asignado al hardware original (transistor Q2)
# GP15 quedo libre (no se uso al identificar el cableado real).

# LED verde (CORRECCION: GP20, no GP21)
PIN_LED = 20                    # GP20 - LED verde (via resistencia de 1k)

# LDR (entrada analogica, solo GP26/GP27/GP28 admiten ADC)
PIN_LDR = 27                  # GP27 - nodo entre 3.3V-LDR-10k-GND

# IMPORTANTE: no fue posible determinar, a partir de los archivos del
# proyecto, si el display fisico es de anodo comun o de catodo comun.
# Por defecto se asume catodo comun; confirmarlo con el display real.
DISPLAY_ANODO_COMUN = False  # False = catodo comun | True = anodo comun


# =============================================================================
# 2. PARAMETROS
# =============================================================================
UMBRAL_OSCURIDAD = 20000     # ajustar segun pruebas reales con la LDR
TIMEOUT_READID_MS = 10000    # 10 segundos para acercar la tarjeta
FRECUENCIA_REFRESCO_HZ = 200 # refresco del multiplexado del display


# Seguridad: evita cablear dos funciones al mismo pin al editar la configuracion.
_todos_los_pines = [PIN_LDR, PIN_LED, PIN_RFID_MISO, PIN_RFID_CS, PIN_RFID_SCK,
                    PIN_RFID_MOSI, PIN_RFID_RST, PIN_COMUN_DECENAS, PIN_COMUN_UNIDADES,
                    PIN_DP] + list(PINES_POR_SEGMENTO.values())
if len(set(_todos_los_pines)) != len(_todos_los_pines):
    raise ValueError("Pin repetido en la configuracion de pines")


