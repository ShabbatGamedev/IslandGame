using Input;
using Interactions;
using Items;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player {
    public class PlayerController : Interactor {
        [SerializeField] TextMeshProUGUI hintTextBlock;
        [SerializeField] CharacterController controller;

        [SerializeField] float jumpHeight = 3;
        [SerializeField] float gravity = 9.81f;

        [SerializeField] float movementSpeed = 5;
        [SerializeField] float runningSpeed = 7;

        Interactable _currentInteractable;
        PlayerInput _input;
        Vector2 _movement;
        Vector3 _velocity;
        
        float _speed => (_input.Player.Run.IsPressed() ? runningSpeed : movementSpeed) * Time.deltaTime;

        void Awake() {
            _input = InputsSingleton.PlayerInput;
            
            _input.Player.Interaction.performed += InteractionPerformed;
            _input.Player.UseItem.performed += UseItemPerformed;
        }

        void Update() {
            Move();
            RaycastInteractions();
            //Sneak();
        }

        void OnEnable() => _input.Enable();

        void OnDisable() => _input.Disable();

        void Move() {
            Transform playerTransform = transform;
            bool canJump = _input.Player.Jump.IsPressed() && controller.isGrounded;

            Vector2 moveAxis = _input.Player.Movement.ReadValue<Vector2>();
            Vector3 movement = (playerTransform.right * moveAxis.x + playerTransform.forward * moveAxis.y) * _speed;

            if (canJump) _velocity.y = Mathf.Sqrt(jumpHeight * -2 * -gravity);
            else if (controller.isGrounded) _velocity.y = -2;
            else _velocity.y -= gravity * Time.deltaTime;

            controller.Move(movement);
            controller.Move(_velocity * Time.deltaTime);
        }

        void RaycastInteractions() {
            Ray ray = playerCamera.ScreenPointToRay(_screenCenter);

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
    }
}