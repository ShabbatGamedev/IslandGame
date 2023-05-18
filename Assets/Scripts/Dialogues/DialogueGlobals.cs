using System.Collections.Generic;
using System.Linq;
using DialogueGraph.Runtime;
using Input;
using TMPro;
using UnityEngine;

namespace Dialogues {
    public class DialogueGlobals : MonoBehaviour {
        [SerializeField] public List<GameObject> dialoguePrefabs;

        [SerializeField] GameObject otherUI;
        [SerializeField] GameObject dialogueContainer;
        [SerializeField] GameObject choicesContainer;
        [SerializeField] GameObject speakSeparator;
        [SerializeField] GameObject choicesSeparator;
        [SerializeField] TextMeshProUGUI speakerName;
        [SerializeField] TextMeshProUGUI speakerLine;

        [SerializeField] DialogueChoice choicePrefab;
        [SerializeField] ChoicesController choicesController;

        public List<Dialogue> Dialogues { get; } = new();
        PlayerInput _input;

        static DialogueGlobals Instance { get; set; }

        public static T GetDialogue<T>() where T : Dialogue => 
            Instance.Dialogues.FirstOrDefault(dialogue => dialogue is T) as T;

        void Awake() {
            _input = InputsSingleton.PlayerInput;

            ChoicesController controller = choicesController;
            
            controller.Prefab = choicePrefab;
            controller.Input = _input.DialogueSelection;

            dialoguePrefabs.ForEach(prefab => {
                GameObject dialogue = Instantiate(prefab, transform);
                
                if (!dialogue.TryGetComponent(out Dialogue dialogueComponent)) {
                    Debug.Log($"Prefab {prefab} has no {nameof(Dialogue)} component! Please, remove it from {nameof(DialogueGlobals)} GameObject.");
                    return;
                }

                if (!dialogueComponent.TryGetComponent(out RuntimeDialogueGraph dialogueLogic)) {
                    Debug.Log($"Prefab {prefab} has no {nameof(RuntimeDialogueGraph)} component! Please, remove it from {nameof(DialogueGlobals)} GameObject.");
                    return;
                }

                dialogueComponent.OtherUI = otherUI;
                dialogueComponent.DialogueContainer = dialogueContainer;
                dialogueComponent.ChoicesContainer = choicesContainer;
                dialogueComponent.SpeakSeparator = speakSeparator;
                dialogueComponent.ChoicesSeparator = choicesSeparator;
                dialogueComponent.SpeakerName = speakerName;
                dialogueComponent.SpeakerLine = speakerLine;
                dialogueComponent.Input = _input;
                dialogueComponent.DialogueLogic = dialogueLogic;
                dialogueComponent.ChoicesController = controller;
                
                controller.Dialogue = dialogueComponent;

                Dialogues.Add(dialogueComponent);
            });
        }

        DialogueGlobals() => Instance = this;
    }
}