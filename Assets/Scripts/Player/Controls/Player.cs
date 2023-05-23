using Input;
using Interactions;
using Items;
using KinematicCharacterController.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Controls {
    public class Player : Interactor {
        [Header("Controllers")]
        [SerializeField] CharacterController character;
        [SerializeField] CameraController characterCamera;

        [Header("HUD")]
        [SerializeField] TextMeshProUGUI hintTextBlock;
        
        Interactable _currentInteractable;
        PlayerInput _input;

        void Awake() {
            Camera = characterCamera.mainCamera;
            _input = InputsSingleton.PlayerInput;

            _input.Player.Interaction.performed += InteractionPerformed;
            _input.Player.UseItem.performed += UseItemPerformed;
        }

        void Start() {
            Cursor.lockState = CursorLockMode.Locked;

            characterCamera.SetFollowTransform(character.cameraFollowPoint);

            characterCamera.ignoredColliders.Clear();
            characterCamera.ignoredColliders.AddRange(character.GetComponentsInChildren<Collider>());
        }
        
        void Update() {
            HandleCharacterInput();
            RaycastInteractions();
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
                _cameraRotation = characterCamera.Transform.rotation,
                _moveAxes = _input.Player.Movement.ReadValue<Vector2>(),
                _jump = _input.Player.Jump.WasPressedThisFrame(),
                _crouching = _input.Player.Sneak.IsPressed()
            };

            // Apply inputs to character
            character.SetInputs(ref characterInputs);
        }

        void RaycastInteractions() {
            Ray ray = characterCamera.mainCamera.ScreenPointToRay(_screenCenter);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance) ||
                !hit.transform.TryGetComponent(out _currentInteractable)) {
                if (hintTextBlock.text != "") 
                    hintTextBlock.SetText("");
                
                return;
            }

            hintTextBlock.SetText(_currentInteractable.HintText);
        }

        void InteractionPerformed(InputAction.CallbackContext ctx) {
            if (_currentInteractable != null) _currentInteractable.Interact(this);
        }

        void UseItemPerformed(InputAction.CallbackContext ctx) {
            ItemObject item = inventory.SelectedSlot.GetItem();
            if (item != null) item.Use(this);
        }

        void OnEnable() => _input.Enable();

        void OnDisable() => _input.Disable();
    }
}