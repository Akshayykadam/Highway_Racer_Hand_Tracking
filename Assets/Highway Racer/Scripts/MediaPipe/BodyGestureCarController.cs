using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;

namespace HighwayRacer.MediaPipe
{
    public class BodyGestureCarController : MonoBehaviour
    {
        [Header("Car Reference")]
        public RCC_CarControllerV3 targetCar;

        [Header("Sensitivity")]
        public float steerSensitivity = 2.0f;
        public float leanThreshold = 0.05f; // Deadzone
        public float accelThreshold = 0.4f; // Higher means easier to accel? No, Y is inverted.
        
        // MediaPipe coords: 0,0 is Top-Left. 1,1 is Bottom-Right.
        // Center X = 0.5. Left < 0.5, Right > 0.5.
        // Center Y = 0.5. Up < 0.5, Down > 0.5.

        private float currentSteer = 0f;
        private float currentGas = 0f;
        private float currentBrake = 0f;

        // Landmarks
        private const int NOSE = 0;
        
        // Debug
        private bool isTracking = false;

        public void OnPoseLandmarksReceived(PoseLandmarkerResult result)
        {
            cachedResult = result; // Cache for OnGUI thread
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                isTracking = false;
                return;
            }

            isTracking = true;
            // Take first person
            var landmarks = result.poseLandmarks[0];

            if (landmarks.landmarks == null || landmarks.landmarks.Count < 33) return;

            // Use Nose for generic body position
            var nose = landmarks.landmarks[NOSE];

            ProcessMovement(nose);
        }

        private void ProcessMovement(NormalizedLandmark nose)
        {
            float x = nose.x;
            float y = nose.y;

            // --- Steering ---
            // Center is 0.5
            float steerRaw = (x - 0.5f) * steerSensitivity;
            // Inverted? If I move right (screen right), X increases.
            // If I move right in camera, I appear on right of screen (if not mirrored).
            // Usually selfie cam is mirrored. If I move Right, I go Left on screen.
            // Let's assume standard behavior:
            // X > 0.5 means Right side of image.
            
            // Deadzone
            if (Mathf.Abs(x - 0.5f) < leanThreshold) steerRaw = 0f;

            currentSteer = Mathf.Clamp(steerRaw, -1f, 1f);


            // --- Gas / Brake ---
            // Up (Y < 0.5) -> Gas
            // Down (Y > 0.5) -> Brake
            // Center -> Idle
            
            currentGas = 0f;
            currentBrake = 0f;

            float yDiff = 0.5f - y; // Positive if Up, Negative if Down
            
            if (yDiff > leanThreshold) // Moved Up
            {
                currentGas = 1f; // Full gas or proportional? 
                // Let's make it proportional or just full for simplicity as per "move up then it should accenlater"
                currentGas = Mathf.Clamp01(yDiff * 2f); // Sensitivity
            }
            else if (yDiff < -leanThreshold) // Moved Down
            {
                currentBrake = 1f;
                // Brake logic
                currentBrake = Mathf.Clamp01(Mathf.Abs(yDiff) * 2f);
            }

        }

        void Update()
        {
            if (targetCar != null && targetCar.canControl)
            {
                // Verify external control is enabled
                if (!targetCar.externalController)
                    targetCar.externalController = true;

                // Apply Inputs
                // Note: user said "move right then car should move right side again i move rightside right we have 4 lanes" 
                // This implies lane changing. But valid logic for RCC is steering.
                targetCar.steerInput = currentSteer;
                targetCar.throttleInput = currentGas;
                targetCar.brakeInput = currentBrake;
            }
        }

