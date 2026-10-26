#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShutterExposure.Core;
using ShutterExposure.Player;
using ShutterExposure.CameraSystem;
using ShutterExposure.Interaction;
using ShutterExposure.Audio;
using ShutterExposure.UI;
using UnityEngine.InputSystem.UI;

namespace ShutterExposure.EditorScripts
{
    /// <summary>
    /// Herramienta de Editor para generar la sala de pruebas de Greybox completa con un solo clic.
    /// Crea el entorno 3D, jugador en 1ra persona con pisadas audibles, cámara con flash,
    /// objetos interactivos (puerta y batería pulsante), HUD completo (vidas, estamina, BPM, baterías, tensión)
    /// y menú de pausa funcional (tecla Escape).
    /// </summary>
    public static class GreyboxSceneSetupEditor
    {
        [MenuItem("Shutter Exposure/Configurar Sala Greybox de Prueba", false, 1)]
        public static void SetupGreyboxRoom()
        {
            // 1. Limpieza exhaustiva de todas las copias previas en la escena
            var allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var obj in allSceneObjects)
            {
                if (obj == null) continue;
                if (obj.name.Contains("ENTORNO GREYBOX") || 
                    obj.name.Contains("Player_Patient") || 
                    obj.name.Contains("UI CANVAS") ||
                    obj.name.Contains("EventSystem") ||
                    obj.name.Contains("Door_Interactable") ||
                    obj.name.Contains("Battery_Pickup"))
                {
                    Object.DestroyImmediate(obj);
                }
            }

            // 2. Contenedores principales
            GameObject envRoot = new GameObject("--- ENTORNO GREYBOX ---");
            Undo.RegisterCreatedObjectUndo(envRoot, "Create Greybox Env");

            // 3. Mediadores Globales (NoiseManager y MicManager)
            GameObject managersObj = new GameObject("Managers");
            managersObj.transform.parent = envRoot.transform;
            managersObj.AddComponent<NoiseManager>();
            managersObj.AddComponent<MicrophoneInputManager>();

            // 4. Suelo y Muros
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor (16x16m)";
            floor.transform.parent = envRoot.transform;
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(16f, 1f, 16f);

            CreateWall("Wall_North", new Vector3(0f, 2f, 8f), new Vector3(16f, 4f, 0.5f), envRoot.transform);
            CreateWall("Wall_South", new Vector3(0f, 2f, -8f), new Vector3(16f, 4f, 0.5f), envRoot.transform);
            CreateWall("Wall_West", new Vector3(-8f, 2f, 0f), new Vector3(0.5f, 4f, 16f), envRoot.transform);
            CreateWall("Wall_East", new Vector3(8f, 2f, 0f), new Vector3(0.5f, 4f, 16f), envRoot.transform);

            // 5. Puerta Interactiva (Rúbrica: Rotación)
            GameObject doorFrame = new GameObject("Door_Interactable_Hinge");
            doorFrame.transform.parent = envRoot.transform;
            doorFrame.transform.position = new Vector3(0f, 0f, 3f);

            GameObject doorMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorMesh.name = "Door_Mesh";
            doorMesh.transform.parent = doorFrame.transform;
            doorMesh.transform.localPosition = new Vector3(0.55f, 1.25f, 0f); // Pivotar desde la bisagra
            doorMesh.transform.localScale = new Vector3(1.1f, 2.5f, 0.1f);

            var doorType = System.Type.GetType("ShutterExposure.Interaction.DoorInteractable, Assembly-CSharp");
            if (doorType != null)
            {
                var doorComp = doorMesh.AddComponent(doorType);
                SerializedObject doorSO = new SerializedObject(doorComp);
                var hingeProp = doorSO.FindProperty("doorHinge");
                if (hingeProp != null) hingeProp.objectReferenceValue = doorFrame.transform;
                doorSO.ApplyModifiedProperties();
            }

            // 6. Batería Interactiva con Escalación (Rúbrica: Escalación)
            GameObject batteryObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            batteryObj.name = "Battery_Pickup_Scalable";
            batteryObj.transform.parent = envRoot.transform;
            batteryObj.transform.position = new Vector3(-2.5f, 0.4f, 2.5f);
            batteryObj.transform.localScale = new Vector3(0.2f, 0.35f, 0.2f);
            batteryObj.AddComponent<BatteryPickup>();

