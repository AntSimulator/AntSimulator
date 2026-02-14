using System.Collections.Generic;
using UnityEngine;
using XCharts.Example;

namespace Player
{
    public class PopupPanelSwitcher : MonoBehaviour
    {
        [Header("Popup Panels")] [SerializeField] private List<GameObject> panels = new();

        [SerializeField] private int startIndex = 0;

        private int _index = 0;

        private void OnEnable()
        {
            if(panels == null || panels.Count == 0) return;
            _index = Mathf.Clamp(startIndex, 0, panels.Count - 1);
            ShowOnly(_index);
        }

        public void NextPanel()
        {
            if(panels == null || panels.Count == 0) return;
            _index = (_index + 1) % panels.Count;
            ShowOnly(_index);
        }

        public void ShowPanel(int index)
        {
            if(panels == null||panels.Count == 0) return;
            _index = Mathf.Clamp(index, 0, panels.Count - 1);
            ShowOnly(_index);
        }

        public void ShowOnly(int activeIndex)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if(panels[i] == null) continue;
                panels[i].SetActive(i == activeIndex);
            }
        }
    }
}
