/*
 * JuegoTanques - Control de 2 jugadores para Unity
 * -------------------------------------------------
 * Hardware:
 *   Jugador 1: potenciometro X -> A0, potenciometro Y -> A1, boton (disparo) -> pin 2
 *   Jugador 2: potenciometro X -> A2, potenciometro Y -> A3, boton (disparo) -> pin 3
 *
 * Los botones se conectan entre el pin y GND (se usan con INPUT_PULLUP,
 * por lo tanto se leen LOW cuando estan presionados).
 *
 * Protocolo serie (115200 baudios, una linea por ciclo):
 *   x1,y1,b1,x2,y2,b2\n
 *   - xN, yN: valor crudo del potenciometro (0..1023)
 *   - bN:     1 si el boton esta presionado, 0 si no
 *
 * Ejemplo: 512,512,0,512,512,0
 */

const uint8_t POT_P1_X = A0;
const uint8_t POT_P1_Y = A1;
const uint8_t BOTON_P1 = 2;

const uint8_t POT_P2_X = A2;
const uint8_t POT_P2_Y = A3;
const uint8_t BOTON_P2 = 3;

// Tiempo entre envios (microsegundos). 20000 us = 50 envios por segundo.
const unsigned long INTERVALO_ENVIO_US = 20000;

// Antirrebote de los botones (milisegundos).
const unsigned long DEBOUNCE_MS = 30;

// Numero de muestras para suavizar el ruido de los potenciometros.
const uint8_t MUESTRAS_POT = 8;

unsigned long ultimoEnvioUs = 0;

struct Boton
{
  uint8_t pin;
  bool estable;              // ultimo estado estable (true = presionado)
  bool lecturaAnterior;      // ultima lectura cruda
  unsigned long ultimoCambio; // instante del ultimo cambio crudo
};

Boton botonP1 = {BOTON_P1, false, false, 0};
Boton botonP2 = {BOTON_P2, false, false, 0};

void setup()
{
  Serial.begin(115200);

  pinMode(BOTON_P1, INPUT_PULLUP);
  pinMode(BOTON_P2, INPUT_PULLUP);

  // Estado inicial de los botones.
  botonP1.lecturaAnterior = leerBotonCrudo(botonP1.pin);
  botonP2.lecturaAnterior = leerBotonCrudo(botonP2.pin);
  botonP1.estable = botonP1.lecturaAnterior;
  botonP2.estable = botonP2.lecturaAnterior;
}

void loop()
{
  unsigned long ahoraUs = micros();
  if (ahoraUs - ultimoEnvioUs < INTERVALO_ENVIO_US)
  {
    return;
  }
  ultimoEnvioUs = ahoraUs;

  int p1x = leerPotenciometro(POT_P1_X);
  int p1y = leerPotenciometro(POT_P1_Y);
  int p2x = leerPotenciometro(POT_P2_X);
  int p2y = leerPotenciometro(POT_P2_Y);

  bool b1 = actualizarBoton(botonP1, millis());
  bool b2 = actualizarBoton(botonP2, millis());

  Serial.print(p1x);
  Serial.print(',');
  Serial.print(p1y);
  Serial.print(',');
  Serial.print(b1 ? 1 : 0);
  Serial.print(',');
  Serial.print(p2x);
  Serial.print(',');
  Serial.print(p2y);
  Serial.print(',');
  Serial.println(b2 ? 1 : 0);
}

// Lee un potenciometro promediando varias muestras para reducir el ruido.
int leerPotenciometro(uint8_t pin)
{
  long suma = 0;
  for (uint8_t i = 0; i < MUESTRAS_POT; i++)
  {
    suma += analogRead(pin);
  }
  return (int)(suma / MUESTRAS_POT);
}

// Devuelve true cuando el boton esta presionado (INPUT_PULLUP -> LOW).
bool leerBotonCrudo(uint8_t pin)
{
  return digitalRead(pin) == LOW;
}

// Antirrebote: solo cambia el estado estable si la lectura se mantiene.
bool actualizarBoton(Boton &boton, unsigned long ahoraMs)
{
  bool lectura = leerBotonCrudo(boton.pin);

  if (lectura != boton.lecturaAnterior)
  {
    boton.ultimoCambio = ahoraMs;
    boton.lecturaAnterior = lectura;
  }

  if ((ahoraMs - boton.ultimoCambio) >= DEBOUNCE_MS)
  {
    boton.estable = lectura;
  }

  return boton.estable;
}
