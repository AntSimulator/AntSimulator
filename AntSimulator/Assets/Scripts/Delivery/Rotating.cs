using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float rotateSpeed = 200f;

    void Update()
    {
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
}