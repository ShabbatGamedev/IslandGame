using DialogueGraph.Runtime;
using TMPro;
using UnityEngine;

namespace Dialogues {
    public interface IDialogue {
        public GameObject DialogueContainer { get; }
        public GameObject ChoicesContainer { get; }
        public GameObject SpeakSeparator { get; }
        public GameObject ChoicesSeparator { get; }
        public TextMeshProUGUI SpeakerName { get; }
        public TextMeshProUGUI SpeakerLine { get; }

        public RuntimeDialogueGraph DialogueLogic { get; }
        public IChoicesController ChoicesController { get; }
        public bool DialogueActive { get; }

        public void StartDialogue();
        public void PlayerSelect(int index);
    }
}