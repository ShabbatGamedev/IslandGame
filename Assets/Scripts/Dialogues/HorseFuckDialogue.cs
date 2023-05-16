using System.Collections.Generic;
using DialogueGraph.Runtime;
using Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dialogues {
    public class HorseFuckDialogue : MonoBehaviour, IDialogue {
        [SerializeField] GameObject dialogueContainer;
        [SerializeField] GameObject choicesContainer;
        [SerializeField] GameObject speakSeparator;
        [SerializeField] GameObject choicesSeparator;
        [SerializeField] TextMeshProUGUI speakerName;
        [SerializeField] TextMeshProUGUI speakerLine;

        [SerializeField] RuntimeDialogueGraph dialogueLogic;
        [SerializeField] ChoicesController choicesController;

        PlayerInput _input;

        bool _playerChoosing;
        bool _shouldShowText;
        bool _showingText;
        string _textToShow;

        void Awake() => _input = InputsSingleton.PlayerInput;

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
                speakerName.text = currentActor.Name;
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
            speakerLine.gameObject.SetActive(false);
        }

        void OnEnable() {
            _input.Player.Disable();
            _input.DialogueReading.Enable();
            _input.DialogueReading.NextLine.performed += NextLine;
        }

        void OnDisable() {
            _input.DialogueReading.NextLine.performed -= NextLine;
            _input.DialogueReading.Disable();
            _input.Player.Enable();
        }

        public GameObject DialogueContainer => dialogueContainer;
        public GameObject ChoicesContainer => choicesContainer;
        public GameObject SpeakSeparator => speakSeparator;
        public GameObject ChoicesSeparator => choicesSeparator;
        public TextMeshProUGUI SpeakerName => speakerName;
        public TextMeshProUGUI SpeakerLine => speakerLine;

        public RuntimeDialogueGraph DialogueLogic => dialogueLogic;
        public IChoicesController ChoicesController => choicesController;
        public bool DialogueActive { get; private set; }

        public void StartDialogue() {
            if (DialogueActive) return;

            DialogueLogic.ResetConversation();
            gameObject.SetActive(true);
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