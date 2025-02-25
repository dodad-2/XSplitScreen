using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoDad.XSplitScreen.Components
{
    // To get this working Decalicious must not add any components to the cameras
    class MultiMonitorCameraManager : MonoBehaviour
    {
        #region Variables
        public static MultiMonitorCameraManager instance { get; private set; }

        public static List<string> validScenesByName = new List<string>()
        {
            "title",
            "singleplayer"
        };

        private static List<Camera> cameras = new List<Camera>();

        private static Camera mainCamera;
        private static Hook onWillRenderObject;
        public static RenderTexture renderTexture { get; private set; }

        public static bool isActive { get; private set; } = true;
        #endregion

        #region Unity Methods
        void Awake()
        {
            if (instance != null)
                Destroy(this);

            //DontDestroyOnLoad(gameObject);

            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            //EnableHooks(); // DISABLED BEFORE FINAL BUILD
        }

        void Update()
        {
            if (!isActive && renderTexture != null)
                DestroyCameras();

            if (Run.instance)
                return;

            bool updated = false;

            foreach (var localUser in LocalUserManager.localUsersList)
            {
                LocalSplitscreenUser user = localUser as LocalSplitscreenUser;

                if (user == null || user.assignment == null)
                    continue;

                if (user.assignment.display > -1 && user.assignment.display < Display.displays.Length && !Display.displays[user.assignment.display].active)
                {
                    Display.displays[user.assignment.display].Activate();
                    updated = true;
                }
            }

            if (updated)
                StartRenderingToDisplays();

            if (isActive && mainCamera == null)
                if (CameraRigController.instancesList.Count > 0)
                    mainCamera = CameraRigController.instancesList[0].sceneCam;
        }
        #endregion

        #region Event Handlers
        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            isActive = false;
            DestroyCameras();

            foreach (var name in validScenesByName)
                if (name == newScene.name)
                    isActive = true;
        }
        #endregion

        #region Multi Monitor Cameras
        public void StartRenderingToDisplays()
        {
            if (mainCamera == null)
                return;

            CreateCameras();
        }
        private void CreateCameras()
        {
            if (renderTexture != null)
                DestroyCameras();

            renderTexture = RenderTexture.GetTemporary(Display.displays[0].systemWidth, Display.displays[0].systemHeight, 24);
            renderTexture.Create();

            mainCamera.targetTexture = renderTexture;
			//Log.LogOutput($"renderTexture.IsCreated() = {renderTexture.IsCreated()}"); // 4.0.0 rewrite 9-12-24

			for (int e = 0; e < Display.displays.Length; e++)
            {
                if (!Display.displays[e].active)
                    continue;

                cameras.Add(new GameObject($"Multi-Monitor Camera {e}", typeof(Camera), typeof(MultiMonitorCamera)).GetComponent<Camera>());
                var newCameraObject = cameras[cameras.Count - 1];
                newCameraObject.transform.SetParent(mainCamera.transform);

                var camera = cameras[cameras.Count - 1];
                camera.targetDisplay = e;

				//Log.LogOutput($"Created camera for display {e}"); // 4.0.0 rewrite 9-12-24
			}
		}
        private void DestroyCameras()
        {
            foreach (var camera in cameras)
                GameObject.Destroy(camera.gameObject);

            cameras.Clear();
            RenderTexture.ReleaseTemporary(renderTexture);
            renderTexture = null;
        }
        #endregion

        #region Hooks
        private void EnableHooks()
        {
            var method1 = typeof(ThreeEyedGames.Decal).GetMethod("OnWillRenderObject", BindingFlags.NonPublic | BindingFlags.Instance);
            var method2 = typeof(MultiMonitorCameraManager).GetMethod("Decal_OnWillRenderObject", BindingFlags.NonPublic | BindingFlags.Static);

			//Log.LogOutput($"Method1 is null? {method1 == null}"); // 4.0.0 rewrite 9-12-24
			//Log.LogOutput($"Method2 is null? {method2 == null}"); // 4.0.0 rewrite 9-12-24

			onWillRenderObject = (new Hook(method1,
                method2));

            onWillRenderObject.Apply();

			//Log.LogOutput($"onWillRenderObject.IsValid ? {onWillRenderObject.IsValid}"); // 4.0.0 rewrite 9-12-24
		}
		/// <summary>
		/// Don't add DecaliciousRenderers to multi monitor cameras
		/// </summary>
		/// <param name="orig"></param>
		/// <param name="self"></param>
		private static void Decal_OnWillRenderObject(Hooks.Decal.orig_OnWillRenderObject orig, ThreeEyedGames.Decal self)
        {
			/*if (!MultiMonitorCameraManager.isActive) // 4.0.0 rewrite 9-12-24
                Log.LogOutput($"Current camera: {Camera.current?.name}");*/

			if (Camera.current == null)
                return;

            if (MultiMonitorCameraManager.isActive)
                return;

            orig(self);
        }
        #endregion
    }
}
