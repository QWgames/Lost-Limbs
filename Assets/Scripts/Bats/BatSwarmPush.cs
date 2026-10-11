using System;
using UnityEngine;

public class BatSwarmPush : MonoBehaviour
{
    [Header("Swarm")]
    public GameObject Swarm;
    public Collider2D SwarmCollider;
    public float strength = 1f;
    public float SwarmMoveSpeed = 1f;

    [Header("Player")]
    public GameObject Player;
    public Rigidbody2D rbPlayer;
    public Collider2D PlayerCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Swarm.transform.Translate(Vector3.forward * SwarmMoveSpeed * Time.deltaTime);

        if (SwarmCollider.IsTouching(PlayerCollider)){
            //Player.transform.position = new Vector3(transform.position.x+strength*Time.deltaTime, transform.position.y, 0);
            rbPlayer.AddForce(new Vector2(strength,0));
            
            Debug.Log("Swarm Collided");
        }
    }

}
