using UnityEngine;
using UnityEngine.InputSystem;
using ShutterExposure.Core;
using ShutterExposure.Interfaces;

namespace ShutterExposure.Player
{
    /// <summary>
    /// Controlador de movimiento en 1ra persona cinemático e interactivo con físicas.
    /// Soporta teclado/mouse y gamepad (GameSir) mediante el nuevo Input System.
    /// Incluye detección de inmovilidad (regla de los 60s) y emisión acústica de pisadas.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Velocidades")]
        [SerializeField] private float walkSpeed = 3.8f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float jumpHeight = 1.1f;

        [Header("Empuje Físico")]
        [SerializeField] private float pushPower = 2.5f;
        [SerializeField] private float minMassToPush = 0.1f;
        [SerializeField] private float maxMassToPush = 45f;

        [Header("Agacharse")]
        [SerializeField] private float standingHeight = 1.85f;
        [SerializeField] private float crouchingHeight = 1.1f;
        [SerializeField] private Transform cameraHolder;

        [Header("Emisión de Ruido de Pisadas")]
        [SerializeField] private float walkNoiseRadius = 4f;
        [SerializeField] private float sprintNoiseRadius = 16f;
        [SerializeField] private float stepInterval = 0.52f;
        [SerializeField] private float sprintStepMultiplier = 0.65f;

        [Header("Regla de Inmovilidad (60 segundos)")]
        [SerializeField] private float afkWarningThreshold = 60f;

        // Referencias
        private CharacterController _characterController;
        private StaminaSystem _staminaSystem;
        private CardiacSystem _cardiacSystem;

        // Estados
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isCrouching;
        private float _stepTimer;
        private float _immobileTimer;
        private Vector3 _lastCheckedPosition;

        // Propiedades públicas
        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching => _isCrouching;
        public float CurrentSpeed => _characterController.velocity.magnitude;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _staminaSystem = GetComponent<StaminaSystem>();
            _cardiacSystem = GetComponent<CardiacSystem>();
            _standingCameraY = cameraHolder != null ? cameraHolder.localPosition.y : 1.6f;
            _crouchingCameraY = _standingCameraY - (standingHeight - crouchingHeight);
        }

        private float _standingCameraY;
        private float _crouchingCameraY;

        private void Start()
        {
            _lastCheckedPosition = transform.position;
        }

        private void Update()
        {
            if (_cardiacSystem != null && !_cardiacSystem.IsAlive) return;

            HandleGroundAndGravity();
            Vector2 moveInput = ReadMovementInput();
            bool sprintHeld = ReadSprintInput();
            bool crouchTriggered = ReadCrouchInput();

            HandleCrouch(crouchTriggered);
            HandleMovement(moveInput, sprintHeld);
            HandleFootstepNoise();
            CheckImmobileTimer();
        }

        private Vector2 ReadMovementInput()
        {
            Vector2 input = Vector2.zero;

            // Soporte universal para Gamepad y Teclado
            if (Gamepad.current != null)
            {
                input = Gamepad.current.leftStick.ReadValue();
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private bool ReadSprintInput()
        {
            bool sprint = false;
            if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) sprint = true;
            if (Gamepad.current != null && (Gamepad.current.leftStickButton.isPressed || Gamepad.current.leftTrigger.isPressed)) sprint = true;
            return sprint;
        }

        private bool ReadCrouchInput()
        {
            if (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) return true;
            return false;
        }

        private bool ReadJumpInput()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            return false;
        }

        private void HandleGroundAndGravity()
        {
            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            if (_isGrounded && ReadJumpInput() && !_isCrouching)
            {
                if (_staminaSystem == null || _staminaSystem.TryConsumeJump())
                {
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (NoiseManager.Instance != null)
                    {
                        NoiseManager.Instance.EmitNoise(transform.position, 6f, NoiseType.FootstepWalk);
                    }
                }
            }

            _velocity.y += gravity * Time.deltaTime;
        }

        private void HandleCrouch(bool toggle)
        {
            if (toggle)
            {
                _isCrouching = !_isCrouching;
            }

            float targetHeight = _isCrouching ? crouchingHeight : standingHeight;
            _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * 10f);

            if (cameraHolder != null)
            {
                float targetY = _isCrouching ? _crouchingCameraY : _standingCameraY;
                Vector3 camPos = cameraHolder.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, targetY, Time.deltaTime * 10f);
                cameraHolder.localPosition = camPos;
            }
        }

        private void HandleMovement(Vector2 input, bool sprintRequested)
        {
            IsMoving = input.sqrMagnitude > 0.01f;

            // Determinar velocidad
            float speed = walkSpeed;

            if (_isCrouching)
            {
                speed = crouchSpeed;
                IsSprinting = false;
            }
            else if (sprintRequested && IsMoving && _staminaSystem != null && !_staminaSystem.IsExhausted)
            {
                if (_staminaSystem.TryConsumeSprint(Time.deltaTime))
                {
                    speed = sprintSpeed;
                    IsSprinting = true;
                }
                else
                {
                    IsSprinting = false;
                }
            }
            else
            {
                IsSprinting = false;
            }

            // Aplicar modificador de salud/miedo
            if (_cardiacSystem != null)
            {
                speed *= _cardiacSystem.SpeedMultiplier;
                _cardiacSystem.SetSprinting(IsSprinting);
            }

            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            _characterController.Move(moveDirection * (speed * Time.deltaTime) + _velocity * Time.deltaTime);
        }

        private void HandleFootstepNoise()
        {
            if (!IsMoving || !_isGrounded) return;

            float interval = IsSprinting ? (stepInterval * sprintStepMultiplier) : stepInterval;
            if (_isCrouching) interval *= 1.4f;

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= interval)
            {
                _stepTimer = 0f;

                // Emisión de ruido
                if (NoiseManager.Instance != null)
                {
                    float radius = _isCrouching ? 0.5f : (IsSprinting ? sprintNoiseRadius : walkNoiseRadius);
                    NoiseType type = IsSprinting ? NoiseType.FootstepSprint : NoiseType.FootstepWalk;
                    NoiseManager.Instance.EmitNoise(transform.position, radius, type);
                }
            }
        }

        private void CheckImmobileTimer()
        {
            // Solo cuenta si el juego no está en pausa
            if (Time.timeScale <= 0f) return;

            float distMoved = Vector3.Distance(transform.position, _lastCheckedPosition);
            if (distMoved > 0.4f)
            {
                _immobileTimer = 0f;
                _lastCheckedPosition = transform.position;
            }
            else
            {
                _immobileTimer += Time.deltaTime;
                if (_immobileTimer >= afkWarningThreshold)
                {
                    // Alerta del gatillo 3: Castigo por quedarse quieto más de 60 segundos
                    _immobileTimer = 0f; // reinicia el ciclo de alarma
                    if (NoiseManager.Instance != null)
                    {
                        // Emite una alerta de alta intensidad que atrae al Acechador
                        NoiseManager.Instance.EmitNoise(transform.position, 60f, NoiseType.MicrophoneSpike);
                    }
                }
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            if (hit.moveDirection.y < -0.3f) return;

            if (body.mass < minMassToPush || body.mass > maxMassToPush) return;

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            body.AddForceAtPosition(pushDir * pushPower, hit.point, ForceMode.Impulse);

            // Si el objeto empujado se mueve con fuerza, emitirá ruido
            if (body.linearVelocity.sqrMagnitude > 1.5f && NoiseManager.Instance != null)
            {
                NoiseManager.Instance.EmitNoise(hit.point, 6f, NoiseType.PhysicsCollision);
            }
        }
    }
}
