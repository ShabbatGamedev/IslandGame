using System.Collections.Generic;
using DialogueGraph.Runtime;
using Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dialogues {
    public class ChoicesController : MonoBehaviour, IChoicesController {
        [SerializeField] DialogueChoice prefab;
        [SerializeField] HorseFuckDialogue dialogue;
        
        public DialogueChoice Prefab => prefab;
        public IDialogue Dialogue => dialogue;
        PlayerInput.DialogueSelectionActions _input;
        List<DialogueChoice> _choices;
        int _selectedIndex;

        void Awake() => _input = InputsSingleton.PlayerInput.DialogueSelection;
        void OnEnable() => _input.Enable();
        void OnDisable() => _input.Disable();

        void ChoiceByNumbers(InputAction.CallbackContext ctx) => SelectLine((int)ctx.ReadValue<float>());
        
        void SelectUpper(InputAction.CallbackContext ctx) {
            int nextIndex = Mathf.Max(_selectedIndex - 1, 0);
            _choices[_selectedIndex].Select(false);
            _choices[nextIndex].Select(true);
            _selectedIndex = nextIndex;
        }

        void SelectBottom(InputAction.CallbackContext ctx) {
            int nextIndex = Mathf.Min(_selectedIndex + 1, _choices.Count - 1);
            _choices[_selectedIndex].Select(false);
            _choices[nextIndex].Select(true);
            _selectedIndex = nextIndex;
        }

        void SubmitSelection(InputAction.CallbackContext ctx) => SelectLine(_selectedIndex);

        public void Initialize(List<ConversationLine> lines) {
            _choices = new List<DialogueChoice>();
            int lineIndex = 0;
            
            foreach (ConversationLine line in lines) {
                DialogueChoice choice = Instantiate(Prefab, transform);
                choice.Initialize(line.Message, lineIndex);
                _choices.Add(choice);
                
                lineIndex++;
            }

            _selectedIndex = 0;
            _choices[0].Select(true);
            
            _input.Enable();
            
            _input.ChoiceByNumbers.performed += ChoiceByNumbers;
            _input.SelectUpper.performed += SelectUpper;
            _input.SelectBottom.performed += SelectBottom;
            _input.SubmitSelection.performed += SubmitSelection;
        }

        public void SelectLine(int index) {
            Clear();
            _selectedIndex = -1;
            Dialogue.PlayerSelect(index);
        }

        public void Clear() {
            _input.Disable();
            
            _input.ChoiceByNumbers.performed -= ChoiceByNumbers;
            _input.SelectUpper.performed -= SelectUpper;
            _input.SelectBottom.performed -= SelectBottom;
            _input.SubmitSelection.performed -= SubmitSelection;

            _choices.ForEach(choice => Destroy(choice.gameObject));
            _choices.Clear();
        }

        public void Activate() => gameObject.SetActive(true);
        public void Deactivate() => gameObject.SetActive(false);
    }
}