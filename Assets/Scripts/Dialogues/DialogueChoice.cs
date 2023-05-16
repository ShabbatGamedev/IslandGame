using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dialogues {
    public class DialogueChoice : MonoBehaviour {
        [SerializeField] TextMeshProUGUI choiceText;
        [SerializeField] TextMeshProUGUI indexText;
        [SerializeField] Image choiceFrame;

        public int Index { get; private set; }
        bool _initialized;

        void Awake() {
            if (_initialized) return;
            
            Initialize("Not initialized", 1);
        }

        public void Initialize(string text, int index) {
            Index = index;
            
            choiceText.text = text;
            indexText.text = $"{index + 1}.";

            _initialized = true;
        }

        public void Select(bool value) => choiceFrame.enabled = value;
    }
}