            // 7. Objetos Físicos para Empujar
            for (int i = 0; i < 4; i++)
            {
                GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prop.name = $"PhysicsProp_ChairOrBox_{i + 1}";
                prop.transform.parent = envRoot.transform;
                prop.transform.position = new Vector3(2.5f + (i * 0.8f), 0.5f, 1f + (i * 0.5f));
                prop.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                var rb = prop.AddComponent<Rigidbody>();
                rb.mass = 8f;
            }

            // 8. Iluminación Ambiental
            GameObject lightObj = new GameObject("Asylum_AmbientLight");
            lightObj.transform.parent = envRoot.transform;
            var lightComp = lightObj.AddComponent<Light>();
            lightComp.type = LightType.Point;
            lightComp.range = 16f;
            lightComp.intensity = 0.55f;
            lightComp.color = new Color(0.85f, 0.9f, 0.95f);
            lightObj.transform.position = new Vector3(0f, 3.5f, 0f);

            // 9. Jugador Completo en 1ra Persona
            GameObject playerRoot = new GameObject("Player_Patient");
            playerRoot.transform.position = new Vector3(0f, 1f, -4f);
            Undo.RegisterCreatedObjectUndo(playerRoot, "Create Player");

            var charCtrl = playerRoot.AddComponent<CharacterController>();
            charCtrl.height = 1.85f;
            charCtrl.radius = 0.38f;
            charCtrl.center = new Vector3(0f, 0.925f, 0f);

            var stamina = playerRoot.AddComponent<StaminaSystem>();
            var cardiac = playerRoot.AddComponent<CardiacSystem>();
            var movement = playerRoot.AddComponent<PlayerMovement>();
            var look = playerRoot.AddComponent<PlayerLook>();
            var interaction = playerRoot.AddComponent<PlayerInteraction>();
            var footsteps = playerRoot.AddComponent<FootstepAudioPlayer>();

            // Contenedor de Cabeza / Cámara
            GameObject cameraHolder = new GameObject("CameraHolder");
            cameraHolder.transform.parent = playerRoot.transform;
            cameraHolder.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCam.tag = "MainCamera";
            mainCam.transform.parent = cameraHolder.transform;
            mainCam.transform.localPosition = Vector3.zero;
            mainCam.transform.localRotation = Quaternion.identity;

            // Flash y Cámara en Mano
            GameObject camMeshHolder = new GameObject("HandheldCamera_Holder");
            camMeshHolder.transform.parent = mainCam.transform;
            camMeshHolder.transform.localPosition = new Vector3(0.24f, -0.22f, 0.42f);

            GameObject cameraVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cameraVisual.name = "Camera_Greybox_Mesh";
            cameraVisual.transform.parent = camMeshHolder.transform;
            cameraVisual.transform.localPosition = Vector3.zero;
            cameraVisual.transform.localScale = new Vector3(0.18f, 0.12f, 0.14f);
            Object.DestroyImmediate(cameraVisual.GetComponent<Collider>());

            GameObject flashLightObj = new GameObject("Camera_FlashLight");
            flashLightObj.transform.parent = camMeshHolder.transform;
            flashLightObj.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            var flashComp = flashLightObj.AddComponent<Light>();
            flashComp.type = LightType.Spot;
            flashComp.spotAngle = 65f;
            flashComp.range = 22f;
            flashComp.intensity = 28f;
            flashComp.enabled = false;

            var handheld = playerRoot.AddComponent<HandheldCamera>();

            // Enlazar referencias del Jugador
            SerializedObject moveSO = new SerializedObject(movement);
            moveSO.FindProperty("cameraHolder").objectReferenceValue = cameraHolder.transform;
            moveSO.ApplyModifiedProperties();

            SerializedObject lookSO = new SerializedObject(look);
            lookSO.FindProperty("playerBody").objectReferenceValue = playerRoot.transform;
            lookSO.FindProperty("cameraTransform").objectReferenceValue = mainCam.transform;
            lookSO.ApplyModifiedProperties();

            SerializedObject interactSO = new SerializedObject(interaction);
            interactSO.FindProperty("cameraTransform").objectReferenceValue = mainCam.transform;
            interactSO.ApplyModifiedProperties();

            SerializedObject handheldSO = new SerializedObject(handheld);
            handheldSO.FindProperty("flashLight").objectReferenceValue = flashComp;
            handheldSO.FindProperty("cameraMeshHolder").objectReferenceValue = camMeshHolder.transform;
            handheldSO.ApplyModifiedProperties();

            // 10. Construcción del Canvas UI (HUD + Menú de Pausa)
            SetupUI(playerRoot, cardiac, stamina, handheld, interaction);

