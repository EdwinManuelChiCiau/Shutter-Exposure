using UnityEngine;
using ShutterExposure.Player;

namespace ShutterExposure.Audio
{
    /// <summary>
    /// Genera y reproduce el sonido audible de pisadas del jugador al caminar y correr.
    /// Incluye variación procedural de tono y volumen para un sonido natural de calzado sobre suelo de hospital.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FootstepAudioPlayer : MonoBehaviour
    {
        [Header("Configuración de Sonido")]
        [SerializeField] private float walkVolume = 0.45f;
        [SerializeField] private float sprintVolume = 0.85f;
        [SerializeField] private float crouchVolume = 0.18f;
        [SerializeField] private float minPitch = 0.88f;
        [SerializeField] private float maxPitch = 1.15f;

        [Header("Temporizador de Pisadas")]
        [SerializeField] private float walkInterval = 0.52f;
        [SerializeField] private float sprintInterval = 0.35f;

        private AudioSource _audioSource;
        private PlayerMovement _playerMovement;
        private CharacterController _characterController;
        private float _stepTimer;
        private AudioClip _generatedStepClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D estéreo en los oídos del jugador

            _playerMovement = GetComponent<PlayerMovement>();
            _characterController = GetComponent<CharacterController>();

            // Genera un clip procedural de pisada si no hay uno asignado
            CreateProceduralFootstepClip();
        }

        private void Update()
        {
            if (_playerMovement == null || !_playerMovement.IsMoving || !_characterController.isGrounded)
            {
                _stepTimer = 0.1f;
                return;
            }

            float currentInterval = _playerMovement.IsSprinting ? sprintInterval : walkInterval;
            if (_playerMovement.IsCrouching) currentInterval *= 1.4f;

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= currentInterval)
            {
                _stepTimer = 0f;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            float targetVolume = _playerMovement.IsSprinting ? sprintVolume : (_playerMovement.IsCrouching ? crouchVolume : walkVolume);

            _audioSource.pitch = Random.Range(minPitch, maxPitch);
            _audioSource.volume = targetVolume;
            _audioSource.PlayOneShot(_generatedStepClip != null ? _generatedStepClip : _audioSource.clip);
        }

        /// <summary>
        /// Genera un sonido de impacto sordo y corto (simulando pisada de suela sobre losa/cemento)
        /// para que funcione de inmediato sin requerir importar archivos de audio externos todavía.
        /// </summary>
        private void CreateProceduralFootstepClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                // Envolvente de decaimiento exponencial rápido
                float envelope = Mathf.Exp(-t * 22f);
                // Ruido amortiguado de baja frecuencia (thud)
                float noise = (Random.value * 2f - 1f) * 0.35f;
                float lowFreq = Mathf.Sin(t * Mathf.PI * 2f * 65f);
                samples[i] = (lowFreq + noise) * envelope * 0.7f;
            }

            _generatedStepClip = AudioClip.Create("ProceduralFootstep", sampleCount, 1, sampleRate, false);
            _generatedStepClip.SetData(samples, 0);
        }
    }
}
