using System.Collections.Generic;
using DialogueGraph.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dialogues {
    public class HorseFuckDialogue : MonoBehaviour, IDialogue {
        bool _playerChoosing;
        bool _shouldShowText;
        bool _showingText;
        string _textToShow;

        void Update() {
            if (_shouldShowText) {
                DialogueContainer.SetActive(true);
                SpeakerLine.gameObject.SetActive(true);
                SpeakerLine.text = _textToShow;
                _showingText = true;
                _shouldShowText = false;
            }

            if (_showingText || _playerChoosing) return;

            if (DialogueLogic.IsConversationDone()) {
                // Reset state
                _shouldShowText = false;
                _showingText = false;
                DialogueActive = false;
                
                DialogueContainer.SetActive(false);
                gameObject.SetActive(false);
                return;
            }

            if (DialogueLogic.IsCurrentNpc()) {
                ActorData currentActor = DialogueLogic.GetCurrentActor();
                _shouldShowText = true;
                _textToShow = DialogueLogic.ProgressNpc();
                SpeakerName.text = currentActor.Name;
            } else {
                _playerChoosing = true;
                List<ConversationLine> currentLines = DialogueLogic.GetCurrentLines();
                DialogueContainer.SetActive(true);
                ChoicesController.Activate();
                ChoicesController.Initialize(currentLines);
            }
        }

        void NextLine(InputAction.CallbackContext ctx) {
            if (!_showingText) return;

            _showingText = false;
            DialogueContainer.SetActive(false);
            SpeakerLine.gameObject.SetActive(false);
        }

        void OnEnable() {
            Input.Player.Disable();
            Input.DialogueReading.Enable();
            Input.DialogueReading.NextLine.performed += NextLine;
        }

        void OnDisable() {
            Input.DialogueReading.NextLine.performed -= NextLine;
            Input.DialogueReading.Disable();
            Input.Player.Enable();
        }

        public GameObject DialogueContainer { get; set; }
        public GameObject ChoicesContainer { get; set; }
        public GameObject SpeakSeparator { get; set; }
        public GameObject ChoicesSeparator { get; set; }
        public TextMeshProUGUI SpeakerName { get; set; }
        public TextMeshProUGUI SpeakerLine { get; set; }

        public PlayerInput Input { get; set; }
        public RuntimeDialogueGraph DialogueLogic { get; set; }
        public ChoicesController ChoicesController { get; set; }
        public bool DialogueActive { get; private set; }

        public void StartDialogue() {
            if (DialogueActive) return;

            gameObject.SetActive(true);
            DialogueLogic.ResetConversation();
            DialogueActive = true;
        }

        public void PlayerSelect(int index) {
            ChoicesController.Deactivate();
            _textToShow = DialogueLogic.ProgressSelf(index);
            _shouldShowText = true;
            _playerChoosing = false;
        }

        public void HorseFuck(string node, int lineIndex) => Debug.Log("вы успешно трахнули лошадь");
    }
}