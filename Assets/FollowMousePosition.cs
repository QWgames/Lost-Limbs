using UnityEngine;

public class FollowMousePosition : MonoBehaviour
{
    private Camera mainCamera;

    [SerializeField]
    private float maxSpeed = 10f;
    [SerializeField]
    private float strength = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Dellay(maxSpeed);
       // FollowMousPosition();
    }

    private void Dellay(float maxSpeed)
    {
        transform.position = Vector2.MoveTowards(transform.position, GetWorldPositionFromMouse(), maxSpeed * Time.deltaTime);
    }

    private void FollowMousPosition()
    {
        transform.position = GetWorldPositionFromMouse()/strength;
    }

    private Vector2 GetWorldPositionFromMouse()
    {
        return mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }
}
