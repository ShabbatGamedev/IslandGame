using System;
using Input;
using KinematicCharacterController.Core;
using UnityEngine;

namespace Player.Controls {
    public class Player : MonoBehaviour {
        [SerializeField] CharacterController character;
        [SerializeField] CameraController characterCamera;

        PlayerInput _input;

        void Awake() => _input = InputsSingleton.PlayerInput;

        void Start() {
            Cursor.lockState = CursorLockMode.Locked;
                
            // Tell camera to follow transform
            characterCamera.SetFollowTransform(character.cameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            characterCamera.ignoredColliders.Clear();
            characterCamera.ignoredColliders.AddRange(character.GetComponentsInChildren<Collider>());
        }
        
        void Update() {
            HandleCharacterInput();
        }

        void LateUpdate() {
            // Handle rotating the camera along with physics movers
            if (characterCamera.rotateWithPhysicsMover && character.motor.AttachedRigidbody != null) {
                characterCamera.PlanarDirection = character.motor.AttachedRigidbody.GetComponent<PhysicsMover>()._rotationDeltaFromInterpolation * characterCamera.PlanarDirection;
                characterCamera.PlanarDirection = Vector3.ProjectOnPlane(characterCamera.PlanarDirection, character.motor.CharacterUp).normalized;
            }

            HandleCameraInput();
        }

        void HandleCameraInput() {
            // Create the look input vector for the camera
            Vector2 mouseLookAxes = _input.Player.Camera.ReadValue<Vector2>();
            Vector3 lookInputVector = new(mouseLookAxes.x, mouseLookAxes.y, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked) lookInputVector = Vector3.zero;

            // Apply inputs to the camera
            characterCamera.UpdateWithInput(Time.deltaTime, lookInputVector);
        }

        void HandleCharacterInput() {
            PlayerCharacterInputs characterInputs = new() {
                // Build the CharacterInputs struct
                _moveAxes = _input.Player.Movement.ReadValue<Vector2>(),
                _cameraRotation = characterCamera.Transform.rotation,
                _jumpDown = _input.Player.Jump.WasPressedThisFrame(),
                _crouchDown = _input.Player.Sneak.WasPressedThisFrame(),
                _crouchUp = _input.Player.Sneak.WasReleasedThisFrame()
            };

            // Apply inputs to character
            character.SetInputs(ref characterInputs);
        }
        
        void OnEnable() => _input.Enable();

        void OnDisable() => _input.Disable();
    }
}