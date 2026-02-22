using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Player.Runtime;
using Delivery;

public class purchaseManager : MonoBehaviour
{
    public GameObject purchaseChang; // 인스펙터에서 PurchaseChang 오브젝트

    [Header("Refs")]
    public PlayerController player;

    // 현재 선택된 음식 데이터 (OpenPopup 호출 시 설정됨)
    private DeliverySO currentDeliveryData;

    // purchaseChang 안의 본문 Text(TMP)를 캐싱
    private TextMeshProUGUI _bodyText;
    private string _originalBodyText;

    private void Awake()
    {
        if (purchaseChang != null)
        {
            TextMeshProUGUI[] tmps = purchaseChang.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                // "예" / "아니요" 버튼 텍스트가 아닌 본문 텍스트를 식별
                if (tmp.gameObject.name == "Text (TMP)" &&
                    tmp.transform.parent != null &&
                    tmp.transform.parent.GetComponent<UnityEngine.UI.Button>() == null)
                {
                    _bodyText = tmp;
                    _originalBodyText = tmp.text;
                    break;
                }
            }
        }
    }

    // 음식 버튼을 눌렀을 때 호출 — 해당 음식의 DeliverySO를 넘겨서 팝업을 연다
    public void OpenPopup(DeliverySO data)
    {
        currentDeliveryData = data;

        // 팝업을 열 때 원래 텍스트로 복원
        if (_bodyText != null)
            _bodyText.text = _originalBodyText;
        purchaseChang.SetActive(true); // 창 열기
    }

    // '예' 버튼을 눌렀을 때 함수
    public void OnClickYes()
    {
        if (player == null)
        {
            Debug.LogError("[purchaseManager] player is null");
            return;
        }

        if (currentDeliveryData == null)
        {
            Debug.LogError("[purchaseManager] currentDeliveryData is null");
            return;
        }

        if (player.GetCash() < currentDeliveryData.price)
        {
            Debug.Log("Not enough cash to purchase food.");
            if (_bodyText != null)
                _bodyText.text = "돈이 부족합니다...!";
            return;
        }
        player.SubtractCash(currentDeliveryData.price);
        player.AddHp(currentDeliveryData.healAmount);
        purchaseChang.SetActive(false);
        Debug.Log($"{currentDeliveryData.price}사용 HP + {currentDeliveryData.healAmount}");
    }

    // '아니요' 버튼을 눌렀을 때 함수
    public void OnClickNo()
    {
        purchaseChang.SetActive(false); // 그냥 닫기
        Debug.Log("창 닫기");
    }
}