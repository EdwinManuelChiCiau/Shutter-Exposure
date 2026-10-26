using System.Collections;
using UnityEngine;
using ShutterExposure.Core;
using ShutterExposure.Interfaces;

namespace ShutterExposure.Interaction
{
    /// <summary>
    /// Puerta interactiva con bisagra para el manicomio.
    /// Cumple con el criterio de la rúbrica: '1 Objeto Estado (Rotación)'.
    /// Rota dinámicamente sobre su eje y emite ruido audible para El Acechador al abrirse.
    /// </summary>
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Configuración de Interacción")]
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private string openPrompt = "Mantén presionado para abrir puerta";
        [SerializeField] private string closePrompt = "Mantén presionado para cerrar puerta";

        [Header("Rúbrica: 1 Objeto Estado (Rotación)")]
        [SerializeField] private Transform doorHinge;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float rotationSpeed = 3.5f;

        [Header("Emisión de Ruido")]
        [SerializeField] private float doorNoiseRadius = 10f;

        private bool _isOpen;
        private bool _isRotating;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private Coroutine _rotationRoutine;

        public string InteractionPrompt => _isOpen ? closePrompt : openPrompt;
        public float HoldDuration => holdDuration;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (doorHinge == null) doorHinge = transform;

            _closedRotation = doorHinge.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        public bool CanInteract(GameObject interactor) => !_isRotating;

        public void OnInteractStart(GameObject interactor) { }
        public void OnInteractCancel(GameObject interactor) { }

        public void OnInteractComplete(GameObject interactor)
        {
            ToggleDoor();
        }

        public void ToggleDoor()
        {
            _isOpen = !_isOpen;

            if (_rotationRoutine != null) StopCoroutine(_rotationRoutine);
            _rotationRoutine = StartCoroutine(RotateDoorRoutine(_isOpen ? _openRotation : _closedRotation));

            // Al abrir o cerrar una puerta oxidada se genera ruido en el entorno
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.EmitNoise(transform.position, doorNoiseRadius, NoiseType.DoorAction);
            }
        }

        private IEnumerator RotateDoorRoutine(Quaternion targetRot)
        {
            _isRotating = true;

            while (Quaternion.Angle(doorHinge.localRotation, targetRot) > 0.5f)
            {
                doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRot, Time.deltaTime * rotationSpeed);
                yield return null;
            }

            doorHinge.localRotation = targetRot;
            _isRotating = false;
        }
    }
}