        void OnGUI()
        {
            // === DEBUG UI ===
            GUI.Box(new UnityEngine.Rect(10, 300, 200, 160), "Body Control Debug");
            GUI.Label(new UnityEngine.Rect(20, 320, 180, 20), $"Tracking: {isTracking}");
            GUI.Label(new UnityEngine.Rect(20, 340, 180, 20), $"Steer (X): {currentSteer:F2}");
            GUI.Label(new UnityEngine.Rect(20, 360, 180, 20), $"Gas/Brake (Y): {currentGas - currentBrake:F2}");
            
            // Visual Bar for Steer
            // val, size, min, max
            GUI.HorizontalScrollbar(new UnityEngine.Rect(20, 390, 180, 20), currentSteer, 0.2f, -1.0f, 1.0f);
            
            // Visual Bar for Gas/Brake
            GUI.Label(new UnityEngine.Rect(20, 415, 180, 20), "Brake <----> Accel");
            GUI.HorizontalScrollbar(new UnityEngine.Rect(20, 435, 180, 20), (currentGas - currentBrake), 0.2f, -1.0f, 1.0f);


            // === DRAW SKELETON ===
            DrawSkeleton();
        }

        private void DrawSkeleton()
        {
            if (!isTracking || cachedResult.poseLandmarks == null || cachedResult.poseLandmarks.Count == 0) return;

            var landmarks = cachedResult.poseLandmarks[0].landmarks;
            if (landmarks == null) return;

            // Scale to screen
            // Assuming Camera Feed fills the screen or we just draw over it.
            // MediaPipe 0,0 is Top-Left. Screen 0,0 is Top-Left (GUI).
            
            Color boneColor = Color.green;
            Color jointColor = Color.red;

            // Connections
            int[][] connections = new int[][] {
                new int[]{11, 12}, // Shoulders
                new int[]{11, 13, 15}, // Left Arm
                new int[]{12, 14, 16}, // Right Arm
                new int[]{11, 23}, // Left Body
                new int[]{12, 24}, // Right Body
                new int[]{23, 24}, // Hips
                new int[]{23, 25, 27}, // Left Leg
                new int[]{24, 26, 28}, // Right Leg
                new int[]{0, 1}, new int[]{1, 2}, new int[]{2, 3}, new int[]{3, 7}, // Left Eye/Ear
                new int[]{0, 4}, new int[]{4, 5}, new int[]{5, 6}, new int[]{6, 8}  // Right Eye/Ear
            };

            GUI.color = boneColor;
            foreach (var chain in connections)
            {
                for (int i = 0; i < chain.Length - 1; i++)
                {
                    DrawLine(landmarks[chain[i]], landmarks[chain[i + 1]], 3f);
                }
            }

            GUI.color = jointColor;
            for (int i = 0; i < 33; i++) // 33 Pose Landmarks
            {
                if(i >= landmarks.Count) break;
                DrawPoint(landmarks[i], 8f);
            }
            GUI.color = Color.white;
        }

        private UnityEngine.Rect drawRect = new UnityEngine.Rect(0, 0, Screen.width, Screen.height);

        public void SetDrawRect(UnityEngine.Rect rect)
        {
            drawRect = rect;
        }

        private void DrawLine(NormalizedLandmark start, NormalizedLandmark end, float width)
        {
            Vector2 s = new Vector2(drawRect.x + start.x * drawRect.width, drawRect.y + start.y * drawRect.height);
            Vector2 e = new Vector2(drawRect.x + end.x * drawRect.width, drawRect.y + end.y * drawRect.height);
            
            // Draw
            UnityEngine.Rect lineRect = new UnityEngine.Rect(s.x, s.y, (e - s).magnitude, width);
            float angle = Mathf.Atan2(e.y - s.y, e.x - s.x) * Mathf.Rad2Deg;
            
            GUIUtility.RotateAroundPivot(angle, s);
            GUI.DrawTexture(new UnityEngine.Rect(s.x, s.y - width/2, (e - s).magnitude, width), Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, s);
        }

        private void DrawPoint(NormalizedLandmark point, float size)
        {
            float x = drawRect.x + point.x * drawRect.width;
            float y = drawRect.y + point.y * drawRect.height;
            GUI.DrawTexture(new UnityEngine.Rect(x - size/2, y - size/2, size, size), Texture2D.whiteTexture);
        }
        
        public void SetTargetCar(RCC_CarControllerV3 car)
        {
            targetCar = car;
            targetCar.externalController = true;
        }

        private PoseLandmarkerResult cachedResult;


    }
}
