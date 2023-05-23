using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController.Core {
    /// <summary>
    /// The system that manages the simulation of KinematicCharacterMotor and PhysicsMover
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class KinematicCharacterSystem : MonoBehaviour {
        static KinematicCharacterSystem _instance;

        public static List<KinematicCharacterMotor> _characterMotors = new();
        public static List<PhysicsMover> _physicsMovers = new();

        static float _lastCustomInterpolationStartTime = -1f;
        static float _lastCustomInterpolationDeltaTime = -1f;

        public static KCCSettings _settings;

        void Awake() => _instance = this;

        void FixedUpdate() {
            if (!_settings.autoSimulation) return;

            float deltaTime = Time.deltaTime;

            if (_settings.interpolate) PreSimulationInterpolationUpdate(deltaTime);

            Simulate(deltaTime, _characterMotors, _physicsMovers);

            if (_settings.interpolate) PostSimulationInterpolationUpdate(deltaTime);
        }

        void LateUpdate() {
            if (_settings.interpolate) CustomInterpolationUpdate();
        }

        // This is to prevent duplicating the singleton game object on script recompiles
        void OnDisable() => Destroy(gameObject);

        /// <summary>
        /// Creates a KinematicCharacterSystem instance if there isn't already one
        /// </summary>
        public static void EnsureCreation() {
            if (_instance != null) return;

            GameObject systemGameObject = new("KinematicCharacterSystem");
            _instance = systemGameObject.AddComponent<KinematicCharacterSystem>();

            systemGameObject.hideFlags = HideFlags.NotEditable;
            _instance.hideFlags = HideFlags.NotEditable;

            _settings = ScriptableObject.CreateInstance<KCCSettings>();

            DontDestroyOnLoad(systemGameObject);
        }

        /// <summary>
        /// Gets the KinematicCharacterSystem instance if any
        /// </summary>
        public static KinematicCharacterSystem GetInstance() => _instance;

        /// <summary>
        /// Sets the maximum capacity of the character motors list, to prevent allocations when adding characters
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetCharacterMotorsCapacity(int capacity) {
            if (capacity < _characterMotors.Count) capacity = _characterMotors.Count;

            _characterMotors.Capacity = capacity;
        }

        /// <summary>
        /// Registers a KinematicCharacterMotor into the system
        /// </summary>
        public static void RegisterCharacterMotor(KinematicCharacterMotor motor) => _characterMotors.Add(motor);

        /// <summary>
        /// Unregisters a KinematicCharacterMotor from the system
        /// </summary>
        public static void UnregisterCharacterMotor(KinematicCharacterMotor motor) => _characterMotors.Remove(motor);

        /// <summary>
        /// Sets the maximum capacity of the physics movers list, to prevent allocations when adding movers
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetPhysicsMoversCapacity(int capacity) {
            if (capacity < _physicsMovers.Count) capacity = _physicsMovers.Count;

            _physicsMovers.Capacity = capacity;
        }

        /// <summary>
        /// Registers a PhysicsMover into the system
        /// </summary>
        public static void RegisterPhysicsMover(PhysicsMover mover) {
            _physicsMovers.Add(mover);

            mover.rigidBody.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>
        /// Unregisters a PhysicsMover from the system
        /// </summary>
        public static void UnregisterPhysicsMover(PhysicsMover mover) => _physicsMovers.Remove(mover);

        /// <summary>
        /// Remembers the point to interpolate from for KinematicCharacterMotors and PhysicsMovers
        /// </summary>
        public static void PreSimulationInterpolationUpdate(float deltaTime) {
            // Save pre-simulation poses and place transform at transient pose
            foreach (KinematicCharacterMotor motor in _characterMotors) {
                motor._initialTickPosition = motor.TransientPosition;
                motor._initialTickRotation = motor.TransientRotation;

                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }

            foreach (PhysicsMover mover in _physicsMovers) {
                mover.InitialTickPosition = mover.TransientPosition;
                mover.InitialTickRotation = mover.TransientRotation;

                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.rigidBody.position = mover.TransientPosition;
                mover.rigidBody.rotation = mover.TransientRotation;
            }
        }

        /// <summary>
        /// Ticks characters and/or movers
        /// </summary>
        public static void Simulate(float deltaTime, List<KinematicCharacterMotor> motors, List<PhysicsMover> movers) {
            int characterMotorsCount = motors.Count;
            int physicsMoversCount = movers.Count;

#pragma warning disable 0162
            // Update PhysicsMover velocities
            for (int i = 0; i < physicsMoversCount; i++) movers[i].VelocityUpdate(deltaTime);

            // Character controller update phase 1
            for (int i = 0; i < characterMotorsCount; i++) motors[i].UpdatePhase1(deltaTime);

            // Simulate PhysicsMover displacement
            for (int i = 0; i < physicsMoversCount; i++) {
                PhysicsMover mover = movers[i];

                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.rigidBody.position = mover.TransientPosition;
                mover.rigidBody.rotation = mover.TransientRotation;
            }

            // Character controller update phase 2 and move
            for (int i = 0; i < characterMotorsCount; i++) {
                KinematicCharacterMotor motor = motors[i];

                motor.UpdatePhase2(deltaTime);

                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }
#pragma warning restore 0162
        }

        /// <summary>
        /// Initiates the interpolation for KinematicCharacterMotors and PhysicsMovers
        /// </summary>
        public static void PostSimulationInterpolationUpdate(float deltaTime) {
            _lastCustomInterpolationStartTime = Time.time;
            _lastCustomInterpolationDeltaTime = deltaTime;

            // Return interpolated roots to their initial poses
            foreach (KinematicCharacterMotor motor in _characterMotors) 
                motor.Transform.SetPositionAndRotation(motor._initialTickPosition, motor._initialTickRotation);

            foreach (PhysicsMover mover in _physicsMovers) {
                if (mover.moveWithPhysics) {
                    mover.rigidBody.position = mover.InitialTickPosition;
                    mover.rigidBody.rotation = mover.InitialTickRotation;

                    mover.rigidBody.MovePosition(mover.TransientPosition);
                    mover.rigidBody.MoveRotation(mover.TransientRotation);
                } else {
                    mover.rigidBody.position = mover.TransientPosition;
                    mover.rigidBody.rotation = mover.TransientRotation;
                }
            }
        }

        /// <summary>
        /// Handles per-frame interpolation
        /// </summary>
        static void CustomInterpolationUpdate() {
            float interpolationFactor = Mathf.Clamp01((Time.time - _lastCustomInterpolationStartTime) / _lastCustomInterpolationDeltaTime);

            // Handle characters interpolation
            foreach (KinematicCharacterMotor motor in _characterMotors) {
                motor.Transform.SetPositionAndRotation(
                    Vector3.Lerp(motor._initialTickPosition, motor.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(motor._initialTickRotation, motor.TransientRotation, interpolationFactor));
            }

            // Handle PhysicsMovers interpolation
            foreach (PhysicsMover mover in _physicsMovers) {
                mover.Transform.SetPositionAndRotation(
                    Vector3.Lerp(mover.InitialTickPosition, mover.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(mover.InitialTickRotation, mover.TransientRotation, interpolationFactor));

                Vector3 newPos = mover.Transform.position;
                Quaternion newRot = mover.Transform.rotation;
                mover._positionDeltaFromInterpolation = newPos - mover._latestInterpolationPosition;
                mover._rotationDeltaFromInterpolation = Quaternion.Inverse(mover._latestInterpolationRotation) * newRot;
                mover._latestInterpolationPosition = newPos;
                mover._latestInterpolationRotation = newRot;
            }
        }
    }
}