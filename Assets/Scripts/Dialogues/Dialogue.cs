﻿using DialogueGraph.Runtime;
using TMPro;
using UnityEngine;

namespace Dialogues {
    public abstract class Dialogue : MonoBehaviour {
        public GameObject OtherUI { get; set; }
        public GameObject DialogueContainer { get; set; }
        public GameObject ChoicesContainer { get; set; }
        public GameObject SpeakSeparator { get; set; }
        public GameObject ChoicesSeparator { get; set; }
        public TextMeshProUGUI SpeakerName { get; set; }
        public TextMeshProUGUI SpeakerLine { get; set; }

        public PlayerInput Input { get; set; }
        public RuntimeDialogueGraph DialogueLogic { get; set; }
        public ChoicesController ChoicesController { get; set; }
        public bool DialogueActive { get; set; }

        public void Enabled() {
            OtherUI.SetActive(false);
            Input.Player.Disable();
            Input.DialogueReading.Enable();
        }

        public void Disabled() {
            Input.DialogueReading.Disable();
            Input.Player.Enable();
            OtherUI.SetActive(true);
        }

        public virtual void StartDialogue() {
            if (DialogueActive) return;

            gameObject.SetActive(true);
            DialogueLogic.ResetConversation();
            DialogueActive = true;
        }

        public virtual void PlayerSelect(int index) => ChoicesController.Deactivate();
    }
}