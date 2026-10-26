using System;
using UnityEngine;

namespace ShutterExposure.Player
{
    /// <summary>
    /// Sistema cardíaco y de vida del paciente.
    /// Controla las 3 vidas, el ritmo cardíaco (BPM), los estados de miedo,
    /// el temblor de cámara y la muerte por paro cardíaco.
    /// </summary>
    public class CardiacSystem : MonoBehaviour
    {
        [Header("Vidas y Salud")]
        [SerializeField] private int maxLives = 3;
        private int _currentLives;

        [Header("Configuración de Ritmo Cardíaco (BPM)")]
        [SerializeField] private float baseBPM = 72f;
        [SerializeField] private float maxBPM = 185f;
        [SerializeField] private float sprintBPMIncrease = 35f;
        [SerializeField] private float stalkerProximityBPMIncrease = 50f;
        [SerializeField] private float bpmSmoothSpeed = 2.5f;

        private float _currentBPM;
        private float _targetBPM;
        private bool _isSprinting;
        private float _stalkerProximity; // 0 (lejos) a 1 (inmediato)

        public int CurrentLives => _currentLives;
        public int MaxLives => maxLives;
        public float CurrentBPM => _currentBPM;
        public bool IsAlive => _currentLives > 0;

        /// <summary>
        /// Multiplicador de velocidad de movimiento. Cuando queda 1 vida, el paciente renguea y es más lento.
        /// </summary>
        public float SpeedMultiplier
        {
            get
            {
                if (_currentLives == 1) return 0.68f;
                if (_currentLives == 2) return 0.90f;
                return 1.0f;
            }
        }

        /// <summary>
        /// Intensidad de temblor de cámara derivada del miedo y ritmo cardíaco.
        /// </summary>
        public float CameraTremorIntensity
        {
            get
            {
                if (!IsAlive) return 0f;
                float bpmFactor = Mathf.InverseLerp(baseBPM, maxBPM, _currentBPM);
                if (_currentLives == 1) return 0.45f + (bpmFactor * 0.45f);
                if (_currentLives == 2) return 0.15f + (bpmFactor * 0.25f);
                return bpmFactor * 0.2f;
            }
        }

        public event Action<int, int> OnLivesChanged; // (current, max)
        public event Action<float> OnBPMChanged;       // (currentBPM)
        public event Action<int> OnLifeLost;          // (remainingLives)
        public event Action OnCardiacArrest;          // Game Over

        private void Awake()
        {
            _currentLives = maxLives;
            _currentBPM = baseBPM;
            _targetBPM = baseBPM;
        }

        private void Update()
        {
            if (!IsAlive) return;

            CalculateTargetBPM();
            _currentBPM = Mathf.MoveTowards(_currentBPM, _targetBPM, bpmSmoothSpeed * 10f * Time.deltaTime);
            OnBPMChanged?.Invoke(_currentBPM);
        }

        private void CalculateTargetBPM()
        {
            float target = baseBPM;

            // Incremento permanente según el estado de salud restante
            if (_currentLives == 2) target += 25f;
            else if (_currentLives == 1) target += 55f;

            // Incrementos dinámicos por sprint y proximidad del Acechador
            if (_isSprinting) target += sprintBPMIncrease;
            target += _stalkerProximity * stalkerProximityBPMIncrease;

            _targetBPM = Mathf.Clamp(target, baseBPM, maxBPM);
        }

        public void SetSprinting(bool sprinting)
        {
            _isSprinting = sprinting;
        }

        public void SetStalkerProximity(float proximity01)
        {
            _stalkerProximity = Mathf.Clamp01(proximity01);
        }

        /// <summary>
        /// Llamado cuando el Acechador atrapa o ataca al jugador.
        /// Reduce una vida, dispara taquicardia o paro cardíaco.
        /// </summary>
        public void TakeAttackDamage()
        {
            if (!IsAlive) return;

            _currentLives--;
            _targetBPM = maxBPM;
            _currentBPM = maxBPM;

            OnLivesChanged?.Invoke(_currentLives, maxLives);
            OnLifeLost?.Invoke(_currentLives);

            if (_currentLives <= 0)
            {
                _currentLives = 0;
                OnCardiacArrest?.Invoke();
            }
        }

        public void RestoreFullHealth()
        {
            _currentLives = maxLives;
            _currentBPM = baseBPM;
            _targetBPM = baseBPM;
            OnLivesChanged?.Invoke(_currentLives, maxLives);
        }
    }
}
