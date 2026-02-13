using System;
using UnityEngine;
using Utils;

namespace Player
{
    public class PopupPanelSwitcher : MonoBehaviour
    {
        [Header("Right Panels (PanelSwitchButton)")]
        [SerializeField] private GameObject tradePanel;
        [SerializeField] private GameObject portfolioPanel;
        [SerializeField] private bool openTradePanelOnEnable = true;

        [Header("Mid Panels (Chart/MyPortfolio Buttons)")]
        [SerializeField] private GameObject chartPanel;
        [SerializeField] private GameObject myPortfolioPanel;

        [Header("Popup Manager (Optional)")]
        [SerializeField] private PopupManager popupManager;
        [SerializeField] private string chartPanelId = "ChartPanel";
        [SerializeField] private string myPortfolioPanelId = "MyPortfolioPanel";

        private bool isPortfolioPanelOpen;
        public event Action<bool> PortfolioPanelActiveChanged;

        public bool IsPortfolioPanelOpen => isPortfolioPanelOpen;

        private void OnEnable()
        {
            SetRightPortfolioPanelActive(!openTradePanelOnEnable);
        }

        public void TogglePanel()
        {
            SetRightPortfolioPanelActive(!isPortfolioPanelOpen);
        }

        public void OpenTradePanel()
        {
            SetRightPortfolioPanelActive(false);
        }

        public void OpenChartPanel()
        {
            SetMidPortfolioPanelActive(false);
        }

        public void OpenPortfolioPanel()
        {
            SetRightPortfolioPanelActive(true);
        }

        public void OpenMyPortfolioPanel()
        {
            SetMidPortfolioPanelActive(true);
        }

        private void SetRightPortfolioPanelActive(bool active)
        {
            isPortfolioPanelOpen = active;

            SetPanelActive(tradePanel, !active);
            SetPanelActive(portfolioPanel, active);

            PortfolioPanelActiveChanged?.Invoke(active);
        }

        private void SetMidPortfolioPanelActive(bool openPortfolioPanel)
        {
            var shouldOpenChart = !openPortfolioPanel;
            var shouldOpenPortfolio = openPortfolioPanel;

            SetPanelActive(chartPanel, shouldOpenChart);
            SetPanelActive(myPortfolioPanel, shouldOpenPortfolio);

            SetPanelByPopupId(chartPanelId, shouldOpenChart);
            SetPanelByPopupId(myPortfolioPanelId, shouldOpenPortfolio);
        }

        private void SetPanelByPopupId(string popupId, bool active)
        {
            if (popupManager == null || string.IsNullOrWhiteSpace(popupId))
            {
                return;
            }

            if (active)
            {
                popupManager.Open(popupId);
                return;
            }

            popupManager.Close(popupId);
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(active);
        }
    }
}
