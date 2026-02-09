using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Stocks.Models;

namespace Stocks.UI
{
    public class StockRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button button;

        StockSeedItem _item;
        Action<StockSeedItem> _onClick;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
        }

        public void Bind(StockSeedItem item, Color iconColor, Action<StockSeedItem> onClick)
        {
            _item = item;
            _onClick = onClick;

            if (nameText != null) nameText.text = item != null ? item.name : string.Empty;

            if (iconImage != null)
            {
                iconImage.color = iconColor;
                iconImage.enabled = true;
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        void HandleClick()
        {
            if (_item == null) return;
            _onClick?.Invoke(_item);
        }
    }
}
