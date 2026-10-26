#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ShutterExposure.Core;
using ShutterExposure.Player;
using ShutterExposure.CameraSystem;
using ShutterExposure.Interaction;
using ShutterExposure.Audio;

namespace ShutterExposure.EditorScripts
{
    /// <summary>
    /// Herramienta de Editor para generar la sala de pruebas de Greybox con un solo clic.
    /// Crea el jugador con todos sus componentes, la cámara en mano, la linterna/flash,
    /// objetos interactuables (puerta, batería con escalación) y objetos con físicas para empujar.
    /// </summary>
    public static class GreyboxSceneSetupEditor
    {
        [MenuItem("Shutter Exposure/Configurar Sala Greybox de Prueba", false, 10)]
        public static void SetupGreyboxRoom()
        {
            // 1. Contenedores principales
            GameObject envRoot = new GameObject("--- ENTORNO GREYBOX ---");
            GameObject playerRoot = null;

            // 2. Mediadores Globales (NoiseManager y MicManager)
            GameObject managersObj = new GameObject("Managers");
            managersObj.AddComponent<NoiseManager>();
            managersObj.AddComponent<MicrophoneInputManager>();

            // 3. Suelo y Muros
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor (16x16m)";
            floor.transform.parent = envRoot.transform;
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(16f, 1f, 16f);

            // Muros perimetrales
            CreateWall("Wall_North", new Vector3(0f, 2f, 8f), new Vector3(16f, 4f, 0.5f), envRoot.transform);
            CreateWall("Wall_South", new Vector3(0f, 2f, -8f), new Vector3(16f, 4f, 0.5f), envRoot.transform);
            CreateWall("Wall_West", new Vector3(-8f, 2f, 0f), new Vector3(0.5f, 4f, 16f), envRoot.transform);
            CreateWall("Wall_East", new Vector3(8f, 2f, 0f), new Vector3(0.5f, 4f, 16f), envRoot.transform);

            // 4. Puerta Interactiva (Rúbrica: Rotación)
            GameObject doorFrame = new GameObject("Door_Interactable_Hinge");
            doorFrame.transform.parent = envRoot.transform;
            doorFrame.transform.position = new Vector3(0f, 0f, 3f);

            GameObject doorMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorMesh.name = "Door_Mesh";
            doorMesh.transform.parent = doorFrame.transform;
            doorMesh.transform.localPosition = new Vector3(0.55f, 1.25f, 0f); // Desplazado para pivotar desde la bisagra
            doorMesh.transform.localScale = new Vector3(1.1f, 2.5f, 0.1f);

            var doorScript = doorFrame.AddComponent<DoorInteractable>();
            Undo.RegisterCreatedObjectUndo(doorFrame, "Create Door");

            // 5. Batería Interactiva con Escalación (Rúbrica: Escalación)
            GameObject batteryObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            batteryObj.name = "Battery_Pickup_Scalable";
            batteryObj.transform.parent = envRoot.transform;
            batteryObj.transform.position = new Vector3(-2.5f, 0.4f, 2.5f);
            batteryObj.transform.localScale = new Vector3(0.2f, 0.35f, 0.2f);
            batteryObj.AddComponent<BatteryPickup>();

            // 6. Objetos Físicos para Empujar y Emitir Ruido
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

            // 7. Iluminación Ambiental Tenue
            GameObject lightObj = new GameObject("Asylum_AmbientLight");
            lightObj.transform.parent = envRoot.transform;
            var lightComp = lightObj.AddComponent<Light>();
            lightComp.type = LightType.Point;
            lightComp.range = 14f;
            lightComp.intensity = 0.45f;
            lightComp.color = new Color(0.85f, 0.9f, 0.95f);
            lightObj.transform.position = new Vector3(0f, 3.5f, 0f);

            // 8. Jugador Completo en 1ra Persona
            playerRoot = new GameObject("Player_Patient");
            playerRoot.transform.position = new Vector3(0f, 1f, -4f);

            var charCtrl = playerRoot.AddComponent<CharacterController>();
            charCtrl.height = 1.85f;
            charCtrl.radius = 0.38f;
            charCtrl.center = new Vector3(0f, 0.925f, 0f);

            var stamina = playerRoot.AddComponent<StaminaSystem>();
            var cardiac = playerRoot.AddComponent<CardiacSystem>();
            var movement = playerRoot.AddComponent<PlayerMovement>();
            var look = playerRoot.AddComponent<PlayerLook>();
            var interaction = playerRoot.AddComponent<PlayerInteraction>();

            // Contenedor de Cabeza / Cámara
            GameObject cameraHolder = new GameObject("CameraHolder");
            cameraHolder.transform.parent = playerRoot.transform;
            cameraHolder.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            // Cámara Principal
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
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
            // Destruir el collider para que la cámara no colisione con el propio jugador
            Object.DestroyImmediate(cameraVisual.GetComponent<Collider>());

            // Luz de Flash (Spotlight URP/Standard)
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

            // Enlazar referencias serializadas vía SerializedObject
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

            Selection.activeGameObject = playerRoot;
            Debug.Log("<color=green><b>[Shutter - Exposure]</b> ¡Sala de Greybox y Jugador configurados exitosamente con 1 clic!</color>");
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
