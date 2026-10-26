using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ShutterExposure.Audio;
using ShutterExposure.Player;

namespace ShutterExposure.UI
{
    /// <summary>
    /// Menú de pausa del juego.
    /// Congela el tiempo, desbloquea el cursor, pausa el micrófono para evitar penalizaciones
    /// y ofrece botones de Reanudar, Reiniciar y Salir.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Panel de Pausa")]
        [SerializeField] private GameObject pausePanel;

        private bool _isPaused;
        private PlayerLook _playerLook;

        public bool IsPaused => _isPaused;

        private void Awake()
        {
            _playerLook = FindAnyObjectByType<PlayerLook>();
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void Update()
        {
            bool togglePause = false;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) togglePause = true;
            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) togglePause = true;

            if (togglePause)
            {
                if (_isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        public void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            // Desbloquear cursor para usar botones
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Pausar micrófono para que el jugador no sea detectado mientras está en el menú
            if (MicrophoneInputManager.Instance != null)
            {
                MicrophoneInputManager.Instance.StopMicrophone();
            }
        }

        public void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            // Volver a bloquear cursor para primera persona
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Reanudar captura del micrófono
            if (MicrophoneInputManager.Instance != null)
            {
                MicrophoneInputManager.Instance.InitializeMicrophone();
            }
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
