using UnityEngine;

public class WormSpin : MonoBehaviour {
    [SerializeField] float minSpeed = 10,
        maxSpeed = 15;

    float _directionAngle;

    float _speed;

    void Awake() {
        do {
            _speed = Random.Range(minSpeed, maxSpeed + 1);
            _directionAngle = Random.Range(-1, 2);
        } while (_speed == 0 || _directionAngle == 0);
    }

    void Update() => transform.Rotate(0, _directionAngle * _speed * Time.deltaTime, 0);
}