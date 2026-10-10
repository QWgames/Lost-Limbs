using JetBrains.Annotations;
using UnityEngine;

public class ToggleArm : MonoBehaviour
{
    public GameObject leftArm;
    public string lScript;
    public GameObject rightArm;
    public string rScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private bool armToggle;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(3))
        {
            if (armToggle)
            {
                leftArm.GetComponent<FollowMousePosition>().enabled = false;
                rightArm.GetComponent<FollowMousePosition>().enabled = true;
                armToggle = false;
            }
            else
            {
                rightArm.GetComponent<FollowMousePosition>().enabled = false;
                leftArm.GetComponent<FollowMousePosition>().enabled = true;
                armToggle = true;
            }
        }
    }
}
