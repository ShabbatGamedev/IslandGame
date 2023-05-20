using Interactions;

namespace Items.ItemLogic {
    /// <summary>
    /// Base class of all interactable items logic
    /// </summary>
    public abstract class BaseItemLogic : Interactable {
        public abstract ItemObject Item { get; }
        public override string HintText => $"[{InteractionKey}] {Item.hintText}";

        public virtual bool PickupItem(Interactor interactor) => interactor.inventory.AddItem(Item);
    }
}