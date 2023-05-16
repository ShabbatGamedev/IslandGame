namespace Interactions {
    public interface IInteractable {
        public string HintText { get; }
        
        public void Interact(IInteractor interactor);
    }
}