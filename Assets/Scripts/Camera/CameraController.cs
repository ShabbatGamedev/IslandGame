using Input;
using UnityEngine;

namespace Camera {
    public class CameraController : MonoBehaviour {
        public float sensitivity = 450;
        public Transform player;
        PlayerInput _input;

        float _xRotation;

        void Awake() {
            _input = InputsSingleton.PlayerInput;

            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update() {
            Vector2 axes = _input.Player.Camera.ReadValue<Vector2>() * (sensitivity * Time.deltaTime);

            _xRotation -= axes.y;
            _xRotation = Mathf.Clamp(_xRotation, -75, 80);

            transform.localRotation = Quaternion.Euler(_xRotation, 0, 0);
            player.Rotate(Vector3.up * axes.x);
        }

        void OnEnable() => _input.Enable();

        void OnDisable() => _input.Disable();
    }
}