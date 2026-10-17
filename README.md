# Tanques (2 jugadores con Arduino)

Juego local de tanques para Unity 6, controlado desde **un solo Arduino** con
**4 potenciometros y 2 botones** (2 potenciometros + 1 boton por jugador).

La escena jugable es **`Assets/Scenes/LocalGameplayBaseline.unity`**.
No hay nada de red ni online: todo es local y se juega en una sola maquina.

---

## 1. Montaje en Arduino

### 1.1 Materiales

- 1 placa Arduino (Uno, Nano, Mega o similar)
- 4 potenciometros (10kΩ recomendados)
- 2 botones pulsadores (conector de 2 patas)
- Cables jumper (macho-macho para la placa, macho-hembra o soldadura para los componentes)
- Cable USB para conectar el Arduino al PC

### 1.2 Conexion

| Jugador | Eje | Potenciometro | Pin Arduino |
|---------|-----|---------------|-------------|
| Player 1 | X (izquierda/derecha) | Pot 1 | `A0` |
| Player 1 | Y (adelante/atras) | Pot 2 | `A1` |
| Player 1 | Boton (disparo/PowerUp) | Boton 1 | `D2` |
| Player 2 | X (izquierda/derecha) | Pot 3 | `A2` |
| Player 2 | Y (adelante/atras) | Pot 4 | `A3` |
| Player 2 | Boton (disparo/PowerUp) | Boton 2 | `D3` |

Regla general para **cada potenciometro** (son 3 patas):

- Pata izquierda → `5V`
- Pata central (wiper) → la entrada analogica (`A0`, `A1`, `A2` o `A3`)
- Pata derecha → `GND`

Regla general para **cada boton** (son 2 patas):

- Una pata → el pin digital (`D2` o `D3`)
- La otra pata → `GND`

> Los botones se conectan a `GND` y **no** requieren resistencia externa: el
> codigo usa `INPUT_PULLUP`, es decir la placa internamente pone el pin en
> alto (1) hasta que presionas el boton, que lo pone en bajo (0). Si al
> presionar el boton ves el movimiento invertido, no hay problema: es el
> comportamiento esperado y Unity lo traduce correctamente.

### 1.3 Subir el sketch

1. Abre el archivo `Arduino/JuegoTanques/JuegoTanques.ino` en el IDE de Arduino.
2. En **Herramientas → Placa**, elige tu placa.
3. En **Herramientas → Puerto**, elige el puerto del Arduino (por ejemplo `COM3` en Windows).
4. Presiona **Verificar** y luego **Subir**.
5. Abre el **Monitor Serie** y configura `115200 baudios`. Debes ver una linea
   que cambia cuando mueves los potenciometros, con el formato:

   ```
   x1,y1,b1,x2,y2,b2
   ```

   - `x1`, `y1` = valor del potenciometro del jugador 1 (`0` a `1023`)
   - `b1` = `1` cuando el boton del jugador 1 esta presionado, `0` si no
   - `x2`, `y2`, `b2` = lo mismo para el jugador 2

   Ejemplo: `512,512,0,512,512,0`

6. **Cierra el Monitor Serie antes de abrir Unity**: el puerto serie solo puede
   ser abierto por un programa a la vez.

---

## 2. Como funciona en Unity

### 2.1 Que hay en la escena

- **ArduinoInput** → objeto con el componente `ArduinoSerialManager`. Es el unico
  que abre el puerto serie y comparte los datos con todos los jugadores.
- **tank-body** (Player 1) y **tank-body 2** (Player 2) → cada uno tiene un
  `ArduinoInputSource` con su `playerIndex` (`0` y `1`).
- **tank-body 3** (Player 3) y **tank-body 4** (Player 4) → siguen usando teclado,
  por si quieres jugar sin Arduino.

### 2.2 Controles

| Jugador | Dispositivo | Mover | Disparar / PowerUp |
|---------|-------------|-------|--------------------|
| Player 1 | Arduino | Pot `A0`/`A1` | Boton en `D2` |
| Player 2 | Arduino | Pot `A2`/`A3` | Boton en `D3` |
| Player 3 | Teclado | `I` `J` `K` `L` | `O` |
| Player 4 | Teclado | Numpad `8` `4` `5` `6` | Numpad `0` |

### 2.3 Pasos para jugar

1. Sube el sketch al Arduino (seccion 1.3) y **dejalo conectado**.
2. Abre el proyecto en **Unity 6000.6.0f1** (o la version 6.x que uses).
3. Abre la escena **`Assets/Scenes/LocalGameplayBaseline.unity`**.
4. Selecciona el objeto **`ArduinoInput`** en la jerarquia.
5. En el Inspector, en `Arduino Serial Manager`:
   - **Port Name**: escribe el puerto del Arduino, por ejemplo `COM3`.
     Si lo dejas vacio, Unity intentara usar el primer puerto serie disponible
     automaticamente.
   - **Baud Rate**: debe ser `115200` (igual que el sketch).
   - **Detect Automatically**: si esta activo y no pones puerto, se elige el primero.
