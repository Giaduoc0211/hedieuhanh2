using Cinemachine;
using MalbersAnimations.Scriptables;
using UnityEngine;

namespace MalbersAnimations
{
    [AddComponentMenu("Malbers/Camera/Third Person Follow Zoom (Cinemachine)")]
    public class ThirdPersonFollowZoom : MonoBehaviour
    {
        [Tooltip("Update mode for the Aim Logic")]
        public UpdateType updateMode = UpdateType.FixedUpdate;
      
        [Tooltip("Zoom In Min Value")]
        public FloatReference ZoomMin = new(1);

        [Tooltip("Zoom Out Max Value")]
        public FloatReference ZoomMax = new(12);

        [Tooltip("Zoom step changes")]
        public FloatReference ZoomStep = new(1);

        [Tooltip("Zoom smooth value to change between steps")]
        public FloatReference ZoomLerp = new(5);

        private float TargetZoom;
        private Cinemachine3rdPersonFollow TPF;

        private void OnEnable()
        {
            TPF = this.FindComponent<Cinemachine3rdPersonFollow>();

            if (TPF != null)
                TargetZoom = TPF.CameraDistance;
        }


        public void ZoomIn()
        {
            if (TPF != null)
                TargetZoom = Mathf.Clamp(TargetZoom - ZoomStep, ZoomMin, ZoomMax);
        }

        public void ZoomOut()
        {
            if (TPF != null)
                TargetZoom = Mathf.Clamp(TargetZoom + ZoomStep, ZoomMin, ZoomMax);
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateType.FixedUpdate)
            {
                Zoom(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateType.LateUpdate)
            {
                Zoom(Time.deltaTime);
            }
        }

        private void Zoom(float deltaTime)
        {
            if (TPF)
                TPF.CameraDistance = Mathf.Lerp(TPF.CameraDistance, TargetZoom, ZoomLerp * deltaTime);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            var scroll = gameObject.AddComponent<MMouseScroll>();


            UnityEditor.Events.UnityEventTools.AddPersistentListener(scroll.OnScrollDown, ZoomOut);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(scroll.OnScrollUp, ZoomIn);

        }
#endif
    }
}
