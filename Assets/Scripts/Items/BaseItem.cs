using Interactions;

namespace Items {
    public abstract class BaseItem : Interactable {
        public abstract ItemObject Item { get; }
        public override string HintText => $"[{InteractionKey}] {Item.hintText}";
    }
}