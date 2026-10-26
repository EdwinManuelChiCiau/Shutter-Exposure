using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using ShutterExposure.Core;
using ShutterExposure.Interfaces;

namespace ShutterExposure.Audio
{
    /// <summary>
    /// Captura en tiempo real el audio del micrófono físico del jugador.
    /// Calcula el nivel RMS (Root Mean Square) y detecta picos de ruido que alertan a El Acechador.
    /// Soporta Hot-Swapping (cambio/reconexión de micrófono en caliente) y pausa del juego.
    /// </summary>
    public class MicrophoneInputManager : MonoBehaviour
    {
        public static MicrophoneInputManager Instance { get; private set; }

        [Header("Configuración de Muestreo")]
        [SerializeField] private int sampleRate = 44100;
        [SerializeField] private int sampleWindow = 256;

        [Header("Umbral de Ruido")]
        [Tooltip("Nivel RMS a partir del cual el sonido es considerado delator para El Acechador.")]
        [SerializeField] private float noiseThreshold = 0.045f;
        [SerializeField] private float noiseRadius = 25f;
        [SerializeField] private float noiseCooldown = 0.5f;

        [Header("Hot-Swapping de Dispositivos")]
        [SerializeField] private float deviceCheckInterval = 2.5f;

        // Estado interno
        private AudioClip _micClip;
        private string _activeDevice;
        private string[] _lastKnownDevices;
        private float[] _samples;
        private float _lastNoiseTime;
        private bool _isInitialized;
        private bool _isCalibrating;
        private float _calibrationAccumulator;
        private int _calibrationSamples;

        // Propiedades públicas
        public float CurrentRMS { get; private set; }
        public float NoiseThreshold => noiseThreshold;
        public bool HasMicrophone => Microphone.devices.Length > 0;
        public string ActiveDeviceName => _activeDevice;
        public bool IsCalibrating => _isCalibrating;

        // Eventos
        public event Action<float> OnRMSUpdated;
        public event Action<float> OnNoiseSpikeDetected; // (rmsLevel)
        public event Action<string> OnDeviceChanged;
        public event Action<float> OnCalibrationCompleted; // (newThreshold)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _samples = new float[sampleWindow];
        }

        private void Start()
        {
            InitializeMicrophone();
            StartCoroutine(DeviceWatcherRoutine());
        }

        private void OnDestroy()
        {
            StopMicrophone();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !_isInitialized)
            {
                InitializeMicrophone();
            }
        }

        public void InitializeMicrophone()
        {
            StopMicrophone();

            _lastKnownDevices = Microphone.devices;
            if (_lastKnownDevices.Length == 0)
            {
                Debug.LogWarning("[MicrophoneInputManager] No se detectó ningún micrófono conectado.");
                _isInitialized = false;
                return;
            }

            // Seleccionar el micrófono predeterminado del sistema operativo
            _activeDevice = _lastKnownDevices[0];
            _micClip = Microphone.Start(_activeDevice, true, 1, sampleRate);
            _isInitialized = true;

            OnDeviceChanged?.Invoke(_activeDevice);
            Debug.Log($"[MicrophoneInputManager] Micrófono iniciado exitosamente: {_activeDevice}");
        }

        public void StopMicrophone()
        {
            if (_isInitialized && !string.IsNullOrEmpty(_activeDevice))
            {
                Microphone.End(_activeDevice);
                _isInitialized = false;
            }
        }

        private void Update()
        {
            if (!_isInitialized || _micClip == null) return;

            // Ignorar y congelar si el juego está en pausa
            if (Time.timeScale <= 0f)
            {
                CurrentRMS = 0f;
                return;
            }

            CalculateRMS();
            CheckNoiseThreshold();
        }

        private void CalculateRMS()
        {
            int micPosition = Microphone.GetPosition(_activeDevice);
            int startPosition = micPosition - sampleWindow;

            if (startPosition < 0) return;

            _micClip.GetData(_samples, startPosition);

            // Cálculo matemático de RMS: Sqrt(Sum(samples^2) / N)
            float sum = 0f;
            for (int i = 0; i < sampleWindow; i++)
            {
                sum += _samples[i] * _samples[i];
            }

            CurrentRMS = Mathf.Sqrt(sum / sampleWindow);
            OnRMSUpdated?.Invoke(CurrentRMS);

            if (_isCalibrating)
            {
                _calibrationAccumulator += CurrentRMS;
                _calibrationSamples++;
            }
        }

        private void CheckNoiseThreshold()
        {
            if (_isCalibrating) return;

            if (CurrentRMS >= noiseThreshold && Time.time >= _lastNoiseTime + noiseCooldown)
            {
                _lastNoiseTime = Time.time;
                OnNoiseSpikeDetected?.Invoke(CurrentRMS);

                // Notificar al mediador acústico del mundo para atraer al Acechador
                if (NoiseManager.Instance != null)
                {
                    Vector3 playerPos = transform.position;
                    // Escalar el radio de alerta en base a qué tan fuerte fue el grito o ruido
                    float dynamicRadius = noiseRadius * Mathf.Clamp(CurrentRMS / noiseThreshold, 1f, 2.5f);
                    NoiseManager.Instance.EmitNoise(playerPos, dynamicRadius, NoiseType.MicrophoneSpike);
                }
            }
        }

        /// <summary>
        /// Corrutina de vigilancia para detectar desconexión o cambio de micrófono en caliente (Hot-swapping).
        /// </summary>
        private IEnumerator DeviceWatcherRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(deviceCheckInterval);

                string[] currentDevices = Microphone.devices;

                bool devicesChanged = _lastKnownDevices == null ||
                                      currentDevices.Length != _lastKnownDevices.Length ||
                                      !currentDevices.SequenceEqual(_lastKnownDevices);

                if (devicesChanged)
                {
                    Debug.Log("[MicrophoneInputManager] Cambio de dispositivos de audio detectado (Hot-swap). Reinicializando...");
                    InitializeMicrophone();
                }
            }
        }

        /// <summary>
        /// Inicia un proceso de calibración de 3 segundos para medir el ruido de fondo ambiente del jugador.
        /// </summary>
        public void StartAutoCalibration(float duration = 3.0f, float sensitivityMargin = 1.6f)
        {
            if (!_isInitialized) return;
            StartCoroutine(CalibrationRoutine(duration, sensitivityMargin));
        }

        private IEnumerator CalibrationRoutine(float duration, float margin)
        {
            _isCalibrating = true;
            _calibrationAccumulator = 0f;
            _calibrationSamples = 0;

            yield return new WaitForSecondsRealtime(duration);

            if (_calibrationSamples > 0)
            {
                float averageFloor = _calibrationAccumulator / _calibrationSamples;
                // El nuevo umbral se fija por encima del ruido ambiental detectado
                noiseThreshold = Mathf.Max(averageFloor * margin, 0.02f);
                OnCalibrationCompleted?.Invoke(noiseThreshold);
                Debug.Log($"[MicrophoneInputManager] Calibración completada. Nuevo umbral: {noiseThreshold:F4}");
            }

            _isCalibrating = false;
        }

        public void SetThreshold(float newThreshold)
        {
            noiseThreshold = Mathf.Clamp(newThreshold, 0.01f, 1.0f);
        }
    }
}
