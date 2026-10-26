using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ShutterExposure.Core;
using ShutterExposure.Interfaces;

namespace ShutterExposure.CameraSystem
{
    /// <summary>
    /// Gestiona la cámara física en mano del paciente.
    /// Alterna entre postura de descanso (visión despejada) y postura de encare (apuntado).
    /// Controla la reserva de hasta 3 baterías y el destello defensivo (Flash) contra El Acechador.
    /// </summary>
    public class HandheldCamera : MonoBehaviour
    {
        [Header("Inventario de Baterías")]
        [SerializeField] private int maxBatteries = 3;
        [SerializeField] private int startingBatteries = 1;
        private int _currentBatteries;

        [Header("Configuración del Flash Defensivo")]
        [SerializeField] private Light flashLight;
        [SerializeField] private float flashIntensity = 28f;
        [SerializeField] private float flashRange = 20f;
        [SerializeField] private float flashAngle = 65f;
        [SerializeField] private float flashDuration = 0.18f;
        [SerializeField] private float flashCooldown = 2.5f;
        [SerializeField] private float stalkerStunDuration = 4.0f;
        [SerializeField] private LayerMask flashTargetMask = ~0;

        [Header("Posturas (Descanso vs. Apuntado)")]
        [SerializeField] private Transform cameraMeshHolder;
        [SerializeField] private Vector3 restLocalPosition = new Vector3(0.24f, -0.22f, 0.42f);
        [SerializeField] private Vector3 restLocalEuler = new Vector3(6f, -10f, 0f);
        [SerializeField] private Vector3 aimLocalPosition = new Vector3(0f, -0.04f, 0.32f);
        [SerializeField] private Vector3 aimLocalEuler = Vector3.zero;
        [SerializeField] private float transitionSpeed = 10f;

        private bool _isAiming;
        private bool _isFlashOnCooldown;
        private Coroutine _flashRoutine;

        public int CurrentBatteries => _currentBatteries;
        public int MaxBatteries => maxBatteries;
        public bool IsAiming => _isAiming;
        public bool IsOnCooldown => _isFlashOnCooldown;

        public event Action<int, int> OnBatteryCountChanged; // (current, max)
        public event Action OnFlashFired;
        public event Action OnFlashCooldownComplete;

        private void Awake()
        {
            _currentBatteries = Mathf.Clamp(startingBatteries, 0, maxBatteries);

            if (flashLight != null)
            {
                flashLight.enabled = false;
                flashLight.range = flashRange;
                flashLight.spotAngle = flashAngle;
            }
        }

        private void Start()
        {
            OnBatteryCountChanged?.Invoke(_currentBatteries, maxBatteries);
        }

        private void Update()
        {
            HandleAimInput();
            UpdatePostureTransform();
            HandleFlashInput();
        }

        private void HandleAimInput()
        {
            bool aim = false;
            if (Mouse.current != null && Mouse.current.rightButton.isPressed) aim = true;
            if (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed) aim = true;

            _isAiming = aim;
        }

        private void UpdatePostureTransform()
        {
            if (cameraMeshHolder == null) return;

            Vector3 targetPos = _isAiming ? aimLocalPosition : restLocalPosition;
            Quaternion targetRot = Quaternion.Euler(_isAiming ? aimLocalEuler : restLocalEuler);

            cameraMeshHolder.localPosition = Vector3.Lerp(cameraMeshHolder.localPosition, targetPos, Time.deltaTime * transitionSpeed);
            cameraMeshHolder.localRotation = Quaternion.Slerp(cameraMeshHolder.localRotation, targetRot, Time.deltaTime * transitionSpeed);
        }

        private void HandleFlashInput()
        {
            bool triggerFlash = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) triggerFlash = true;
            if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame) triggerFlash = true;

            if (triggerFlash)
            {
                TryTriggerFlash();
            }
        }

        public bool TryTriggerFlash()
        {
            if (_isFlashOnCooldown) return false;
            if (_currentBatteries <= 0) return false;

            // Consumir 1 batería
            _currentBatteries--;
            OnBatteryCountChanged?.Invoke(_currentBatteries, maxBatteries);

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashBurstRoutine());

            // Detección de entidades afectadas por el flash en el cono de visión
            DetectAndStunTargets();

            // Emitir sonido del destello
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.EmitNoise(transform.position, 12f, NoiseType.CameraFlashCharge);
            }

            OnFlashFired?.Invoke();
            return true;
        }

        private IEnumerator FlashBurstRoutine()
        {
            _isFlashOnCooldown = true;

            if (flashLight != null)
            {
                flashLight.enabled = true;
                flashLight.intensity = flashIntensity;

                float elapsed = 0f;
                while (elapsed < flashDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / flashDuration;
                    flashLight.intensity = Mathf.Lerp(flashIntensity, 0f, t);
                    yield return null;
                }

                flashLight.enabled = false;
            }

            // Cooldown de recarga del flash
            yield return new WaitForSeconds(flashCooldown);

            _isFlashOnCooldown = false;
            OnFlashCooldownComplete?.Invoke();
        }

        private void DetectAndStunTargets()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            Collider[] hits = Physics.OverlapSphere(origin, flashRange, flashTargetMask, QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                Vector3 dirToTarget = (col.bounds.center - origin).normalized;
                float angle = Vector3.Angle(forward, dirToTarget);

                if (angle <= flashAngle * 0.5f)
                {
                    // Comprobar si hay línea de visión directa (sin muros de por medio)
                    if (Physics.Raycast(origin, dirToTarget, out RaycastHit hit, flashRange, flashTargetMask))
                    {
                        if (hit.collider == col || hit.collider.transform.IsChildOf(col.transform))
                        {
                            var target = col.GetComponentInParent<IFlashTarget>();
                            target?.OnHitByFlash(origin, forward, flashIntensity, stalkerStunDuration);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Intenta añadir una batería al inventario (máximo 3).
        /// </summary>
        /// <returns>True si fue recogida, False si el inventario ya está lleno.</returns>
        public bool TryAddBattery()
        {
            if (_currentBatteries >= maxBatteries) return false;

            _currentBatteries++;
            OnBatteryCountChanged?.Invoke(_currentBatteries, maxBatteries);
            return true;
        }
    }
}
