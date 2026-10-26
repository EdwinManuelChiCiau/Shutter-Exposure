using UnityEngine;
using UnityEngine.InputSystem;

namespace ShutterExposure.Player
{
    /// <summary>
    /// Control de rotación de cámara en 1ra persona, head bob y temblor cardíaco.
    /// Soporta mouse y gamepad (GameSir).
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [Header("Sensibilidad")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float gamepadSensitivity = 140f;
        [SerializeField] private float pitchLimit = 82f;

        [Header("Referencias")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform cameraTransform;

        [Header("Head Bobbing (Caminar / Correr)")]
        [SerializeField] private float bobFrequency = 8.5f;
        [SerializeField] private float bobHorizontalAmplitude = 0.035f;
        [SerializeField] private float bobVerticalAmplitude = 0.055f;

        private float _pitch;
        private float _bobTimer;
        private Vector3 _originalCamLocalPos;
        private PlayerMovement _playerMovement;
        private CardiacSystem _cardiacSystem;

        private void Awake()
        {
            if (playerBody == null) playerBody = transform;
            _playerMovement = GetComponent<PlayerMovement>();
            _cardiacSystem = GetComponent<CardiacSystem>();

            if (cameraTransform != null)
            {
                _originalCamLocalPos = cameraTransform.localPosition;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // No procesar rotación de cámara si el juego está pausado
            if (Time.timeScale <= 0f) return;

            HandleRotation();
            HandleHeadBobAndTremor();
        }

        private void HandleRotation()
        {
            Vector2 lookInput = Vector2.zero;

            if (Mouse.current != null)
            {
                lookInput += Mouse.current.delta.ReadValue() * mouseSensitivity;
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                lookInput += stick * (gamepadSensitivity * Time.deltaTime);
            }

            // Rotación vertical (Pitch) en la cámara
            _pitch -= lookInput.y;
            _pitch = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            // Rotación horizontal (Yaw) en el cuerpo del jugador
            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * lookInput.x);
            }
        }

        private void HandleHeadBobAndTremor()
        {
            if (cameraTransform == null) return;

            Vector3 targetOffset = Vector3.zero;

            // 1. Head bobbing al caminar/correr
            if (_playerMovement != null && _playerMovement.IsMoving)
            {
                float speedFactor = _playerMovement.IsSprinting ? 1.4f : (_playerMovement.IsCrouching ? 0.7f : 1f);
                _bobTimer += Time.deltaTime * (bobFrequency * speedFactor);

                float hBob = Mathf.Cos(_bobTimer) * bobHorizontalAmplitude;
                float vBob = Mathf.Sin(_bobTimer * 2f) * bobVerticalAmplitude;

                targetOffset = new Vector3(hBob, vBob, 0f);
            }
            else
            {
                _bobTimer = 0f;
            }

            // 2. Temblor derivado de miedo y taquicardia
            if (_cardiacSystem != null)
            {
                float tremor = _cardiacSystem.CameraTremorIntensity;
                if (tremor > 0.05f)
                {
                    float shakeX = (Mathf.PerlinNoise(Time.time * 22f, 0f) - 0.5f) * tremor * 0.06f;
                    float shakeY = (Mathf.PerlinNoise(0f, Time.time * 22f) - 0.5f) * tremor * 0.06f;
                    targetOffset += new Vector3(shakeX, shakeY, 0f);
                }
            }

            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, _originalCamLocalPos + targetOffset, Time.deltaTime * 12f);
        }

        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
