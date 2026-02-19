using System;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Example;

namespace Player.UI
{
    public class PopupPanelSwitcher : MonoBehaviour
    {
        public static event Action<int> OnPanelChanged;

        [Header("Popup Panels")] [SerializeField] private List<GameObject> panels = new();

        [SerializeField] private int startIndex = 0;

        private int _index = 0;

        private void OnEnable()
        {
            if(panels == null || panels.Count == 0) return;
            _index = Mathf.Clamp(startIndex, 0, panels.Count - 1);
            ShowOnly(_index);
            OnPanelChanged?.Invoke(_index);
        }

        public void NextPanel()
        {
            if(panels == null || panels.Count == 0) return;
            _index = (_index + 1) % panels.Count;
            ShowOnly(_index);
            OnPanelChanged?.Invoke(_index);
        }

        public void ShowPanel(int index)
        {
            if(panels == null||panels.Count == 0) return;
            _index = Mathf.Clamp(index, 0, panels.Count - 1);
            ShowOnly(_index);
            OnPanelChanged?.Invoke(_index);
        }

        public void ShowOnly(int activeIndex)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if(panels[i] == null) continue;

                // Keep panel GameObjects active so their subscriptions/lifecycle keep running.
                if (!panels[i].activeSelf)
                    panels[i].SetActive(true);

                var canvasGroup = GetOrAddCanvasGroup(panels[i]);
                bool isActive = i == activeIndex;
                canvasGroup.alpha = isActive ? 1f : 0f;
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive;
            }
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject panel)
        {
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();

            return canvasGroup;
        }
    }
}
