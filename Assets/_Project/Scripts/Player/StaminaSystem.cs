using System;
using UnityEngine;

namespace ShutterExposure.Player
{
    /// <summary>
    /// Gestiona la estamina del jugador para correr y saltar.
    /// Afecta directamente el ritmo cardíaco y las posibilidades de escape.
    /// </summary>
    public class StaminaSystem : MonoBehaviour
    {
        [Header("Configuración de Estamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float sprintDrainRate = 22f; // por segundo
        [SerializeField] private float jumpDrain = 15f;
        [SerializeField] private float regenRate = 18f; // por segundo
        [SerializeField] private float regenDelay = 1.2f;

        private float _currentStamina;
        private float _regenTimer;

        public float CurrentStamina => _currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaRatio => maxStamina > 0 ? _currentStamina / maxStamina : 0f;
        public bool IsExhausted => _currentStamina <= 0.05f;

        public event Action<float, float> OnStaminaChanged; // (current, max)
        public event Action OnExhausted;

        private void Awake()
        {
            _currentStamina = maxStamina;
        }

        private void Update()
        {
            if (_regenTimer > 0)
            {
                _regenTimer -= Time.deltaTime;
            }
            else if (_currentStamina < maxStamina)
            {
                ModifyStamina(regenRate * Time.deltaTime);
            }
        }

        public bool TryConsumeSprint(float deltaTime)
        {
            if (IsExhausted) return false;

            float cost = sprintDrainRate * deltaTime;
            ModifyStamina(-cost);
            _regenTimer = regenDelay;

            if (_currentStamina <= 0f)
            {
                OnExhausted?.Invoke();
            }

            return true;
        }

        public bool TryConsumeJump()
        {
            if (_currentStamina < jumpDrain) return false;

            ModifyStamina(-jumpDrain);
            _regenTimer = regenDelay;
            return true;
        }

        private void ModifyStamina(float amount)
        {
            _currentStamina = Mathf.Clamp(_currentStamina + amount, 0f, maxStamina);
            OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
        }
    }
}
