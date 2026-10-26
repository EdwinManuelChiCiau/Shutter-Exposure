using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShutterExposure.Player;
using ShutterExposure.CameraSystem;
using ShutterExposure.Interaction;

namespace ShutterExposure.UI
{
    /// <summary>
    /// Gestiona la interfaz de usuario del jugador durante la partida (HUD).
    /// Muestra las 3 vidas, la barra de estamina, el ritmo cardíaco (BPM),
    /// las baterías recolectadas y la barra de progreso de tensión al interactuar.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Referencias del Jugador")]
        [SerializeField] private CardiacSystem cardiacSystem;
        [SerializeField] private StaminaSystem staminaSystem;
        [SerializeField] private HandheldCamera handheldCamera;
        [SerializeField] private PlayerInteraction playerInteraction;

        [Header("Elementos de Vidas")]
        [SerializeField] private TextMeshProUGUI livesText; // Muestra "♥ ♥ ♥"
        [SerializeField] private Color fullLifeColor = new Color(0.95f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color lostLifeColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);

        [Header("Elementos de Estamina")]
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image staminaFillImage;

        [Header("Ritmo Cardíaco (BPM)")]
        [SerializeField] private TextMeshProUGUI bpmText;
        [SerializeField] private Transform bpmPulseIcon;

        [Header("Baterías")]
        [SerializeField] private TextMeshProUGUI batteryText;

        [Header("Interacción por Tensión")]
        [SerializeField] private GameObject interactionPromptGroup;
        [SerializeField] private TextMeshProUGUI promptMessageText;
        [SerializeField] private Slider tensionProgressSlider;
        [SerializeField] private Image crosshairImage;

        private float _pulseTimer;

        private void Start()
        {
            FindPlayerReferences();
            SubscribeEvents();
            UpdateInitialVisuals();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void FindPlayerReferences()
        {
            if (cardiacSystem == null) cardiacSystem = FindAnyObjectByType<CardiacSystem>();
            if (staminaSystem == null) staminaSystem = FindAnyObjectByType<StaminaSystem>();
            if (handheldCamera == null) handheldCamera = FindAnyObjectByType<HandheldCamera>();
            if (playerInteraction == null) playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        }

        private void SubscribeEvents()
        {
            if (cardiacSystem != null)
            {
                cardiacSystem.OnLivesChanged += HandleLivesChanged;
                cardiacSystem.OnBPMChanged += HandleBPMChanged;
            }

            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChanged += HandleStaminaChanged;
            }

            if (handheldCamera != null)
            {
                handheldCamera.OnBatteryCountChanged += HandleBatteryCountChanged;
            }

            if (playerInteraction != null)
            {
                playerInteraction.OnInteractableHovered += HandleInteractableHovered;
                playerInteraction.OnInteractableUnhovered += HandleInteractableUnhovered;
                playerInteraction.OnHoldProgressChanged += HandleHoldProgressChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (cardiacSystem != null)
            {
                cardiacSystem.OnLivesChanged -= HandleLivesChanged;
                cardiacSystem.OnBPMChanged -= HandleBPMChanged;
            }

            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChanged -= HandleStaminaChanged;
            }

            if (handheldCamera != null)
            {
                handheldCamera.OnBatteryCountChanged -= HandleBatteryCountChanged;
            }

            if (playerInteraction != null)
            {
                playerInteraction.OnInteractableHovered -= HandleInteractableHovered;
                playerInteraction.OnInteractableUnhovered -= HandleInteractableUnhovered;
                playerInteraction.OnHoldProgressChanged -= HandleHoldProgressChanged;
            }
        }

        private void UpdateInitialVisuals()
        {
            if (cardiacSystem != null)
            {
                HandleLivesChanged(cardiacSystem.CurrentLives, cardiacSystem.MaxLives);
                HandleBPMChanged(cardiacSystem.CurrentBPM);
            }

            if (staminaSystem != null)
            {
                HandleStaminaChanged(staminaSystem.CurrentStamina, staminaSystem.MaxStamina);
            }

            if (handheldCamera != null)
            {
                HandleBatteryCountChanged(handheldCamera.CurrentBatteries, handheldCamera.MaxBatteries);
            }

            if (interactionPromptGroup != null)
            {
                interactionPromptGroup.SetActive(false);
            }
        }

        private void Update()
        {
            // Efecto visual de pulsación de corazón derivado de los BPM
            if (bpmPulseIcon != null && cardiacSystem != null)
            {
                float beatsPerSecond = Mathf.Max(cardiacSystem.CurrentBPM, 40f) / 60f;
                _pulseTimer += Time.deltaTime * beatsPerSecond * Mathf.PI * 2f;
                float scale = 1f + (Mathf.Sin(_pulseTimer) * 0.15f);
                bpmPulseIcon.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void HandleLivesChanged(int current, int max)
        {
            if (livesText == null) return;

            string hearts = "";
            for (int i = 0; i < max; i++)
            {
                if (i < current)
                {
                    hearts += "<color=#E82525>♥</color> ";
                }
                else
                {
                    hearts += "<color=#404040>♥</color> ";
                }
            }
            livesText.text = $"Vidas: {hearts.TrimEnd()}";
        }

        private void HandleBPMChanged(float currentBPM)
        {
            if (bpmText == null) return;

            string colorTag = currentBPM > 140f ? "<color=#FF2020>" : (currentBPM > 105f ? "<color=#FF9520>" : "<color=#E0E0E0>");
            bpmText.text = $"♥ {colorTag}{Mathf.RoundToInt(currentBPM)} BPM</color>";
        }

        private void HandleStaminaChanged(float current, float max)
        {
            if (staminaSlider != null && max > 0)
            {
                staminaSlider.value = current / max;
            }
        }

        private void HandleBatteryCountChanged(int current, int max)
        {
            if (batteryText == null) return;

            string batteries = "";
            for (int i = 0; i < max; i++)
            {
                batteries += i < current ? "<color=#30FF60>■</color> " : "<color=#404040>□</color> ";
            }
            batteryText.text = $"Pilas: {batteries}({current}/{max})";
        }

        private void HandleInteractableHovered(string prompt)
        {
            if (interactionPromptGroup != null) interactionPromptGroup.SetActive(true);
            if (promptMessageText != null) promptMessageText.text = prompt;
            if (tensionProgressSlider != null) tensionProgressSlider.value = 0f;
            if (crosshairImage != null) crosshairImage.color = Color.yellow;
        }

        private void HandleInteractableUnhovered()
        {
            if (interactionPromptGroup != null) interactionPromptGroup.SetActive(false);
            if (crosshairImage != null) crosshairImage.color = Color.white;
        }

        private void HandleHoldProgressChanged(float progress01)
        {
            if (tensionProgressSlider != null)
            {
                tensionProgressSlider.value = progress01;
            }
        }
    }
}
