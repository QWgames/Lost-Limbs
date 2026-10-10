using UnityEngine;
using UnityEngine.UIElements;

public class grabingScirpt : MonoBehaviour
{
    public CircleCollider2D hand;
    public GameObject arm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (hand.isTrigger && Input.GetMouseButtonDown(1))
        {
            //Vector2 cache = Position.Relative(hand);
            //transform.position
        }
    }



}
