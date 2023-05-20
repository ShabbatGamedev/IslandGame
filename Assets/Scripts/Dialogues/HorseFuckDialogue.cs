using System.Collections.Generic;
using DialogueGraph.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dialogues {
    public class HorseFuckDialogue : Dialogue {
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

        void OnEnable() {
            Enabled();
            Input.DialogueReading.NextLine.performed += NextLine;
        }

        void OnDisable() {
            Input.DialogueReading.NextLine.performed -= NextLine;
            Disabled();
        }

        void NextLine(InputAction.CallbackContext ctx) {
            if (!_showingText) return;

            _showingText = false;
            DialogueContainer.SetActive(false);
            SpeakerLine.gameObject.SetActive(false);
        }

        public override void PlayerSelect(int index) {
            base.PlayerSelect(index);
            _textToShow = DialogueLogic.ProgressSelf(index);
            _shouldShowText = true;
            _playerChoosing = false;
        }

        public void HorseFuck(string node, int lineIndex) => Debug.Log("вы успешно трахнули лошадь");
    }
}