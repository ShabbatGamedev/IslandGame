using DialogueGraph.Runtime;
using TMPro;
using UnityEngine;

namespace Dialogues {
    public interface IDialogue {
        public GameObject DialogueContainer { set; }
        public GameObject ChoicesContainer { set; }
        public GameObject SpeakSeparator { set; }
        public GameObject ChoicesSeparator { set; }
        public TextMeshProUGUI SpeakerName { set; }
        public TextMeshProUGUI SpeakerLine { set; }

        public PlayerInput Input { set; }
        public RuntimeDialogueGraph DialogueLogic { set; }
        public ChoicesController ChoicesController { set; }
        public bool DialogueActive { get; }

        public void StartDialogue();

        public void PlayerSelect(int index);
    }
}