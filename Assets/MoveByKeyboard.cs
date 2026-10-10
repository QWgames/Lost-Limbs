using UnityEngine;

public class MoveByKeyboard : MonoBehaviour
{
    public float moveSpeed = 5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        Vector2 direction = new Vector2(0, -horizontalInput);

        transform.Translate(direction * moveSpeed * Time.deltaTime);

    }
}
