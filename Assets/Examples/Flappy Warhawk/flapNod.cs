using System;
using UnityEngine;

public class flapNod : MonoBehaviour
{
    // if your character is rotating but off a fixed angle, adjust this value, likely will be 90 or -90 (degrees)
    [SerializeField] int angleOffset;

    Rigidbody2D rb;
    private float vVelocity = 0;
    private Vector3 dir = new Vector3(0, 0, 0);
    public int rotationSpeed = 2;
    public float maxFallSpeed = -3.0f;
    
    private void Start()
    {
        if(!TryGetComponent<Rigidbody2D>(out rb))
        {
            Debug.LogError("flapNod requires a Rigidbody2D component to work.");
        }
        
        // var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffset;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        vVelocity = rb.linearVelocityY;
        if (vVelocity < 0)
        {
            angleOffset -= rotationSpeed;
            if (angleOffset < -15)
                angleOffset = -15;
        }
        else if (vVelocity > 0)
        {
            angleOffset += rotationSpeed;
            if (angleOffset > 15)
            {
                angleOffset = 15;
                print("clamping angle!");
            }
        }
        
        if(vVelocity < maxFallSpeed)
        {
            vVelocity = maxFallSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vVelocity);
        }
        
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffset;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
