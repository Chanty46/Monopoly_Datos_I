# Protocolo cliente-servidor — Monopoly Distribuido

- **Transporte:** TCP, puerto por defecto `5000`.
- **Formato:** texto UTF-8 (sin BOM), **un mensaje por línea** (`\n`), campos separados por `|`.
- **Autoridad:** el servidor (banco) valida y modifica todo el estado. El cliente solo *solicita*.
- **Errores:** siempre `ERROR|CODIGO|detalle`. Un error nunca cambia el estado del juego.

## 1. Cliente → Servidor

| Comando | Cuándo | Validaciones del servidor |
|---|---|---|
| `CONECTAR\|nombre` | Solo en el lobby | Nombre 1–20 caracteres (letras, números, espacio, `_ - .`), no repetido, hay cupo |
| `TIRAR_DADOS` | En su turno | Es su turno, no ha tirado ya en este turno |
| `COMPRAR_PROPIEDAD` | Tras tirar, si cayó en propiedad libre | Es su turno, ya tiró, propiedad sin dueño, saldo suficiente, (RFID válido si `--rfid`) |
| `NO_COMPRAR` | Igual que la anterior | Igual, sin saldo |
| `TERMINAR_TURNO` | En su turno, tras tirar | Es su turno, ya tiró (si había compra pendiente se toma como `NO_COMPRAR`) |
| `CONSULTAR_ESTADO` | Siempre | — |
| `CONSULTAR_TRANSACCIONES` `[\|MODO[\|filtro]]` | Siempre | `MODO` = `ANTIGUAS` (defecto), `RECIENTES`, `JUGADOR\|nombre`, `TIPO\|nombreTipo` |
| `REGISTRAR_TARJETA` | Después de `CONECTAR` | El servidor lee el UID en **su** lector RFID; el UID no puede estar en uso |
| `SALIR` | Siempre | Si está en partida queda eliminado |

## 2. Servidor → Cliente

### Respuestas y lobby
| Mensaje | Significado |
|---|---|
| `BIENVENIDO\|jugadoresRequeridos` | Al abrir la conexión |
| `CONECTADO\|nombre` | Registro aceptado |
| `JUGADOR_UNIDO\|nombre\|registrados\|requeridos` | (broadcast) |
| `JUGADOR_SALIO\|nombre\|registrados\|requeridos` | (broadcast, solo en lobby) |
| `LOBBY\|registrados\|requeridos\|nombres` | Respuesta a `CONSULTAR_ESTADO` antes de iniciar |
| `INFO\|ACERQUE_SU_TARJETA_RFID` | El servidor espera una tarjeta (10 s) |
| `TARJETA_REGISTRADA\|nombre\|uid` | UID asociado |
| `ERROR\|codigo\|detalle` | Ver códigos abajo |

### Eventos de partida (broadcast a todos)
| Mensaje | Significado |
|---|---|
| `INICIO\|cantidadJugadores\|saldoInicial` | Comienza la partida |
| `TURNO\|idJugador\|nombre\|numeroTurno` | Turno del jugador |
| `DADOS\|id\|nombre\|d1\|d2` | Resultado de los dados |
| `MOVER\|id\|nombre\|casillaDesde\|casillaHasta\|nombreCasilla` | Movimiento por el tablero |
| `REUBICADO\|id\|nombre\|casillaId\|nombreCasilla` | Movido por carta o policía |
| `EVENTO\|id\|nombre\|descripcionCarta` | Carta de evento ejecutada |
| `ENCARCELADO\|id\|nombre` · `EN_CARCEL\|id\|nombre\|turnosRestantes` · `LIBERADO\|id\|nombre` | Estado de cárcel |
| `PUEDE_COMPRAR\|id\|nombre\|casillaId\|nombreProp\|precio` | Propiedad libre disponible |
| `COMPRA\|id\|nombre\|casillaId\|nombreProp\|precio` · `NO_COMPRA\|id\|nombre\|nombreProp` | Decisión de compra |
| `NUEVA_TX\|turno\|tipo\|origen\|destino\|monto\|descripcion` | Se registró una transacción |
| `FIN_TURNO\|id\|nombre` | Terminó el turno |
| `BANCARROTA\|id\|nombre\|acreedor\|motivo` · `DESCONECTADO\|id\|nombre` | Jugador eliminado |
| `ESTADO\|turno\|jugadorEnTurno\|id,nombre,saldo,casilla,activo;...` | Estado tras cada acción importante |
| `DUENIOS\|casillaId,nombre,idDueno,hipotecada;...` | Propiedades compradas |
| `PATRIMONIO\|id\|nombre\|valor` · `GANADOR\|id\|nombre\|patrimonio\|motivo` · `FIN_PARTIDA\|texto` | Fin (`motivo` = `UNICO_SOBREVIVIENTE` o `LIMITE_DE_TURNOS`) |

### Historial (solo al solicitante)
`TX|id|turno|fechaHora|tipo|origen|destino|monto|descripcion` (uno por transacción) seguido de `TX_FIN|modo|cantidad`.

### Códigos de error
`NO_CONECTADO`, `NOMBRE_INVALIDO`, `YA_CONECTADO`, `NOMBRE_EN_USO`, `PARTIDA_LLENA`, `PARTIDA_EN_CURSO`, `PARTIDA_NO_INICIADA`, `PARTIDA_FINALIZADA`, `ELIMINADO`, `FUERA_DE_TURNO`, `YA_TIRO_DADOS`, `DEBE_TIRAR_PRIMERO`, `SIN_PROPIEDAD_DISPONIBLE`, `YA_TIENE_PROPIETARIO`, `SALDO_INSUFICIENTE`, `COMPRA_RECHAZADA`, `TARJETA_NO_LEIDA`, `TARJETA_INVALIDA`, `TARJETA_EN_USO`, `SIN_TARJETA_REGISTRADA`, `SIN_HARDWARE`, `FALTA_FILTRO`, `TIPO_INVALIDO`, `MODO_INVALIDO`, `COMANDO_DESCONOCIDO`, `ERROR_INTERNO`.

## 3. Ejemplo de un turno
```
C→S  TIRAR_DADOS
S→*  DADOS|1|Ana|3|4
S→*  MOVER|1|Ana|0|7|Suerte
S→*  EVENTO|1|Ana|Recibes $50 de regalo.
S→*  NUEVA_TX|1|GananciaPorEvento|Banco|Ana|50|Recibes $50 de regalo.
S→*  ESTADO|1|Ana|1,Ana,1550,7,1;2,Beto,1500,0,1
C→S  TERMINAR_TURNO
S→*  FIN_TURNO|1|Ana
S→*  TURNO|2|Beto|2
```

## 4. Manual de ejecución
```bash
# Computadora A (banco): 4 jugadores, 100 turnos máximo, hardware simulado
dotnet run -- --server --puerto 5000 --jugadores 4 --turnos 100
# Con la Pico real y tarjeta obligatoria para comprar:
dotnet run -- --server --pico --rfid          # o:  --pico COM5  /  --pico /dev/ttyACM0

# Cualquier computadora (incluida la A, en otra terminal): un cliente por jugador
dotnet run -- --client <IP_DEL_SERVIDOR> 5000
```
Abrir el puerto TCP en el firewall de la computadora del servidor. En el menú del servidor: `6` simula acercar una tarjeta cuando se usa el hardware simulado.
