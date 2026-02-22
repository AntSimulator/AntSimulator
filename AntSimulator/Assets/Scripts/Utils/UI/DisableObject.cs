using UnityEngine;

public class DisableObject : MonoBehaviour
{
    public GameObject thing;

    public void Close()
    {
        thing.SetActive(false);
    }

}