6. Presiona **Play**.
7. En la consola deberias ver: `[ArduinoSerialManager] Conectado a COM3 @ 115200`.
8. Mueve los potenciometros: los dos primeros tanques se moveran. Presiona los
   botones: disparan y aparece el mensaje de PowerUp en pantalla.

### 2.4 Si no conecta

- Confirma que cerraste el Monitor Serie y cualquier otro programa que use el puerto.
- Revisa el puerto en el **Gerenciador de dispositivos** (Windows) → `Puertos (COM y LPT)`.
- Cambia el `Port Name` en el Inspector al puerto correcto.
- Prueba subiendo el Monitor Serie a `115200` para confirmar que el Arduino si envia datos.
- Verifica que el baud rate del sketch y el del Inspector coincidan.
- Si el mensaje en consola dice `No se encontro ningun puerto serie`, revisa el
  cable USB y los controladores del Arduino.

---

## 3. Como esta organizado el codigo

Todo el juego vive en la carpeta `Assets/Scripts/` dentro del assembly `NetworkingLab`.

### Entrada (input)

- `Assets/Scripts/Player/PlayerInputBase.cs`
  Clase base abstracta con lo minimo que el juego necesita: la propiedad
  `Movement` (Vector2) y el evento `PowerUpPressed`. Cualquier fuente de entrada
  la implementa y el gameplay no se entera de donde vienen los datos.
- `Assets/Scripts/Player/ArduinoSerialManager.cs`
  Abre el puerto serie en un hilo aparte, parsea la linea `x1,y1,b1,x2,y2,b2`
  y expone los datos ya normalizados al rango `-1..1`. Es un singleton
  (`ArduinoSerialManager.Instance`) para que un solo puerto se comparta entre
  los dos jugadores.
- `Assets/Scripts/Player/ArduinoInputSource.cs`
  Componente por jugador. Lee del `ArduinoSerialManager` usando su `playerIndex`,
  aplica la zona muerta del potenciometro y detecta el flanco de subida del boton
  para disparar el evento `PowerUpPressed`.
- `Assets/Scripts/Player/PlayerInputSource.cs`
  Igual que lo anterior pero para teclado, usando el Input System de Unity con el
  asset `Assets/Input/LocalPlayers.inputactions`. Solo lo usan Player 3 y Player 4.

### Gameplay

- `Player/PlayerController.cs` → mueve el tanque segun `Movement` y resuelve colisiones.
- `Player/MovementCollision.cs` → colision por ejes contra obstaculos.
- `Combat/TankWeapon.cs` → apunta y dispara el proyectil al recibir `PowerUpPressed`.
- `Combat/Projectile.cs`, `Combat/Health.cs` → proyectil y vida/respawn.
- `Combat/TankFacing.cs` → gira el tanche hacia donde se mueve.
- `PowerUp/PlayerPowerUp.cs`, `PowerUp/PowerUpEvents.cs` → mensaje de PowerUp.
- `UI/PowerUpMessageUI.cs` → muestra y limpia el mensaje en pantalla.
- `Editor/LocalBaselineSceneBuilder.cs` → menu **`Networking Lab/Build Local Baseline`**,
  que regenera la escena y el asset de input desde cero.

---

## 4. Notas tecnicas

- **Puerto serie unico**: `ArduinoSerialManager` es el unico que toca el puerto
  serie, por eso los dos jugadores pueden leerlo a la vez sin conflicto.
- **Hilo de lectura**: la lectura se hace en un hilo de fondo y se pasa al hilo
  principal de Unity en `Update`, asi el juego nunca se congela esperando datos.
- **Zona muerta**: por defecto `0.15`. Subela en el `ArduinoInputSource` del
  jugador si el tanque se mueve solo con el potenciometro en reposo.
- **Eje invertido**: si el tanque se mueve al reves de como giras el potenciometro,
  activa `Invert Y` en el `ArduinoInputSource` de ese jugador.
- **Reconexion**: si el Arduino se desconecta, Unity reintenta cada 2 segundos.
- **System.IO.Ports**: el proyecto usa el perfil de compatibilidad **.NET Framework**
  (Edit → Project Settings → Player → Other Settings → Api Compatibility Level),
  porque `System.IO.Ports` no existe en el perfil .NET Standard de Unity. Si ese
  ajuste se cambia a .NET Standard, volveran los errores
  `The type or namespace name 'Ports' does not exist` y `SerialPort could not be found`.
  Dejalo en **.NET Framework**.
