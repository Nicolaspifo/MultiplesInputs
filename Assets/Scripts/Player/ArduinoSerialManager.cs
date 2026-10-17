using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

namespace NetworkingLab.Player
{
    /// <summary>
    /// Opens the Arduino serial port once and shares the parsed data between all players.
    /// Expected line format (see Arduino sketch): "x1,y1,b1,x2,y2,b2".
    /// Raw potentiometer values are mapped to the [-1, 1] range.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArduinoSerialManager : MonoBehaviour
    {
        public const int PlayerCount = 2;
        private const int ValuesPerPlayer = 3;

        [Header("Puerto serie")]
        [Tooltip("Ej: COM3. Si se deja vacio y 'Detectar' esta activo se usa el primer puerto disponible.")]
        [SerializeField] private string portName = "";
        [SerializeField, Min(9600)] private int baudRate = 115200;
        [Tooltip("Si el puerto esta vacio, intenta usar el primer puerto serie detectado.")]
        [SerializeField] private bool detectAutomatically = true;

        [Header("Estado")]
        [SerializeField] private bool logStatus = true;

        public static ArduinoSerialManager Instance { get; private set; }

        private readonly Vector2[] movement = new Vector2[PlayerCount];
        private readonly bool[] buttons = new bool[PlayerCount];
        private readonly Queue<string> pendingLines = new Queue<string>();
        private readonly object lineLock = new object();

        private SerialPort port;
        private Thread reader;
        private volatile bool running;
        private string status = "Sin conexion";
        private int parsedLines;
        private float nextReconnectTime;

        public bool IsConnected => port != null && port.IsOpen;
        public string Status => status;
        public string ConnectedPortName => port != null && port.IsOpen ? port.PortName : string.Empty;

        public Vector2 Movement(int playerIndex)
        {
            return playerIndex >= 0 && playerIndex < PlayerCount ? movement[playerIndex] : Vector2.zero;
        }

        public bool ButtonDown(int playerIndex)
        {
            return playerIndex >= 0 && playerIndex < PlayerCount && buttons[playerIndex];
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            TryOpen();
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Close();
        }

        private void Update()
        {
            if (!IsConnected)
            {
                if (Time.unscaledTime >= nextReconnectTime)
                {
                    nextReconnectTime = Time.unscaledTime + 2f;
                    TryOpen();
                }

                return;
            }

            DrainLines();

            bool showStatus = logStatus && (int)(Time.unscaledTime * 0.5f) != (int)((Time.unscaledTime - Time.unscaledDeltaTime) * 0.5f);
            if (showStatus)
            {
                status = "Conectado a " + port.PortName + " (" + parsedLines + " tramas)";
            }
        }

        private void TryOpen()
        {
            if (IsConnected)
            {
                return;
            }

            string resolved = ResolvePortName();
            if (string.IsNullOrEmpty(resolved))
            {
                status = "No se encontro ningun puerto serie";
                return;
            }

            try
            {
                var candidate = new SerialPort(resolved, baudRate)
                {
                    ReadTimeout = 100,
                    WriteTimeout = 100,
                    NewLine = "\n",
                    DtrEnable = true,
                    RtsEnable = true
                };
                candidate.Open();
                candidate.DiscardInBuffer();

                port = candidate;
                parsedLines = 0;
                running = true;
                status = "Conectado a " + resolved;

                reader = new Thread(ReadLoop) { IsBackground = true, Name = "ArduinoSerialReader" };
                reader.Start();

                Debug.Log("[ArduinoSerialManager] Conectado a " + resolved + " @ " + baudRate);
            }
            catch (Exception exception)
            {
                status = "Error abriendo el puerto: " + exception.Message;
                Debug.LogWarning("[ArduinoSerialManager] " + status);
                port = null;
            }
        }

        private string ResolvePortName()
        {
            if (!detectAutomatically && !string.IsNullOrWhiteSpace(portName))
            {
                return portName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(portName))
            {
                string configured = portName.Trim();
                string[] available = SerialPort.GetPortNames();
                foreach (string name in available)
                {
                    if (string.Equals(name, configured, StringComparison.OrdinalIgnoreCase))
                    {
                        return configured;
                    }
                }
            }

            string[] ports = SerialPort.GetPortNames();
            if (ports.Length > 0)
            {
                Array.Sort(ports, ComparePortNames);
                return ports[0];
            }

            return null;
        }

        private static int ComparePortNames(string left, string right)
        {
            int leftNumber = ParsePortNumber(left);
            int rightNumber = ParsePortNumber(right);
            return leftNumber.CompareTo(rightNumber);
        }

        private static int ParsePortNumber(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }

            string digits = name.TrimStart('C', 'O', 'M', 'c', 'o', 'm');
            return int.TryParse(digits, out int value) ? value : 0;
        }

        private void ReadLoop()
        {
            while (running)
            {
                try
                {
                    string line = port.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    lock (lineLock)
                    {
                        pendingLines.Enqueue(line);

                        while (pendingLines.Count > 50)
                        {
                            pendingLines.Dequeue();
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Sin datos todavia, se reintenta.
                }
                catch (Exception)
                {
                    running = false;
                }
            }
        }

        private void DrainLines()
        {
            while (true)
            {
                string line;
                lock (lineLock)
                {
                    if (pendingLines.Count == 0)
                    {
                        return;
                    }

                    line = pendingLines.Dequeue();
                }

                ParseLine(line);
            }
        }

        private void ParseLine(string line)
        {
            string[] parts = line.Trim().Split(',');
            if (parts.Length < PlayerCount * ValuesPerPlayer)
            {
                return;
            }

            for (int player = 0; player < PlayerCount; player++)
            {
                int offset = player * ValuesPerPlayer;
                if (!TryParseFloat(parts[offset], out float x) ||
                    !TryParseFloat(parts[offset + 1], out float y) ||
                    !TryParseFloat(parts[offset + 2], out float button))
                {
                    return;
                }

                movement[player] = new Vector2(
                    NormalizeAxis(x),
                    NormalizeAxis(y));
                buttons[player] = button > 0.5f;
            }

            parsedLines++;
        }

        private static float NormalizeAxis(float raw)
        {
            return Mathf.Clamp((raw / 1023f) * 2f - 1f, -1f, 1f);
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private void Close()
        {
            running = false;

            Thread currentReader = reader;
            reader = null;

            try
            {
                port?.Close();
            }
            catch (Exception)
            {
                // El puerto ya estaba cerrado.
            }

            port = null;
            currentReader?.Join(200);
            status = "Sin conexion";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                portName = "";
            }
        }
#endif
    }
}
