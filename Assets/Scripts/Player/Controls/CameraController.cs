using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player.Controls {
    public class CameraController : MonoBehaviour {
        const int MaxObstructions = 32;

        [Header("Framing")]
        public Camera mainCamera;
        public Vector2 followPointFraming = new(0, 0);
        public float followingSharpness = 10000;

        [Header("Distance")] 
        public float defaultDistance = 6;
        public float minDistance;
        public float maxDistance = 10;
        public float distanceMovementSpeed = 5;
        public float distanceMovementSharpness = 10;

        [Header("Rotation")] 
        public bool invertX;
        public bool invertY;

        [Range(-90, 90)] public float defaultVerticalAngle = 20;
        [Range(-90, 90)] public float minVerticalAngle = -90;
        [Range(-90, 90)] public float maxVerticalAngle = 90;

        public float rotationSpeed = 1;
        public float rotationSharpness = 10000;
        public bool rotateWithPhysicsMover;

        [Header("Obstruction")] 
        public float obstructionCheckRadius = 0.2f;
        public float obstructionSharpness = 10000;
        public LayerMask obstructionLayers = -1;
        public List<Collider> ignoredColliders = new();

        float _currentDistance;
        Vector3 _currentFollowPosition;
        readonly RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];

        RaycastHit _obstructionHit;
        bool _distanceIsObstructed;
        int _obstructionCount;
        float _obstructionTime;
        float _targetVerticalAngle;

        public Transform Transform { get; private set; }
        public Transform FollowTransform { get; private set; }

        public Vector3 PlanarDirection { get; set; }
        public float TargetDistance { get; set; }

        void Awake() {
            Transform = transform;

            _currentDistance = defaultDistance;
            TargetDistance = _currentDistance;

            _targetVerticalAngle = 0;

            PlanarDirection = Vector3.forward;
        }
        
        void OnValidate() {
            defaultDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
            defaultVerticalAngle = Mathf.Clamp(defaultVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }

        // Set the transform that the camera will orbit around
        public void SetFollowTransform(Transform t) {
            FollowTransform = t;
            PlanarDirection = FollowTransform.forward;
            _currentFollowPosition = FollowTransform.position;
        }

        public void UpdateWithInput(float deltaTime, Vector3 rotationInput) {
            if (!FollowTransform) return;

            if (invertX) rotationInput.x *= -1;
            if (invertY) rotationInput.y *= -1;

            // Process rotation input
            Vector3 up = FollowTransform.up;
            Quaternion rotationFromInput = Quaternion.Euler(up * (rotationInput.x * rotationSpeed));
            PlanarDirection = rotationFromInput * PlanarDirection;
            PlanarDirection = Vector3.Cross(up, Vector3.Cross(PlanarDirection, up));
            Quaternion planarRot = Quaternion.LookRotation(PlanarDirection, up);

            _targetVerticalAngle -= rotationInput.y * rotationSpeed;
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, minVerticalAngle, maxVerticalAngle);
            Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
            Quaternion targetRotation = Quaternion.Slerp(Transform.rotation, planarRot * verticalRot, 1 - Mathf.Exp(-rotationSharpness * deltaTime));

            // Apply rotation
            Transform.rotation = targetRotation;

            // Process distance input
            if (_distanceIsObstructed) TargetDistance = _currentDistance;

            TargetDistance += distanceMovementSpeed;
            TargetDistance = Mathf.Clamp(TargetDistance, minDistance, maxDistance);

            // Find the smoothed follow position
            _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position, 1 - Mathf.Exp(-followingSharpness * deltaTime));

            // Handle obstructions
            RaycastHit closestHit = new() { distance = Mathf.Infinity };

            _obstructionCount = Physics.SphereCastNonAlloc(_currentFollowPosition,
                obstructionCheckRadius,
                -Transform.forward,
                _obstructions,
                TargetDistance,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < _obstructionCount; i++) {
                bool isIgnored = ignoredColliders.Any(t => t == _obstructions[i].collider);

                if (!isIgnored && _obstructions[i].distance < closestHit.distance && _obstructions[i].distance > 0) {
                    closestHit = _obstructions[i];
                }
            }

            // If obstructions detector
            if (closestHit.distance < Mathf.Infinity) {
                _distanceIsObstructed = true;
                _currentDistance = Mathf.Lerp(_currentDistance, closestHit.distance, 1 - Mathf.Exp(-obstructionSharpness * deltaTime));
            } else { // If no obstruction
                _distanceIsObstructed = false;
                _currentDistance = Mathf.Lerp(_currentDistance, TargetDistance, 1 - Mathf.Exp(-distanceMovementSharpness * deltaTime));
            }

            // Find the smoothed camera orbit position
            Vector3 targetPosition = _currentFollowPosition - targetRotation * Vector3.forward * _currentDistance;

            // Handle framing
            targetPosition += Transform.right * followPointFraming.x;
            targetPosition += Transform.up * followPointFraming.y;

            // Apply position
            Transform.position = targetPosition;
        }
    }
}