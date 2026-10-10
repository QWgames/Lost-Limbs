using UnityEngine;

public class PausePlayerMovementAtStart : MonoBehaviour
{
    public Rigidbody2D rb;
    public PlayerInputController playerInput;
    public FlappyJumpMotor jumpMotor;
    public float pauseTime = 2f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb.gravityScale = 0;
        playerInput.enabled = false;
        jumpMotor.enabled = false;
        Invoke(nameof(ResumeMovement), pauseTime);
    }

    public void ResumeMovement()
    {
        rb.gravityScale = 1;
        playerInput.enabled = true;
        jumpMotor.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
