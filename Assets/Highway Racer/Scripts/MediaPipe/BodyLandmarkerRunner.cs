using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace HighwayRacer.MediaPipe
{
    public class BodyLandmarkerRunner : MonoBehaviour
    {
        [Header("Controller")]
        public BodyGestureCarController carController;

        [Header("Configuration")]
        [SerializeField] private string _modelPath = "pose_landmarker_full.bytes";
        [SerializeField] private int _numPoses = 1;
        [SerializeField] private float _minPoseDetectionConfidence = 0.5f;
        [SerializeField] private float _minPosePresenceConfidence = 0.5f;
        [SerializeField] private float _minTrackingConfidence = 0.5f;
        [SerializeField] private int _cameraWidth = 640;
        [SerializeField] private int _cameraHeight = 480;
        [SerializeField] private int _cameraFPS = 30;

        [Header("UI")]
        [SerializeField] private RawImage _screenDisplay;

        private PoseLandmarker _poseLandmarker;
        private WebCamTexture _webCamTexture;
        private Texture2D _inputTexture;
        private bool _isRunning = false;
        private Color32[] _pixelBuffer;

        private void Start()
        {
            CreateScreen();
            
            // Pass Rect to Controller (OnGUI coordinates)
            // Bottom Right 320x240
            float w = 320; 
            float h = 240;
            float x = UnityEngine.Screen.width - w - 10;
            float y = UnityEngine.Screen.height - h - 10;
            
            if(carController != null) 
                carController.SetDrawRect(new UnityEngine.Rect(x, y, w, h));

            StartCoroutine(Run());
        }

        private void CreateScreen()
        {
            if (_screenDisplay != null) return;
            
            var canvasObj = new GameObject("MediaPipeScreenCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            var rawImageObj = new GameObject("CameraFeed");
            rawImageObj.transform.SetParent(canvasObj.transform, false);
            _screenDisplay = rawImageObj.AddComponent<RawImage>();
            
            // Bottom Right positioning
            var rect = _screenDisplay.rectTransform;
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-10, 10);
            rect.sizeDelta = new Vector2(320, 240); 
            
            // Mirror
            rect.localEulerAngles = new Vector3(0, 180, 0);
        }

        private void OnDestroy()
        {
            _isRunning = false;
            if (_poseLandmarker != null)
            {
                _poseLandmarker.Close();
                _poseLandmarker = null;
            }
            if (_webCamTexture != null && _webCamTexture.isPlaying)
            {
                _webCamTexture.Stop();
            }
        }

        private IEnumerator Run()
        {
            // Start WebCam
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("No camera found!");
                yield break;
            }

            _webCamTexture = new WebCamTexture(devices[0].name, _cameraWidth, _cameraHeight, _cameraFPS);
            _webCamTexture.Play();

            // Wait for camera to start
            int maxWait = 100;
            while (!_webCamTexture.didUpdateThisFrame && maxWait > 0)
            {
                yield return null;
                maxWait--;
            }

            if (!_webCamTexture.isPlaying)
            {
                Debug.LogError("Failed to start camera!");
                yield break;
            }

            Debug.Log($"Camera started: {_webCamTexture.width}x{_webCamTexture.height}");

            // Display camera
            if (_screenDisplay != null)
            {
                _screenDisplay.texture = _webCamTexture;
                _screenDisplay.color = UnityEngine.Color.white;
            }

            // Prepare input texture
            _inputTexture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
            _pixelBuffer = new Color32[_webCamTexture.width * _webCamTexture.height];

            // Load model
            string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, _modelPath);
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, need to copy from StreamingAssets
            string destPath = System.IO.Path.Combine(Application.persistentDataPath, _modelPath);
            if (!System.IO.File.Exists(destPath))
            {
                var www = new WWW(modelPath);
                yield return www;
                System.IO.File.WriteAllBytes(destPath, www.bytes);
            }
            modelPath = destPath;
            #endif
            
            // Check if model exists
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (!System.IO.File.Exists(modelPath))
            {
                Debug.LogError($"Model not found at: {modelPath}");
                yield break;
            }
            #endif

            var options = new PoseLandmarkerOptions(
                new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetPath: modelPath
                ),
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE,
                numPoses: _numPoses,
                minPoseDetectionConfidence: _minPoseDetectionConfidence,
                minPosePresenceConfidence: _minPosePresenceConfidence,
                minTrackingConfidence: _minTrackingConfidence
            );

            _poseLandmarker = PoseLandmarker.CreateFromOptions(options);
            Debug.Log("PoseLandmarker loaded!");

            _isRunning = true;

            // Main loop - process frames
            while (_isRunning)
            {
                if (_webCamTexture.didUpdateThisFrame)
                {
                    ProcessFrame();
                }
                yield return null;
            }
        }

        private void ProcessFrame()
        {
            if (_poseLandmarker == null || !_webCamTexture.isPlaying) return;

            // Get pixels from webcam
            _webCamTexture.GetPixels32(_pixelBuffer);
            _inputTexture.SetPixels32(_pixelBuffer);
            _inputTexture.Apply();

            // Create MediaPipe Image
            var image = new Mediapipe.Image(
                ImageFormat.Types.Format.Srgba,
                _inputTexture.width,
                _inputTexture.height,
                _inputTexture.width * 4,
                _inputTexture.GetRawTextureData<byte>()
            );

            // Detect
            var result = _poseLandmarker.Detect(image);

            // Send to controller
            if (carController != null && result.poseLandmarks != null)
            {
                carController.OnPoseLandmarksReceived(result);
            }
        }
    }
}