            Selection.activeGameObject = playerRoot;
            Debug.Log("<color=green><b>[Shutter - Exposure]</b> ¡Sala Greybox, Jugador, HUD y Menú de Pausa creados exitosamente!</color>");
        }

        private static void SetupUI(GameObject player, CardiacSystem cardiac, StaminaSystem stamina, HandheldCamera handheld, PlayerInteraction interaction)
        {
            // EventSystem configurado con el nuevo Input System (InputSystemUIInputModule)
            var existingES = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingES == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var legacyModule = existingES.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (legacyModule != null) Object.DestroyImmediate(legacyModule);
                if (existingES.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existingES.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }

            // Canvas Principal
            GameObject canvasObj = new GameObject("--- UI CANVAS (HUD & PAUSA) ---");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // Retícula Central (Crosshair)
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvasObj.transform, false);
            var crossImg = crosshair.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.75f);
            var crossRT = crosshair.GetComponent<RectTransform>();
            crossRT.sizeDelta = new Vector2(6f, 6f);

            // Grupo Superior Izquierdo: Vidas y Ritmo Cardíaco
            GameObject topLeftGroup = new GameObject("TopLeft_StatsGroup");
            topLeftGroup.transform.SetParent(canvasObj.transform, false);
            var tlRT = topLeftGroup.AddComponent<RectTransform>();
            tlRT.anchorMin = new Vector2(0, 1);
            tlRT.anchorMax = new Vector2(0, 1);
            tlRT.pivot = new Vector2(0, 1);
            tlRT.anchoredPosition = new Vector2(35, -35);
            tlRT.sizeDelta = new Vector2(300, 100);

            // Texto de Vidas (♥ ♥ ♥)
            GameObject livesObj = new GameObject("Lives_Text");
            livesObj.transform.SetParent(topLeftGroup.transform, false);
            var livesTMP = livesObj.AddComponent<TextMeshProUGUI>();
            livesTMP.fontSize = 32;
            livesTMP.text = "Vidas: <color=#E82525>♥ ♥ ♥</color>";
            livesTMP.fontStyle = FontStyles.Bold;
            var livesRT = livesObj.GetComponent<RectTransform>();
            livesRT.anchorMin = new Vector2(0, 1);
            livesRT.anchorMax = new Vector2(0, 1);
            livesRT.pivot = new Vector2(0, 1);
            livesRT.anchoredPosition = Vector2.zero;
            livesRT.sizeDelta = new Vector2(300, 40);

            // Texto de Ritmo Cardíaco (BPM)
            GameObject bpmObj = new GameObject("BPM_Text");
            bpmObj.transform.SetParent(topLeftGroup.transform, false);
            var bpmTMP = bpmObj.AddComponent<TextMeshProUGUI>();
            bpmTMP.fontSize = 26;
            bpmTMP.text = "♥ 72 BPM";
            bpmTMP.color = new Color(0.9f, 0.9f, 0.9f);
            var bpmRT = bpmObj.GetComponent<RectTransform>();
            bpmRT.anchorMin = new Vector2(0, 1);
            bpmRT.anchorMax = new Vector2(0, 1);
            bpmRT.pivot = new Vector2(0, 1);
            bpmRT.anchoredPosition = new Vector2(0, -45);
            bpmRT.sizeDelta = new Vector2(300, 40);

            // Grupo Superior Derecho: Baterías
            GameObject topRightGroup = new GameObject("TopRight_BatteryGroup");
            topRightGroup.transform.SetParent(canvasObj.transform, false);
            var trRT = topRightGroup.AddComponent<RectTransform>();
            trRT.anchorMin = new Vector2(1, 1);
            trRT.anchorMax = new Vector2(1, 1);
            trRT.pivot = new Vector2(1, 1);
            trRT.anchoredPosition = new Vector2(-35, -35);
            trRT.sizeDelta = new Vector2(250, 50);

            GameObject batObj = new GameObject("Battery_Text");
            batObj.transform.SetParent(topRightGroup.transform, false);
            var batTMP = batObj.AddComponent<TextMeshProUGUI>();
            batTMP.fontSize = 28;
            batTMP.alignment = TextAlignmentOptions.TopRight;
            batTMP.text = "Pilas: <color=#30FF60>■</color> □ □ (1/3)";
            var batRT = batObj.GetComponent<RectTransform>();
            batRT.anchorMin = Vector2.zero;
            batRT.anchorMax = Vector2.one;
            batRT.sizeDelta = Vector2.zero;

            // Grupo Inferior Izquierdo: Barra de Estamina
            GameObject staminaGroup = new GameObject("Stamina_Group");
            staminaGroup.transform.SetParent(canvasObj.transform, false);
            var stamRT = staminaGroup.AddComponent<RectTransform>();
            stamRT.anchorMin = new Vector2(0, 0);
            stamRT.anchorMax = new Vector2(0, 0);
            stamRT.pivot = new Vector2(0, 0);
            stamRT.anchoredPosition = new Vector2(35, 35);
            stamRT.sizeDelta = new Vector2(240, 30);

            GameObject stamLabel = new GameObject("Stamina_Label");
            stamLabel.transform.SetParent(staminaGroup.transform, false);
            var stamLabelTMP = stamLabel.AddComponent<TextMeshProUGUI>();
            stamLabelTMP.fontSize = 16;
            stamLabelTMP.text = "ESTAMINA";
            stamLabelTMP.color = new Color(0.8f, 0.8f, 0.8f);
            var stamLabelRT = stamLabel.GetComponent<RectTransform>();
            stamLabelRT.anchorMin = new Vector2(0, 1);
            stamLabelRT.anchorMax = new Vector2(1, 1);
            stamLabelRT.pivot = new Vector2(0, 0);
            stamLabelRT.anchoredPosition = new Vector2(0, 2);
            stamLabelRT.sizeDelta = new Vector2(0, 20);

            GameObject sliderObj = new GameObject("Stamina_Slider");
            sliderObj.transform.SetParent(staminaGroup.transform, false);
            var slider = sliderObj.AddComponent<Slider>();
            var sliderRT = sliderObj.GetComponent<RectTransform>();
            sliderRT.anchorMin = Vector2.zero;
            sliderRT.anchorMax = Vector2.one;
            sliderRT.sizeDelta = Vector2.zero;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.75f);
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero;
            faRT.anchorMax = Vector2.one;
            faRT.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 0.35f, 0.9f);
            var fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;

            slider.targetGraphic = fillImg;
            slider.fillRect = fillRT;
            slider.value = 1f;

            // Grupo Central de Interacción por Tensión
            GameObject interactGroup = new GameObject("Interaction_Prompt_Group");
            interactGroup.transform.SetParent(canvasObj.transform, false);
            var intRT = interactGroup.AddComponent<RectTransform>();
            intRT.anchorMin = new Vector2(0.5f, 0.5f);
            intRT.anchorMax = new Vector2(0.5f, 0.5f);
            intRT.pivot = new Vector2(0.5f, 0.5f);
            intRT.anchoredPosition = new Vector2(0, -65);
            intRT.sizeDelta = new Vector2(360, 60);

            GameObject promptTextObj = new GameObject("Prompt_Text");
            promptTextObj.transform.SetParent(interactGroup.transform, false);
            var promptTMP = promptTextObj.AddComponent<TextMeshProUGUI>();
            promptTMP.fontSize = 18;
            promptTMP.alignment = TextAlignmentOptions.Center;
            promptTMP.text = "Mantén presionado E para interactuar";
            promptTMP.color = Color.yellow;
            var ptRT = promptTextObj.GetComponent<RectTransform>();
            ptRT.anchorMin = new Vector2(0, 0.5f);
            ptRT.anchorMax = new Vector2(1, 1);
            ptRT.sizeDelta = Vector2.zero;

            GameObject tensionSliderObj = new GameObject("Tension_Progress_Bar");
            tensionSliderObj.transform.SetParent(interactGroup.transform, false);
            var tensionSlider = tensionSliderObj.AddComponent<Slider>();
            var tsRT = tensionSliderObj.GetComponent<RectTransform>();
            tsRT.anchorMin = new Vector2(0.1f, 0);
            tsRT.anchorMax = new Vector2(0.9f, 0.35f);
            tsRT.sizeDelta = Vector2.zero;

            GameObject tbgObj = new GameObject("Tension_BG");
            tbgObj.transform.SetParent(tensionSliderObj.transform, false);
            var tbgImg = tbgObj.AddComponent<Image>();
            tbgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
            var tbgRT = tbgObj.GetComponent<RectTransform>();
            tbgRT.anchorMin = Vector2.zero;
            tbgRT.anchorMax = Vector2.one;
            tbgRT.sizeDelta = Vector2.zero;

            GameObject tfillArea = new GameObject("Tension_FillArea");
            tfillArea.transform.SetParent(tensionSliderObj.transform, false);
            var tfaRT = tfillArea.AddComponent<RectTransform>();
            tfaRT.anchorMin = Vector2.zero;
            tfaRT.anchorMax = Vector2.one;
            tfaRT.sizeDelta = Vector2.zero;

            GameObject tfillObj = new GameObject("Tension_Fill");
            tfillObj.transform.SetParent(tfillArea.transform, false);
            var tfillImg = tfillObj.AddComponent<Image>();
            tfillImg.color = new Color(1f, 0.85f, 0.15f, 0.95f);
            var tfillRT = tfillObj.GetComponent<RectTransform>();
            tfillRT.anchorMin = Vector2.zero;
            tfillRT.anchorMax = Vector2.one;
            tfillRT.sizeDelta = Vector2.zero;

            tensionSlider.targetGraphic = tfillImg;
            tensionSlider.fillRect = tfillRT;
            tensionSlider.value = 0f;

            // Conectar PlayerHUD
            var hud = canvasObj.AddComponent<PlayerHUD>();
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("cardiacSystem").objectReferenceValue = cardiac;
            hudSO.FindProperty("staminaSystem").objectReferenceValue = stamina;
            hudSO.FindProperty("handheldCamera").objectReferenceValue = handheld;
            hudSO.FindProperty("playerInteraction").objectReferenceValue = interaction;
            hudSO.FindProperty("livesText").objectReferenceValue = livesTMP;
            hudSO.FindProperty("staminaSlider").objectReferenceValue = slider;
            hudSO.FindProperty("bpmText").objectReferenceValue = bpmTMP;
            hudSO.FindProperty("bpmPulseIcon").objectReferenceValue = bpmTMP.transform;
            hudSO.FindProperty("batteryText").objectReferenceValue = batTMP;
            hudSO.FindProperty("interactionPromptGroup").objectReferenceValue = interactGroup;
            hudSO.FindProperty("promptMessageText").objectReferenceValue = promptTMP;
            hudSO.FindProperty("tensionProgressSlider").objectReferenceValue = tensionSlider;
            hudSO.FindProperty("crosshairImage").objectReferenceValue = crossImg;
            hudSO.ApplyModifiedProperties();

            // 11. Menú de Pausa
            GameObject pausePanel = new GameObject("Pause_Panel");
            pausePanel.transform.SetParent(canvasObj.transform, false);
            var pImg = pausePanel.AddComponent<Image>();
            pImg.color = new Color(0.04f, 0.04f, 0.06f, 0.92f);
            var pRT = pausePanel.GetComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero;
            pRT.anchorMax = Vector2.one;
            pRT.sizeDelta = Vector2.zero;

            GameObject pTitle = new GameObject("Pause_Title");
            pTitle.transform.SetParent(pausePanel.transform, false);
            var ptTMP = pTitle.AddComponent<TextMeshProUGUI>();
            ptTMP.fontSize = 54;
            ptTMP.alignment = TextAlignmentOptions.Center;
            ptTMP.text = "JUEGO PAUSADO";
            ptTMP.fontStyle = FontStyles.Bold;
            var ptTitleRT = pTitle.GetComponent<RectTransform>();
            ptTitleRT.anchorMin = new Vector2(0.5f, 0.7f);
            ptTitleRT.anchorMax = new Vector2(0.5f, 0.7f);
            ptTitleRT.sizeDelta = new Vector2(500, 80);

            var pauseMenu = canvasObj.AddComponent<PauseMenu>();
            SerializedObject pauseSO = new SerializedObject(pauseMenu);
            pauseSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            pauseSO.ApplyModifiedProperties();

            // Botones de Pausa
            CreateButton(pausePanel.transform, "Boton_Reanudar", "REANUDAR (ESC)", new Vector2(0, 10), () => pauseMenu.ResumeGame());
            CreateButton(pausePanel.transform, "Boton_Reiniciar", "REINICIAR SALA", new Vector2(0, -60), () => pauseMenu.RestartLevel());
            CreateButton(pausePanel.transform, "Boton_Salir", "SALIR DEL JUEGO", new Vector2(0, -130), () => pauseMenu.QuitGame());

            pausePanel.SetActive(false);
        }

        private static void CreateButton(Transform parent, string name, string text, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(action);

            var btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta = new Vector2(280, 50);
            btnRT.anchoredPosition = pos;

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(btnObj.transform, false);
            var tmp = tObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = text;
            tmp.color = Color.white;
            var tRT = tObj.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale, Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
        }
    }
}
#endif
