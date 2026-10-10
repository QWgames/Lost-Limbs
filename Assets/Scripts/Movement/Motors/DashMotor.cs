using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DashMotor : MonoBehaviour
{
    private Rigidbody2D rb;
    
    [Tooltip("How fast does the Dash go?")]
    public float dashSpeed = 20f;
    [Tooltip("How long does the dash last?")]
    public float dashDuration = 0.2f;
    [Tooltip("How long is the cooldown between dashes?")]
    public float dashCooldown = 1f;
    public float dashCooldownTimer = 0f;
    // public float 
    public bool canDash = true;
    public bool willDash = false;
    public bool dashing = false;
    //
    public AudioClip jumpSound;
    [Range(0f,1f)]
    public float soundVolume = 1f;
    //
    public KeyCode dashKey = KeyCode.LeftShift;
    
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    // Update is called once per frame
    void Update()
    {
        if(!canDash)
        {
            dashCooldownTimer += Time.deltaTime;
            if(dashCooldownTimer >= dashCooldown)
            {
                canDash = true;
                dashCooldownTimer = 0f;
            }
        }
        
        if (Input.GetKeyDown(dashKey) && canDash)
        {
            willDash = true;
        }
    }

    void FixedUpdate()
    {
        if(willDash)
        {
            StartCoroutine(Dash());
            willDash = false;
        }
    }

    IEnumerator Dash()
    {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.right * dashSpeed, ForceMode2D.Impulse);
            yield return new WaitForSeconds(dashDuration);
            canDash = true;
            dashCooldownTimer = dashCooldown;   
        yield return false;
    }
}
