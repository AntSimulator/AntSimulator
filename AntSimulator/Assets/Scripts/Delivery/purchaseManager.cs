using UnityEngine;
using UnityEngine.UI;

public class purchaseManager : MonoBehaviour
{
    public GameObject purchaseChang; // 인스펙터에서 PurchaseChang 등록
    public int foodPrice = 230000;   // 모든 품목 정가 23만원

    // 식당 버튼을 눌렀을 때 호출
    public void OpenPopup()
    {
        purchaseChang.SetActive(true); // 팝업 띄우기
    }

    // '예' 버튼에 연결할 함수
    public void OnClickYes()
    {
        // 1. 돈이 충분한지 확인 (예: StockManager 스크립트 연동)
        // if (StockManager.instance.currentMoney >= foodPrice) {

        // 2. 돈 차감 및 HP 증가 로직 실행
        Debug.Log(foodPrice + "원 결제! HP 회복 완료");

        // 3. 팝업 닫기
        purchaseChang.SetActive(false);
        // }
    }

    // '아니오' 버튼에 연결할 함수
    public void OnClickNo()
    {
        purchaseChang.SetActive(false); // 그냥 닫기
        Debug.Log("창 종료");
    }
}