using UnityEngine;

namespace ShutterExposure.Interfaces
{
    public enum NoiseType
    {
        FootstepWalk,
        FootstepSprint,
        PhysicsCollision,
        MicrophoneSpike,
        DoorAction,
        CameraFlashCharge
    }

    /// <summary>
    /// Contrato para cualquier objeto o acción que genere ruido audible en el mundo.
    /// Desacopla emisores (jugador, objetos con físicas) de los receptores (IA del Acechador).
    /// </summary>
    public interface INoiseEmitter
    {
        void EmitNoise(Vector3 origin, float radius, NoiseType type);
    }

    /// <summary>
    /// Contrato para entidades que escuchan ruido en el juego (ej. El Acechador).
    /// </summary>
    public interface INoiseReceiver
    {
        void OnNoiseHeard(Vector3 noisePosition, float noiseIntensity, NoiseType type);
    }
}
