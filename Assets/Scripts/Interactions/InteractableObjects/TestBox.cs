using Dialogues;

namespace Interactions.InteractableObjects {
    public class TestBox : Interactable {
        HorseFuckDialogue _dialogue;

        public override string HintText => $"[{InteractionKey}] Talk";

        void Start() => _dialogue = DialogueGlobals.GetDialogue<HorseFuckDialogue>();

        public override void Interact(Interactor interactor) => _dialogue.StartDialogue();
    }
}