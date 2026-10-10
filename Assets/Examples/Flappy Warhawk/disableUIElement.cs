using UnityEngine;

public class disableUIElement : MonoBehaviour
{

    public GameObject UIElement;
    public KeyCode disableKey = KeyCode.Space;
    
    public void disableElement()
    {
        UIElement.SetActive(false);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(disableKey))
        {
            disableElement();
        }
    }
}
