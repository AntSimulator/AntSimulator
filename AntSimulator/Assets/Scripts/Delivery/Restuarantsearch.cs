using UnityEngine;
using UnityEngine.UI;

namespace Delivery
{
    public class Restaurantsearch : MonoBehaviour
    {
        public Button searchButton;
        // 1. 단일 프리팹 대신, 6개의 서로 다른 프리팹을 담을 배열을 만듭니다.
        public GameObject[] myCustomPrefabs;

        void Start()
        {
            ClearList();
            if (searchButton != null)
            {
                searchButton.onClick.AddListener(OnSearchButtonClick);
            }
        }

        public void OnSearchButtonClick()
        {
            this.gameObject.SetActive(true);
            ClearList();

            // 2. 미리 준비한 6개의 프리팹을 순서대로 생성합니다.
            for (int i = 0; i < myCustomPrefabs.Length; i++)
            {
                if (myCustomPrefabs[i] == null) continue;

                GameObject newRes = Instantiate(myCustomPrefabs[i], this.transform);
                newRes.SetActive(true);

                RectTransform rect = newRes.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;       // 크기 1 고정
                rect.localPosition = Vector3.zero;    // 부모 기준 위치 초기화
                rect.anchoredPosition3D = Vector3.zero; // Z값을 포함한 모든 좌표 0으로 초기화
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            Debug.Log(myCustomPrefabs.Length + "개의 하드코딩된 프리팹이 생성되었습니다.");
        }

        void ClearList()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}