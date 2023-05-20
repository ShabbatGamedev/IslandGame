using UnityEngine;

public class Sun : MonoBehaviour {
    public float speed = 3f;

    void Update() => transform.Rotate(5 * speed * Time.deltaTime, 0, 0);
}