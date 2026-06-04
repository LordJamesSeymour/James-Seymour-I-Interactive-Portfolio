using UnityEngine;

namespace Portfolio.Cameras
{
    [System.Obsolete("Use PlayerCameraController with Cinemachine. This bridge only preserves older serialized references.")]
    [DisallowMultipleComponent]
    public sealed class PortfolioCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            PlayerCameraController cameraController = GetComponent<PlayerCameraController>();
            if (cameraController != null && target != null)
            {
                cameraController.Target = target;
            }
        }
    }
}
