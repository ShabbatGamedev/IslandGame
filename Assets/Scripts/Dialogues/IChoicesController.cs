using System.Collections.Generic;
using DialogueGraph.Runtime;

namespace Dialogues {
    public interface IChoicesController {
        public DialogueChoice Prefab { get; }
        public IDialogue Dialogue { get; }

        public void Initialize(List<ConversationLine> lines);
        
        public void Activate();
        public void Deactivate();
    }
}