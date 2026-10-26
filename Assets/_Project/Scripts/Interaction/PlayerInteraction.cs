using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ShutterExposure.Interfaces;

namespace ShutterExposure.Interaction
{
    /// <summary>
    /// Sistema de interacción frontal mediante Raycast con mecánica de tensión (Hold-to-Interact).
    /// El jugador debe sostener el botón durante el tiempo especificado (1 segundo) para completar acciones.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Configuración de Raycast")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float rayDistance = 3.5f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private IInteractable _currentInteractable;
        private float _holdTimer;
        private bool _isInteracting;

        public IInteractable CurrentInteractable => _currentInteractable;
        public float CurrentHoldProgress => _currentInteractable != null && _currentInteractable.HoldDuration > 0
            ? Mathf.Clamp01(_holdTimer / _currentInteractable.HoldDuration)
            : 0f;

        public event Action<string> OnInteractableHovered;     // (prompt)
        public event Action OnInteractableUnhovered;
        public event Action<float> OnHoldProgressChanged;       // (progress 0 a 1)
        public event Action OnInteractionCompleted;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            CheckForInteractable();
            HandleInteractionInput();
        }

        private void CheckForInteractable()
        {
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableMask, QueryTriggerInteraction.Collide))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    if (_currentInteractable != interactable)
                    {
                        ResetInteraction();
                        _currentInteractable = interactable;
                        OnInteractableHovered?.Invoke(_currentInteractable.InteractionPrompt);
                    }
                    return;
                }
            }

            // Si perdimos el objetivo con el raycast
            if (_currentInteractable != null)
            {
                ResetInteraction();
                _currentInteractable = null;
                OnInteractableUnhovered?.Invoke();
            }
        }

        private void HandleInteractionInput()
        {
            if (_currentInteractable == null) return;

            bool isHeld = ReadInteractInput();

            if (isHeld)
            {
                if (!_isInteracting)
                {
                    _isInteracting = true;
                    _currentInteractable.OnInteractStart(gameObject);
                }

                _holdTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_holdTimer / _currentInteractable.HoldDuration);
                OnHoldProgressChanged?.Invoke(progress);

                if (_holdTimer >= _currentInteractable.HoldDuration)
                {
                    _currentInteractable.OnInteractComplete(gameObject);
                    OnInteractionCompleted?.Invoke();
                    ResetInteraction();
                }
            }
            else
            {
                if (_isInteracting)
                {
                    _currentInteractable.OnInteractCancel(gameObject);
                    ResetInteraction();
                }
            }
        }

        private bool ReadInteractInput()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.isPressed) return true;
            if (Gamepad.current != null && (Gamepad.current.buttonWest.isPressed || Gamepad.current.buttonSouth.isPressed || Gamepad.current.rightShoulder.isPressed)) return true;
            return false;
        }

        private void ResetInteraction()
        {
            _holdTimer = 0f;
            _isInteracting = false;
            OnHoldProgressChanged?.Invoke(0f);
        }
    }
}
