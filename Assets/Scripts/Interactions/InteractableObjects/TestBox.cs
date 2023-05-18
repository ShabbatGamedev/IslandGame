using Dialogues;
using Input;
using UnityEngine;

namespace Interactions.InteractableObjects {
    public class TestBox : MonoBehaviour, IInteractable {
        PlayerInput _input;
        HorseFuckDialogue _dialogue;

        string _buttonDisplayName => _input.Player.Interaction.controls[0].name.ToUpper();
        
        public string HintText => $"Press {_buttonDisplayName} to interact";
        
        public void Interact(IInteractor interactor) => _dialogue.StartDialogue();

        void Awake() => _input = InputsSingleton.PlayerInput;
        void Start() => _dialogue = DialogueGlobals.GetDialogue<HorseFuckDialogue>();
    }
}