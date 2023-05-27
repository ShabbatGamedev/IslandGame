using System.Collections.Generic;
using System.Linq;
using Input;
using Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Inventory {
    public class InventorySystem : MonoBehaviour {
        /// <summary>
        /// Prefab with <see cref="InventorySlot"/> component.
        /// </summary>
        [SerializeField] GameObject prefab;
        [SerializeField] int maxSlots = 4;
        
        public bool HaveSpace => _stacks.Count < _slots.Count;
        public InventorySlot SelectedSlot => _slots.SingleOrDefault(slot => slot.selected);

        /// <summary>
        /// List with <see cref="InventorySlot"/> components>
        /// </summary>
        List<InventorySlot> _slots;
        PlayerInput.InventorySelectActions _input;
        List<ItemStack> _stacks => _slots.Where(slot => slot.HasStack).Select(slot => slot.GetStack()).ToList();

        void Awake() {
            _slots = new List<InventorySlot>(maxSlots);
            _input = InputsSingleton.PlayerInput.InventorySelect;

            for (int i = 0; i < maxSlots; i++) {
                GameObject slot = Instantiate(prefab, transform); // Create a slot on screen
                InventorySlot slotComponent = slot.GetComponent<InventorySlot>(); // Getting inventory slot component from created slot

                if (i == 0) slotComponent.SetSelection(true); // Select the first slot
                
                _slots.Add(slotComponent); // Adding the inventory slot component to the list
            }

            _slots.First().SetSelection(true);
        }

        void OnEnable() {
            _input.Enable();

            _input.SelectByNumbers.performed += SelectByNumbers;
            _input.MouseWheel.performed += MouseWheelSelect;
        }

        void OnDisable() {
            _input.Disable();

            _input.SelectByNumbers.performed -= SelectByNumbers;
            _input.MouseWheel.performed -= MouseWheelSelect;
        }

        public bool AddItem(ItemObject item) {
            ItemStack stack = _stacks.FirstOrDefault(stack => stack.CanTakeItem(item));

            if (stack != null) {
                stack.AddItem(item);
                return true;
            }

            if (!HaveSpace) return false;

            stack = new ItemStack(item);
            _slots.First(slot => !slot.HasStack).SetStack(stack);

            return true;
        }

        public void RemoveItem(ItemStack itemStack, ItemObject item) {
            ItemStack stack = _stacks.First(s => s == itemStack);

            stack.RemoveItem(item);
            if (!stack.HasItems) _stacks.Remove(stack);
        }

        void SelectByNumbers(InputAction.CallbackContext ctx) => SelectSlot((int)ctx.ReadValue<float>());

        void MouseWheelSelect(InputAction.CallbackContext ctx) {
            int currentIndex = _slots.IndexOf(SelectedSlot);
            float rotationAxis = ctx.ReadValue<float>();

            switch (rotationAxis) {
                case > 0:
                    SelectSlot(currentIndex + 1);
                    break;

                case < 0:
                    SelectSlot(currentIndex - 1);
                    break;
            }
        }

        void SelectSlot(int slotIndex) {
            int slotsCount = _slots.Count - 1;

            if (slotIndex > slotsCount) slotIndex = 0;
            else if (slotIndex < 0) slotIndex = slotsCount;

            SelectedSlot.SetSelection(false);
            _slots[slotIndex].SetSelection(true);
        }
    }
}