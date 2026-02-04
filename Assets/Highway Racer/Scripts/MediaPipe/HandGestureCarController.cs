using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using System.Collections.Generic;

namespace HighwayRacer.MediaPipe
{
    public class HandGestureCarController : MonoBehaviour
    {
        [Header("Car Reference")]
        public RCC_CarControllerV3 targetCar;

        [Header("Auto Speed Settings")]
        [Tooltip("Constant throttle value (0-1). Car will always move at this speed.")]
        [Range(0f, 1f)]
        public float autoThrottle = 0.5f;
        
        [Tooltip("Enable/disable auto speed")]
        public bool useAutoSpeed = true;

        [Header("Steering Settings")]
        [Tooltip("How sensitive the steering is to hand movement")]
        [Range(0.5f, 5f)]
        public float steerSensitivity = 2.0f;
        
        [Tooltip("Dead zone in center where no steering occurs")]
        [Range(0f, 0.3f)]
        public float deadZone = 0.1f;
        
        [Tooltip("Smoothing for steering (higher = smoother but slower response)")]
        [Range(1f, 20f)]
        public float steerSmoothing = 8f;

        [Header("Debug")]
        public bool showDebugUI = true;
        public bool showLandmarks = true;

        // Internal state
        private float currentSteer = 0f;
        private float targetSteer = 0f;
        private bool isTracking = false;
        private Vector2 handPosition = new Vector2(0.5f, 0.5f);
        private int detectionCount = 0;
        
        // For drawing
        private UnityEngine.Rect drawRect = new UnityEngine.Rect(0, 0, 240, 180);
        private List<NormalizedLandmark> cachedLandmarks = null;

        private const int WRIST = 0;

        public void SetDrawRect(UnityEngine.Rect rect)
        {
            drawRect = rect;
        }

        public void OnHandLandmarksReceived(HandLandmarkerResult result)
        {
            detectionCount++;
            
            if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            {
                isTracking = false;
                cachedLandmarks = null;
                return;
            }

            isTracking = true;
            var landmarks = result.handLandmarks[0];
            
            if (landmarks.landmarks == null || landmarks.landmarks.Count < 21) 
            {
                cachedLandmarks = null;
                return;
            }

            // Cache for drawing
            cachedLandmarks = new List<NormalizedLandmark>(landmarks.landmarks);

            // Use wrist position for steering
            var wrist = landmarks.landmarks[WRIST];
            handPosition = new Vector2(wrist.x, wrist.y);

            // Calculate steering
            // In mirrored camera: moving left in real world = higher X value
            // We want: real hand left = car steer left (negative)
            // So we invert: steer = -(x - 0.5)
            float xOffset = -(handPosition.x - 0.5f);
            
            if (Mathf.Abs(xOffset) < deadZone)
            {
                targetSteer = 0f;
            }
            else
            {
                float sign = Mathf.Sign(xOffset);
                float adjustedOffset = (Mathf.Abs(xOffset) - deadZone) / (0.5f - deadZone);
                targetSteer = sign * adjustedOffset * steerSensitivity;
                targetSteer = Mathf.Clamp(targetSteer, -1f, 1f);
            }
        }

        void Update()
        {
            // Smooth steering
            currentSteer = Mathf.Lerp(currentSteer, targetSteer, Time.deltaTime * steerSmoothing);

            // Apply to car
            if (targetCar != null && targetCar.canControl)
            {
                if (!targetCar.externalController)
                    targetCar.externalController = true;

                targetCar.steerInput = currentSteer;
                
                if (useAutoSpeed)
                {
                    targetCar.throttleInput = autoThrottle;
                    targetCar.brakeInput = 0f;
                }
            }
        }

        void OnGUI()
        {
            if (showDebugUI)
            {
                // Debug Box - top left
                GUI.Box(new UnityEngine.Rect(10, 10, 220, 140), "Hand Control Debug");
                GUI.Label(new UnityEngine.Rect(20, 35, 200, 20), $"Tracking: {(isTracking ? "YES" : "NO")}");
                GUI.Label(new UnityEngine.Rect(20, 55, 200, 20), $"Hand X: {handPosition.x:F2}");
                GUI.Label(new UnityEngine.Rect(20, 75, 200, 20), $"Target Steer: {targetSteer:F2}");
                GUI.Label(new UnityEngine.Rect(20, 95, 200, 20), $"Current Steer: {currentSteer:F2}");
                GUI.Label(new UnityEngine.Rect(20, 115, 200, 20), $"Detections: {detectionCount}");
            }

            if (showLandmarks)
            {
                DrawHandLandmarks();
            }
        }

        private void DrawHandLandmarks()
        {
            if (!isTracking || cachedLandmarks == null || cachedLandmarks.Count < 21) return;

            // Hand skeleton connections
            int[][] connections = new int[][] {
                new int[]{0, 1, 2, 3, 4},      // Thumb
                new int[]{0, 5, 6, 7, 8},      // Index
                new int[]{0, 9, 10, 11, 12},   // Middle
                new int[]{0, 13, 14, 15, 16},  // Ring
                new int[]{0, 17, 18, 19, 20},  // Pinky
                new int[]{5, 9, 13, 17}        // Palm base
            };

            // Draw bones
            GUI.color = Color.cyan;
            foreach (var chain in connections)
            {
                for (int i = 0; i < chain.Length - 1; i++)
                {
                    int idx1 = chain[i];
                    int idx2 = chain[i + 1];
                    if (idx1 < cachedLandmarks.Count && idx2 < cachedLandmarks.Count)
                    {
                        DrawLine(cachedLandmarks[idx1], cachedLandmarks[idx2], 2f);
                    }
                }
            }

            // Draw joints
            GUI.color = Color.yellow;
            for (int i = 0; i < Mathf.Min(21, cachedLandmarks.Count); i++)
            {
                DrawPoint(cachedLandmarks[i], 5f);
            }
            
            // Highlight wrist
            GUI.color = Color.red;
            DrawPoint(cachedLandmarks[0], 8f);
            
            GUI.color = Color.white;
        }

        private void DrawLine(NormalizedLandmark start, NormalizedLandmark end, float width)
        {
            // Map normalized coords to draw rect
            // Flip X for mirrored display, Flip Y because image is upside down
            Vector2 s = new Vector2(
                drawRect.x + (1f - start.x) * drawRect.width,
                drawRect.y + (1f - start.y) * drawRect.height  // Flip Y
            );
            Vector2 e = new Vector2(
                drawRect.x + (1f - end.x) * drawRect.width,
                drawRect.y + (1f - end.y) * drawRect.height  // Flip Y
            );
            
            float length = (e - s).magnitude;
            if (length < 0.1f) return;
            
            float angle = Mathf.Atan2(e.y - s.y, e.x - s.x) * Mathf.Rad2Deg;
            
            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, s);
            GUI.DrawTexture(new UnityEngine.Rect(s.x, s.y - width/2, length, width), Texture2D.whiteTexture);
            GUI.matrix = matrixBackup;
        }

        private void DrawPoint(NormalizedLandmark point, float size)
        {
            float x = drawRect.x + (1f - point.x) * drawRect.width;  // Flip X
            float y = drawRect.y + (1f - point.y) * drawRect.height;  // Flip Y
            GUI.DrawTexture(new UnityEngine.Rect(x - size/2, y - size/2, size, size), Texture2D.whiteTexture);
        }

        public void SetTargetCar(RCC_CarControllerV3 car)
        {
            targetCar = car;
            if (targetCar != null)
                targetCar.externalController = true;
        }
    }
}
