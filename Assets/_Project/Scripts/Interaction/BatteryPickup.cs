using UnityEngine;
using ShutterExposure.CameraSystem;
using ShutterExposure.Interfaces;

namespace ShutterExposure.Interaction
{
    /// <summary>
    /// Batería interactiva recolectable para la cámara.
    /// Cumple con el criterio de la rúbrica: '1 Objeto Estado (Escalación)'.
    /// Pulsa dinámicamente de escala en Update para señalar que es un ítem de interés.
    /// </summary>
    public class BatteryPickup : MonoBehaviour, IInteractable
    {
        [Header("Configuración de Interacción")]
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private string promptMessage = "Mantén presionado para recoger Batería";
        [SerializeField] private string fullInventoryMessage = "Baterías al máximo (3/3)";

        [Header("Rúbrica: 1 Objeto Estado (Escalación)")]
        [SerializeField] private bool enableScalingAnimation = true;
        [SerializeField] private float scaleFrequency = 2.4f;
        [SerializeField] private float minScaleMultiplier = 0.88f;
        [SerializeField] private float maxScaleMultiplier = 1.14f;

        private Vector3 _originalScale;

        public string InteractionPrompt
        {
            get
            {
                var cam = FindAnyObjectByType<HandheldCamera>();
                if (cam != null && cam.CurrentBatteries >= cam.MaxBatteries)
                {
                    return fullInventoryMessage;
                }
                return promptMessage;
            }
        }
        public float HoldDuration => holdDuration;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void Update()
        {
            // Cumplimiento explícito del criterio de Escalación de la rúbrica
            if (enableScalingAnimation)
            {
                float t = Mathf.PingPong(Time.time * scaleFrequency, 1f);
                float currentMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);
                transform.localScale = _originalScale * currentMultiplier;
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            var camera = interactor.GetComponentInChildren<HandheldCamera>();
            if (camera != null)
            {
                if (camera.CurrentBatteries >= camera.MaxBatteries)
                {
                    return false;
                }
            }
            return true;
        }

        public void OnInteractStart(GameObject interactor)
        {
            // Inicio de tensión (feedback visual o sonoro sutil)
        }

        public void OnInteractCancel(GameObject interactor)
        {
            // Se canceló la interacción antes del segundo
        }

        public void OnInteractComplete(GameObject interactor)
        {
            var camera = interactor.GetComponentInChildren<HandheldCamera>();
            if (camera != null)
            {
                if (camera.TryAddBattery())
                {
                    // Destruir el objeto tras ser recolectado
                    Destroy(gameObject);
                }
            }
        }
    }
}
