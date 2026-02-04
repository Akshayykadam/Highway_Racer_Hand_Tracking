using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe;
using Mediapipe.Tasks.Vision.HandLandmarker;

namespace HighwayRacer.MediaPipe
{
    public class HandLandmarkerRunner : MonoBehaviour
    {
        [Header("Controller")]
        public HandGestureCarController carController;

        [Header("Camera Settings")]
        [SerializeField] private int _cameraWidth = 640;
        [SerializeField] private int _cameraHeight = 480;
        [SerializeField] private int _cameraFPS = 30;

        [Header("Model Settings")]
        [SerializeField] private string _modelPath = "hand_landmarker.bytes";
        [SerializeField] private int _numHands = 1;
        [SerializeField] private float _minHandDetectionConfidence = 0.5f;
        [SerializeField] private float _minHandPresenceConfidence = 0.5f;
        [SerializeField] private float _minTrackingConfidence = 0.5f;

        [Header("UI Settings")]
        [SerializeField] private float _previewWidth = 400f;
        [SerializeField] private float _previewHeight = 300f;
        [SerializeField] private float _previewMargin = 20f;

        private RawImage _screenDisplay;
        private Canvas _canvas;
        private HandLandmarker _handLandmarker;
        private WebCamTexture _webCamTexture;
        private Texture2D _inputTexture;
        private bool _isRunning = false;
        private Color32[] _pixelBuffer;

        private void Start()
        {
            CreateScreen();
            StartCoroutine(Run());
        }

        private void CreateScreen()
        {
            // Create canvas that follows game camera
            var canvasObj = new GameObject("HandTrackingCanvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000; // Very high to be on top
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create camera feed display
            var rawImageObj = new GameObject("CameraFeed");
            rawImageObj.transform.SetParent(canvasObj.transform, false);
            _screenDisplay = rawImageObj.AddComponent<RawImage>();
            
            // Position at bottom right with margin
            var rect = _screenDisplay.rectTransform;
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-_previewMargin, _previewMargin);
            rect.sizeDelta = new Vector2(_previewWidth, _previewHeight);
            
            // Add outline for visibility
            var outline = rawImageObj.AddComponent<Outline>();
            outline.effectColor = UnityEngine.Color.green;
            outline.effectDistance = new Vector2(2, 2);
        }

        private void Update()
        {
            // Update draw rect for controller based on actual screen position
            if (carController != null && _screenDisplay != null)
            {
                // Get screen rect of the RawImage
                Vector3[] corners = new Vector3[4];
                _screenDisplay.rectTransform.GetWorldCorners(corners);
                
                // Convert to screen coordinates
                Camera cam = Camera.main ?? Camera.current;
                if (cam != null)
                {
                    Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                    Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                    
                    // OnGUI uses top-left origin, Screen uses bottom-left
                    float x = min.x;
                    float y = UnityEngine.Screen.height - max.y;
                    float w = max.x - min.x;
                    float h = max.y - min.y;
                    
                    carController.SetDrawRect(new UnityEngine.Rect(x, y, w, h));
                }
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;
            if (_handLandmarker != null)
            {
                _handLandmarker.Close();
                _handLandmarker = null;
            }
            if (_webCamTexture != null && _webCamTexture.isPlaying)
            {
                _webCamTexture.Stop();
            }
        }

        private IEnumerator Run()
        {
            Debug.Log("[HandTracking] Starting...");
            
            // Start WebCam
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("[HandTracking] No camera found!");
                yield break;
            }

            Debug.Log($"[HandTracking] Found {devices.Length} cameras. Using: {devices[0].name}");
            _webCamTexture = new WebCamTexture(devices[0].name, _cameraWidth, _cameraHeight, _cameraFPS);
            _webCamTexture.Play();

            int maxWait = 100;
            while (!_webCamTexture.didUpdateThisFrame && maxWait > 0)
            {
                yield return null;
                maxWait--;
            }

            if (!_webCamTexture.isPlaying)
            {
                Debug.LogError("[HandTracking] Failed to start camera!");
                yield break;
            }

            Debug.Log($"[HandTracking] Camera started: {_webCamTexture.width}x{_webCamTexture.height}");

            if (_screenDisplay != null)
            {
                _screenDisplay.texture = _webCamTexture;
                _screenDisplay.color = UnityEngine.Color.white;
                
                // Flip horizontally for mirror effect
                _screenDisplay.uvRect = new UnityEngine.Rect(1, 0, -1, 1);
            }

            _inputTexture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
            _pixelBuffer = new Color32[_webCamTexture.width * _webCamTexture.height];

            // Load model
            string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, _modelPath);
            Debug.Log($"[HandTracking] Looking for model at: {modelPath}");
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            string destPath = System.IO.Path.Combine(Application.persistentDataPath, _modelPath);
            if (!System.IO.File.Exists(destPath))
            {
                Debug.Log($"[HandTracking] Copying model to: {destPath}");
                var www = new WWW(modelPath);
                yield return www;
                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError($"[HandTracking] Failed to load model: {www.error}");
                    yield break;
                }
                System.IO.File.WriteAllBytes(destPath, www.bytes);
            }
            modelPath = destPath;
            #endif
            
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (!System.IO.File.Exists(modelPath))
            {
                Debug.LogError($"[HandTracking] Model NOT found at: {modelPath}");
                Debug.LogError("[HandTracking] Please ensure hand_landmarker.bytes is in Assets/StreamingAssets/");
                yield break;
            }
            Debug.Log("[HandTracking] Model file found!");
            #endif

            HandLandmarkerOptions options = null;
            try
            {
                options = new HandLandmarkerOptions(
                    new Mediapipe.Tasks.Core.BaseOptions(
                        Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                        modelAssetPath: modelPath
                    ),
                    runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE,
                    numHands: _numHands,
                    minHandDetectionConfidence: _minHandDetectionConfidence,
                    minHandPresenceConfidence: _minHandPresenceConfidence,
                    minTrackingConfidence: _minTrackingConfidence
                );
                
                _handLandmarker = HandLandmarker.CreateFromOptions(options);
                Debug.Log("[HandTracking] HandLandmarker created successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HandTracking] Failed to create HandLandmarker: {e.Message}");
                yield break;
            }

            _isRunning = true;
            Debug.Log("[HandTracking] Starting detection loop...");

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
            if (_handLandmarker == null || !_webCamTexture.isPlaying) return;

            try
            {
                _webCamTexture.GetPixels32(_pixelBuffer);
                _inputTexture.SetPixels32(_pixelBuffer);
                _inputTexture.Apply();

                var image = new Mediapipe.Image(
                    ImageFormat.Types.Format.Srgba,
                    _inputTexture.width,
                    _inputTexture.height,
                    _inputTexture.width * 4,
                    _inputTexture.GetRawTextureData<byte>()
                );

                var result = _handLandmarker.Detect(image);

                if (carController != null)
                {
                    carController.OnHandLandmarksReceived(result);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HandTracking] Detection error: {e.Message}");
            }
        }
    }
}
