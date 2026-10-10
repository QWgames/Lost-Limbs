using UnityEngine;

public class CharacterFix : MonoBehaviour
{
    [Header("Tracking Settings")]
    public Transform rootTransform; // The start of your IK chain (e.g., the shoulder)
    public float maxDistance = 3.5f; // Maximum length of the entire joint chain

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. Get the mouse position in world coordinates
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f; // Keep it in 2D space

        // 2. Calculate direction and distance from the root
        Vector3 direction = mousePos - rootTransform.position;
        float currentDistance = direction.magnitude;

        // 3. If mouse is too far, clamp the target position to the max distance
        if (currentDistance > maxDistance)
        {
            transform.position = rootTransform.position + (direction.normalized * maxDistance);
        }
        else
        {
            transform.position = mousePos;
        }
    }
}
