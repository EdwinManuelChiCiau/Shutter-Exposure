using UnityEngine;

namespace ShutterExposure.Interfaces
{
    /// <summary>
    /// Contrato para entidades susceptibles a la luz defensiva del flash de la cámara.
    /// Permite que El Acechador o anomalías reaccionen sin que la cámara dependa de sus scripts concretos.
    /// </summary>
    public interface IFlashTarget
    {
        /// <summary>
        /// Recibe el impacto del destello del flash de la cámara.
        /// </summary>
        /// <param name="flashOrigin">Posición desde donde se disparó el flash.</param>
        /// <param name="flashDirection">Dirección frontal del flash.</param>
        /// <param name="intensity">Intensidad lumínica del destello.</param>
        /// <param name="stunDuration">Duración estimada del aturdimiento/ceguera.</param>
        void OnHitByFlash(Vector3 flashOrigin, Vector3 flashDirection, float intensity, float stunDuration);
    }
}
