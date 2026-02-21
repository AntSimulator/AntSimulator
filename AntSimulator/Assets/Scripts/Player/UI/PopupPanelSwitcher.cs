using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Player.UI
{
    public class PopupPanelSwitcher : MonoBehaviour
    {
        [Serializable]
        private class PanelEntry
        {
            public GameObject panel;
            public string label;
        }

        public static event Action<int> OnPanelChanged;

        [Header("Popup Panels")] [SerializeField] private List<PanelEntry> panels = new();
        [SerializeField] private TMP_Text panelLabelText;

        [SerializeField] private int startIndex = 0;

        private int _index = 0;

        private void OnEnable()
        {
            if(panels == null || panels.Count == 0) return;
            _index = Mathf.Clamp(startIndex, 0, panels.Count - 1);
            ShowOnly(_index);
            UpdateLabel(_index);
            OnPanelChanged?.Invoke(_index);
        }

        public void NextPanel()
        {
            if(panels == null || panels.Count == 0) return;
            _index = (_index + 1) % panels.Count;
            ShowOnly(_index);
            UpdateLabel(_index);
            OnPanelChanged?.Invoke(_index);
        }

        public void ShowPanel(int index)
        {
            if(panels == null||panels.Count == 0) return;
            _index = Mathf.Clamp(index, 0, panels.Count - 1);
            ShowOnly(_index);
            UpdateLabel(_index);
            OnPanelChanged?.Invoke(_index);
        }

        public void ShowOnly(int activeIndex)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if(panels[i] == null || panels[i].panel == null) continue;
                panels[i].panel.SetActive(i == activeIndex);
            }
        }

        private void UpdateLabel(int activeIndex)
        {
            if(panelLabelText == null) return;
            if(activeIndex < 0 || activeIndex >= panels.Count) return;
            panelLabelText.text = panels[activeIndex].label ?? string.Empty;
        }
    }
}
