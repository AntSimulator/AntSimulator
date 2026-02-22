using UnityEngine;

namespace Delivery
{
    [CreateAssetMenu(fileName = "NewDelivery", menuName = "Scriptable Objects/DeliverySO")]
    public class DeliverySO : ScriptableObject
    {
        [Header("음식 정보")]
        public int price;
        public int healAmount;
    }
}
