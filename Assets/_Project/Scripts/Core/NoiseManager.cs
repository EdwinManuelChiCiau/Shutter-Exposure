using System;
using System.Collections.Generic;
using UnityEngine;
using ShutterExposure.Interfaces;

namespace ShutterExposure.Core
{
    /// <summary>
    /// Mediador global de eventos de sonido en el mundo.
    /// Desacopla por completo a emisores (jugador, pisadas, objetos físicos que caen, micrófono)
    /// de los receptores (IA del Acechador).
    /// </summary>
    public class NoiseManager : MonoBehaviour
    {
        public static NoiseManager Instance { get; private set; }

        private readonly List<INoiseReceiver> _registeredListeners = new List<INoiseReceiver>();

        public static event Action<Vector3, float, NoiseType> OnGlobalNoiseEmitted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterListener(INoiseReceiver receiver)
        {
            if (!_registeredListeners.Contains(receiver))
            {
                _registeredListeners.Add(receiver);
            }
        }

        public void UnregisterListener(INoiseReceiver receiver)
        {
            _registeredListeners.Remove(receiver);
        }

        /// <summary>
        /// Emite un evento de sonido con posición y radio de alcance.
        /// Notifica a todos los oyentes que se encuentren dentro del radio de audición.
        /// </summary>
        public void EmitNoise(Vector3 origin, float radius, NoiseType type)
        {
            OnGlobalNoiseEmitted?.Invoke(origin, radius, type);

            for (int i = _registeredListeners.Count - 1; i >= 0; i--)
            {
                var listener = _registeredListeners[i];
                if (listener is MonoBehaviour mb && mb != null)
                {
                    float distance = Vector3.Distance(origin, mb.transform.position);
                    if (distance <= radius)
                    {
                        // Intensidad inversamente proporcional a la distancia
                        float intensity = 1f - Mathf.Clamp01(distance / radius);
                        listener.OnNoiseHeard(origin, intensity, type);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Opcional para depurar en el editor
        }
#endif
    }
}
