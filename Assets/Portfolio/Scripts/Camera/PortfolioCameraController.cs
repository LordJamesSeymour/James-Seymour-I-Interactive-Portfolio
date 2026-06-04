using UnityEngine;

namespace Portfolio.Cameras
{
    [DisallowMultipleComponent]
    public sealed class PortfolioCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -8f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0.01f)] private float followSmoothing = 8f;
        [SerializeField, Min(0.01f)] private float rotationSmoothing = 10f;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSmoothing * Time.deltaTime));

            Vector3 lookTarget = target.position + lookAtOffset;
            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime));
        }
    }
}
