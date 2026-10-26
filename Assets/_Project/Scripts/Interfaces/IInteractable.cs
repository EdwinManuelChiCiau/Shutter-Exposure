using UnityEngine;

namespace ShutterExposure.Interfaces
{
    /// <summary>
    /// Contrato para cualquier objeto interactivo en el mundo del juego.
    /// Diseñado para soportar la mecánica de interacción por tensión (Hold-to-Interact).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Mensaje contextual que se muestra en pantalla (Ej. "Mantén presionado para recoger batería").
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Tiempo en segundos requerido para completar la interacción (por defecto 1.0s para tensión).
        /// </summary>
        float HoldDuration { get; }

        /// <summary>
        /// Indica si la interacción está actualmente habilitada.
        /// </summary>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Invocado cuando el jugador inicia el sostenimiento de la interacción.
        /// </summary>
        void OnInteractStart(GameObject interactor);

        /// <summary>
        /// Invocado si el jugador suelta el botón antes de cumplir el tiempo de tensión.
        /// </summary>
        void OnInteractCancel(GameObject interactor);

        /// <summary>
        /// Invocado cuando se completa con éxito el tiempo de tensión requerido.
        /// </summary>
        void OnInteractComplete(GameObject interactor);
    }
}
