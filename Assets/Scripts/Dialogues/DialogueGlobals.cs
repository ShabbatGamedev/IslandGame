using System.Collections.Generic;
using System.Linq;
using DialogueGraph.Runtime;
using Input;
using TMPro;
using UnityEngine;

namespace Dialogues {
    public class DialogueGlobals : MonoBehaviour {
        [SerializeField] public List<GameObject> dialoguePrefabs;

        [SerializeField] GameObject dialogueContainer;
        [SerializeField] GameObject choicesContainer;
        [SerializeField] GameObject speakSeparator;
        [SerializeField] GameObject choicesSeparator;
        [SerializeField] TextMeshProUGUI speakerName;
        [SerializeField] TextMeshProUGUI speakerLine;

        [SerializeField] DialogueChoice choicePrefab;
        [SerializeField] ChoicesController choicesController;

        public List<IDialogue> Dialogues { get; } = new();
        PlayerInput _input;

        static DialogueGlobals Instance { get; set; }

        public static T GetDialogue<T>() where T : class, IDialogue => 
            Instance.Dialogues.FirstOrDefault(dialogue => dialogue is T) as T;

        void Awake() {
            _input = InputsSingleton.PlayerInput;

            ChoicesController controller = choicesController;
            
            controller.Prefab = choicePrefab;
            controller.Input = _input.DialogueSelection;

            dialoguePrefabs.ForEach(prefab => {
                GameObject dialogue = Instantiate(prefab, transform);
                
                if (!dialogue.TryGetComponent(out IDialogue iDialogue)) {
                    Debug.Log($"Prefab {prefab} has no {nameof(IDialogue)} component! Please, remove it from {nameof(DialogueGlobals)} GameObject.");
                    return;
                }

                if (!dialogue.TryGetComponent(out RuntimeDialogueGraph dialogueLogic)) {
                    Debug.Log($"Prefab {prefab} has no {nameof(RuntimeDialogueGraph)} component! Please, remove it from {nameof(DialogueGlobals)} GameObject.");
                    return;
                }

                iDialogue.DialogueContainer = dialogueContainer;
                iDialogue.ChoicesContainer = choicesContainer;
                iDialogue.SpeakSeparator = speakSeparator;
                iDialogue.ChoicesSeparator = choicesSeparator;
                iDialogue.SpeakerName = speakerName;
                iDialogue.SpeakerLine = speakerLine;
                iDialogue.Input = _input;
                iDialogue.DialogueLogic = dialogueLogic;
                iDialogue.ChoicesController = controller;
                
                controller.Dialogue = iDialogue;

                Dialogues.Add(iDialogue);
            });
        }

        DialogueGlobals() => Instance = this;
    }
}