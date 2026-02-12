using UnityEngine;

namespace Player
{
    public class PopupPanelSwitcher : MonoBehaviour
    {
        [Header("Popup Panels")]
        [SerializeField] private GameObject tradePanel;
        [SerializeField] private GameObject portfolioPanel;
        [SerializeField] private bool openTradePanelOnEnable = true;

        private bool isPortfolioPanelOpen;

        private void OnEnable()
        {
            SetPortfolioPanelActive(!openTradePanelOnEnable);
        }

        public void TogglePanel()
        {
            SetPortfolioPanelActive(!isPortfolioPanelOpen);
        }

        public void OpenTradePanel()
        {
            SetPortfolioPanelActive(false);
        }

        public void OpenPortfolioPanel()
        {
            SetPortfolioPanelActive(true);
        }

        private void SetPortfolioPanelActive(bool active)
        {
            isPortfolioPanelOpen = active;

            if (tradePanel != null)
            {
                tradePanel.SetActive(!active);
            }

            if (portfolioPanel != null)
            {
                portfolioPanel.SetActive(active);
            }
        }
    }
}
