using UnityEngine;
using UnityEngine.UI;
using Player.Runtime;

public class purchaseManager : MonoBehaviour
{
    public GameObject purchaseChang; // �ν����Ϳ��� PurchaseChang ���
    public int foodPrice = 230000;   // ��� ǰ�� ���� 23����
    
    
    [Header("Refs")]
    public PlayerController player;  
    public int healAmount = 10;   

    // �Ĵ� ��ư�� ������ �� ȣ��
    public void OpenPopup()
    {
        purchaseChang.SetActive(true); // �˾� ����
    }

    // '��' ��ư�� ������ �Լ�
    public void OnClickYes()
    {
        if (player == null)
        {
            Debug.LogError("[purchaseManager] player is null");
            return;
        }

        player.Cash -= foodPrice;
        player.AddHp(healAmount);
        purchaseChang.SetActive(false);
        Debug.Log($"{foodPrice}사용 HP + {healAmount}");
    }

    // '�ƴϿ�' ��ư�� ������ �Լ�
    public void OnClickNo()
    {
        purchaseChang.SetActive(false); // �׳� �ݱ�
        Debug.Log("â ����");
    }
}