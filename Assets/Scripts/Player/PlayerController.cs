using Input;
using Interactions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player {
    public class PlayerController : Interactor {
        public TextMeshProUGUI speedInfo;
        public TextMeshProUGUI hintText;

        public CharacterController controller;
        public UnityEngine.Camera playerCamera;

        public float jumpHeight = 3;
        public float sneakHeight = 0.25f;

        public float gravity = 9.81f;

        public float movementSpeed = 5;
        public float runningSpeed = 7;
        public float sneakSpeed = 3;

        public float maxInteractionDistance = 5;
        Interactable _currentInteractable;
        PlayerInput _input;
        bool _isSneaking;
        Vector2 _movement;
        Vector3 _normalScale;
        Vector3 _position;
        Vector3 _velocity;

        static float _deltaTime => Time.deltaTime;
        float _speed => (_isSneaking ? sneakSpeed : _isRunning ? runningSpeed : movementSpeed) * _deltaTime;

        static Vector2 _screenCenter => new Vector2(Screen.width, Screen.height) / 2;
        bool _isRunning => _input.Player.Run.IsPressed();

        void Awake() {
            _normalScale = transform.localScale;

            _input = InputsSingleton.PlayerInput;
            _input.Player.Interaction.performed += InteractionPerformed;
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
            else _velocity.y -= gravity * _deltaTime;

            controller.Move(movement);
            speedInfo.SetText($"Speed: {controller.velocity.magnitude:0.#}");

            controller.Move(_velocity * _deltaTime);
        }

        //todo: fix this shit
        /*void Sneak() {
            bool canSneak = _input.Player.Sneak.IsPressed() && controller.isGrounded;

            switch (canSneak) {
                case true when !_isSneaking:
                    _isSneaking = true;
                    playerTransform.localScale = new Vector3(_normalScale.x, _normalScale.y - sneakHeight, _normalScale.z);
                    break;

                case false when _isSneaking:
                    _isSneaking = false;
                    playerTransform.localScale = _normalScale;
                    break;
            }
        }*/

        void RaycastInteractions() {
            Ray ray = playerCamera.ScreenPointToRay(_screenCenter);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance) ||
                !hit.transform.TryGetComponent(out _currentInteractable)) {
                hintText.SetText("");
                return;
            }

            hintText.SetText(_currentInteractable.HintText);
        }

        void InteractionPerformed(InputAction.CallbackContext ctx) {
            if (_currentInteractable != null) _currentInteractable.Interact(this);
        }
    }
}