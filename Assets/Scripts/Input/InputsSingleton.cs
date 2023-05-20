namespace Input {
    public static class InputsSingleton {
        static PlayerInput _playerInput;
        static readonly object Lock = new();

        public static PlayerInput PlayerInput {
            get {
                lock (Lock) {
                    return _playerInput ??= new PlayerInput();
                }
            }
        }
    }
}