using UnityEngine;
using UnityEngine.EventSystems; // UI 이벤트를 처리하기 위해 꼭 필요합니다!

// IBeginDragHandler, IDragHandler 인터페이스를 상속받습니다.
public class UIDragger : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Tooltip("드래그할 때 실제로 움직일 전체 창 (비워두면 이 스크립트가 붙은 오브젝트가 움직임)")]
    public RectTransform targetWindow; 
    
    private Canvas canvas;

    void Awake()
    {
        // 캔버스의 스케일 값을 가져오기 위해 최상단 캔버스를 찾습니다.
        canvas = GetComponentInParent<Canvas>();
        
        // targetWindow를 지정하지 않았다면, 스크립트가 붙은 자기 자신을 움직입니다.
        if (targetWindow == null)
        {
            targetWindow = GetComponent<RectTransform>();
        }
    }

    // 마우스로 드래그를 시작하는 순간 1번 실행됨
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 클릭한 창을 화면 맨 앞으로 가져옵니다. (팝업창이 여러 개일 때 유용함)
        targetWindow.SetAsLastSibling(); 
    }

    // 마우스를 클릭한 채로 움직이는 동안 매 프레임 실행됨
    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 이동량(delta)을 캔버스 해상도 비율(scaleFactor)로 나누어 창 위치에 더해줍니다.
        // scaleFactor로 나누지 않으면 해상도에 따라 마우스와 창이 따로 노는 버그가 생깁니다.
        targetWindow.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}