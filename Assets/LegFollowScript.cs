using UnityEngine;

public class LegFollowScript : MonoBehaviour
{
    [SerializeField]
    public float moveSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject defaultLegPlacement;
    public GameObject legLimb;
    public float speed = 1f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //float horz = Input.GetAxisRaw("Horizontal");

        //Vector2 direction = new Vector2(0, -horz);

        //transform.Translate(direction * moveSpeed * Time.deltaTime);
        
   
        transform.position = Vector2.MoveTowards(transform.position, defaultLegPlacement.transform.position, speed * Time.deltaTime);
    }
}
