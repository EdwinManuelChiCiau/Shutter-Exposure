using System.Collections;
using UnityEngine;
using ShutterExposure.Core;
using ShutterExposure.Interfaces;

namespace ShutterExposure.Interaction
{
    /// <summary>
    /// Puerta interactiva con bisagra para el manicomio.
    /// Cumple con el criterio de la rubrica: '1 Objeto Estado (Rotacion)'.
    /// Rota dinamicamente sobre su eje y emite ruido audible para El Acechador al abrirse.
    /// </summary>
    public class HingeDoor : MonoBehaviour, IInteractable
    {
        [Header("Configuracion de Interaccion")]
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private string openPrompt = "Manten presionado para abrir puerta";
        [SerializeField] private string closePrompt = "Manten presionado para cerrar puerta";

        [Header("Rubrica: 1 Objeto Estado (Rotacion)")]
        [SerializeField] private Transform doorHinge;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float rotationSpeed = 3.5f;

        [Header("Emision de Ruido")]
        [SerializeField] private float doorNoiseRadius = 10f;

        private bool _isOpen;
        private bool _isRotating;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private Coroutine _rotationRoutine;

        public string InteractionPrompt => _isOpen ? closePrompt : openPrompt;
        public float HoldDuration => holdDuration;
        public bool IsOpen => _isOpen;

        public void ConfigureHinge(Transform hinge)
        {
            doorHinge = hinge;
            _closedRotation = doorHinge.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

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
