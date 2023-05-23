using System;
using UnityEngine;

namespace KinematicCharacterController.Core {
    /// <summary>
    /// Represents the entire state of a PhysicsMover that is pertinent for simulation.
    /// Use this to save state or revert to past state
    /// </summary>
    [Serializable]
    public struct PhysicsMoverState {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }

    /// <summary>
    /// Component that manages the movement of moving kinematic rigidbodies for
    /// proper interaction with characters
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsMover : MonoBehaviour {
        /// <summary>
        /// The mover's Rigidbody
        /// </summary>
        [ReadOnly] public Rigidbody rigidBody;

        /// <summary>
        /// Determines if the platform moves with rigidbody.MovePosition (when true), or with rigidbody.position (when false)
        /// </summary>
        public bool moveWithPhysics = true;

        Vector3 _internalTransientPosition;
        Quaternion _internalTransientRotation;

        /// <summary>
        /// Remembers latest position in interpolation
        /// </summary>
        [NonSerialized] public Vector3 _latestInterpolationPosition;

        /// <summary>
        /// Remembers latest rotation in interpolation
        /// </summary>
        [NonSerialized] public Quaternion _latestInterpolationRotation;

        /// <summary>
        /// Index of this motor in KinematicCharacterSystem arrays
        /// </summary>
        [NonSerialized] public IMoverController _moverController;

        /// <summary>
        /// The latest movement made by interpolation
        /// </summary>
        [NonSerialized] public Vector3 _positionDeltaFromInterpolation;

        /// <summary>
        /// The latest rotation made by interpolation
        /// </summary>
        [NonSerialized] public Quaternion _rotationDeltaFromInterpolation;

        /// <summary>
        /// Index of this motor in KinematicCharacterSystem arrays
        /// </summary>
        public int IndexInCharacterSystem { get; set; }
        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        public Vector3 Velocity { get; protected set; }
        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        public Vector3 AngularVelocity { get; protected set; }
        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        public Vector3 InitialTickPosition { get; set; }
        /// <summary>
        /// Remembers initial rotation before all simulation are done
        /// </summary>
        public Quaternion InitialTickRotation { get; set; }

        /// <summary>
        /// The mover's Transform
        /// </summary>
        public Transform Transform { get; private set; }
        /// <summary>
        /// The character's position before the movement calculations began
        /// </summary>
        public Vector3 InitialSimulationPosition { get; private set; }
        /// <summary>
        /// The character's rotation before the movement calculations began
        /// </summary>
        public Quaternion InitialSimulationRotation { get; private set; }

        /// <summary>
        /// The mover's rotation (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 TransientPosition {
            get => _internalTransientPosition;
            private set => _internalTransientPosition = value;
        }

        /// <summary>
        /// The mover's rotation (always up-to-date during the character update phase)
        /// </summary>
        public Quaternion TransientRotation {
            get => _internalTransientRotation;
            private set => _internalTransientRotation = value;
        }

        void Awake() {
            Transform = transform;
            ValidateData();

            TransientPosition = rigidBody.position;
            TransientRotation = rigidBody.rotation;
            InitialSimulationPosition = rigidBody.position;
            InitialSimulationRotation = rigidBody.rotation;
            _latestInterpolationPosition = Transform.position;
            _latestInterpolationRotation = Transform.rotation;
        }


        void Reset() => ValidateData();

        void OnEnable() {
            KinematicCharacterSystem.EnsureCreation();
            KinematicCharacterSystem.RegisterPhysicsMover(this);
        }

        void OnDisable() => KinematicCharacterSystem.UnregisterPhysicsMover(this);

        void OnValidate() => ValidateData();

        /// <summary>
        /// Handle validating all required values
        /// </summary>
        public void ValidateData() {
            rigidBody = gameObject.GetComponent<Rigidbody>();

            rigidBody.centerOfMass = Vector3.zero;
            rigidBody.maxAngularVelocity = Mathf.Infinity;
            rigidBody.maxDepenetrationVelocity = Mathf.Infinity;
            rigidBody.isKinematic = true;
            rigidBody.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>
        /// Sets the mover's position directly
        /// </summary>
        public void SetPosition(Vector3 position) {
            Transform.position = position;
            rigidBody.position = position;
            InitialSimulationPosition = position;
            TransientPosition = position;
        }

        /// <summary>
        /// Sets the mover's rotation directly
        /// </summary>
        public void SetRotation(Quaternion rotation) {
            Transform.rotation = rotation;
            rigidBody.rotation = rotation;
            InitialSimulationRotation = rotation;
            TransientRotation = rotation;
        }

        /// <summary>
        /// Sets the mover's position and rotation directly
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) {
            Transform.SetPositionAndRotation(position, rotation);
            rigidBody.position = position;
            rigidBody.rotation = rotation;
            InitialSimulationPosition = position;
            InitialSimulationRotation = rotation;
            TransientPosition = position;
            TransientRotation = rotation;
        }

        /// <summary>
        /// Returns all the state information of the mover that is pertinent for simulation
        /// </summary>
        public PhysicsMoverState GetState() {
            PhysicsMoverState state = new() {
                position = TransientPosition,
                rotation = TransientRotation,
                velocity = Velocity,
                angularVelocity = AngularVelocity
            };

            return state;
        }

        /// <summary>
        /// Applies a mover state instantly
        /// </summary>
        public void ApplyState(PhysicsMoverState state) {
            SetPositionAndRotation(state.position, state.rotation);
            Velocity = state.velocity;
            AngularVelocity = state.angularVelocity;
        }

        /// <summary>
        /// Caches velocity values based on delta-time and target position/rotations
        /// </summary>
        public void VelocityUpdate(float deltaTime) {
            InitialSimulationPosition = TransientPosition;
            InitialSimulationRotation = TransientRotation;

            _moverController.UpdateMovement(out _internalTransientPosition, out _internalTransientRotation, deltaTime);

            if (deltaTime <= 0f) return;

            Velocity = (TransientPosition - InitialSimulationPosition) / deltaTime;

            Quaternion rotationFromCurrentToGoal = TransientRotation * Quaternion.Inverse(InitialSimulationRotation);
            AngularVelocity = Mathf.Deg2Rad * rotationFromCurrentToGoal.eulerAngles / deltaTime;
        }
    }
}