using UnityEngine;

public class Rotator2 : MonoBehaviour
{
    public float rotateSpeed2 = 200f;

    void Update()
    {
        transform.Rotate(0, 0, -rotateSpeed2 * Time.deltaTime);
    }
}