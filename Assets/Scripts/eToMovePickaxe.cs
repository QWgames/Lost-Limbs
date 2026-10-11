using UnityEngine;

public class eToMovePickaxe : MonoBehaviour
{
    public GameObject pick;
    public float positionAdd = 0.1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if(Input.GetMouseButtonDown(0))
        {
            pick.transform.position = new Vector3(transform.position.x, transform.position.y + positionAdd, 0);
        }
        if (Input.GetMouseButtonUp(0))
        {
            pick.transform.position = new Vector3(transform.position.x, transform.position.y - positionAdd, 0);
        }
    }
}
