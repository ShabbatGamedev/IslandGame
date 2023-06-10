using Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Player.Inventory {
    public class InventorySlot : MonoBehaviour {
        const float NonSelectedAlpha = 0.3529411764705882f;
        const float SelectedAlpha = 0.5882352941176471f;

        public bool selected;

        [SerializeField] Image icon;
        [SerializeField] Image background;
        [SerializeField] TextMeshProUGUI itemsCount;
        ItemStack _stack;
        
        public bool HasStack => _stack != null;

        public void SetStack(ItemStack stack) {
            _stack = stack;
            _stack.StackUpdated += StackUpdated;

            UpdateUI();
        }

        public void ClearStack() {
            _stack.StackUpdated -= StackUpdated;
            _stack = null;

            UpdateUI();
        }

        public ItemStack GetStack() => _stack;

        public ItemObject GetItem() => HasStack ? _stack.LastItem() : null;

        public void SetSelection(bool value) {
            selected = value;

            background.color = SetAlpha(background.color, selected ? SelectedAlpha : NonSelectedAlpha);
        }

        static Color SetAlpha(Color color, float a) {
            color.a = a;
            return color;
        }

        void StackUpdated() {
            if (!_stack.HasItems) {
                ClearStack();
                return;
            }
            
            UpdateUI();
        }
        
        void UpdateUI() {
            if (_stack == null) {
                itemsCount.text = "";
                icon.enabled = false;
                icon.sprite = null;
            } else {
                itemsCount.text = _stack.ItemsCount > 1 ? $"{_stack.ItemsCount}" : "";
                icon.sprite = _stack.Icon;
                icon.enabled = true;
            }
        }
    }
}