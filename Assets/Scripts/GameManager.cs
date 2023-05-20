using UnityEngine;

/// <summary>
/// Managing the game.
/// </summary>
public class GameManager : MonoBehaviour {
    void Awake() => DontDestroyOnLoad(this); // This object and its children wont be destroyed on scene changing
}