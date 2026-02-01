using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stocks.UI
{
    public class StockRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;

        public void Bind(string stockName, Color iconColor)
        {
            if (nameText != null) nameText.text = stockName;

            if (iconImage != null)
            {
                iconImage.color = iconColor;
                iconImage.enabled = true;
            }
        }
    }
}