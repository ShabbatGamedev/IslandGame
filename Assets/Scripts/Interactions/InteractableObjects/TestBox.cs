using Dialogues;
using I2.Loc;
using Input;
using UnityEngine;

namespace Interactions.InteractableObjects {
    public class TestBox : MonoBehaviour, IInteractable {
        readonly LocalizedString _localizedHintText = "HintText";
        
        // ReSharper disable once ConvertToAutoProperty
        public string HintText => $"{_localizedHintText}".Replace("BUTTON_NAME", _buttonDisplayName);

        PlayerInput _input;
        string _buttonDisplayName => _input.Player.Interaction.controls[0].name.ToUpper();

        [SerializeField] HorseFuckDialogue dialogue;
        
        public void Interact(IInteractor interactor) => dialogue.StartDialogue();

        void Awake() => _input = InputsSingleton.PlayerInput;
    }
}