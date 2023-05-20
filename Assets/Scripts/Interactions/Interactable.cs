using Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interactions {
    public abstract class Interactable : MonoBehaviour {
        public string InteractionKey => InteractionInput.controls[0].name.ToUpper();
        public virtual string HintText => $"[{InteractionKey}]";

        InputAction InteractionInput => InputsSingleton.PlayerInput.Player.Interaction;

        public abstract void Interact(Interactor interactor);
    }
}