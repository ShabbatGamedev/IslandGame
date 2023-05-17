using Dialogues;
using I2.Loc;
using Input;
using UnityEngine;

namespace Interactions.InteractableObjects {
    public class TestBox : MonoBehaviour, IInteractable {
        PlayerInput _input;
        HorseFuckDialogue _dialogue;
        readonly LocalizedString _localizedHintText = "HintText";

        string _buttonDisplayName => _input.Player.Interaction.controls[0].name.ToUpper();
        
        // ReSharper disable once ConvertToAutoProperty
        public string HintText => $"{_localizedHintText}".Replace("BUTTON_NAME", _buttonDisplayName);
        
        public void Interact(IInteractor interactor) => _dialogue.StartDialogue();

        void Awake() => _input = InputsSingleton.PlayerInput;
        void Start() => _dialogue = DialogueGlobals.GetDialogue<HorseFuckDialogue>();
    }
}