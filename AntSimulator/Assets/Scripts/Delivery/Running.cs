using UnityEngine;

public class Running : MonoBehaviour
{
    public float speed = 300f;        
    public float maxSpeed = 2000f;    
    public float acceleration = 50f;
    public float leftLimit = -600f;
    public float rightLimit = 800f;

    private bool movingRight = true;
    private RectTransform rectTransform;

  
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (speed < maxSpeed)
        {
            speed += acceleration * Time.deltaTime;
        }

        if (movingRight)
        {
            rectTransform.anchoredPosition += Vector2.right * speed * Time.deltaTime;
            if (rectTransform.anchoredPosition.x >= rightLimit)
            {
                movingRight = false;
                FlipImage();
            }
        }
        else
        {
            rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;
            if (rectTransform.anchoredPosition.x <= leftLimit)
            {
                movingRight = true;
                FlipImage();
            }
        }
    }

    void FlipImage()
    {
        Vector3 newScale = rectTransform.localScale;
        newScale.x *= -1;
        rectTransform.localScale = newScale;
    }
}