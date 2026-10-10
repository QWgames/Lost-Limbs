using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConditionalInteractable : MonoBehaviour, IInteractable
{
    [Header("Put the sprite for when the object is turned OFF here")]
    public Sprite OffSprite;
    [Header("Put the sprite for when the object is turned ON here")]
    public Sprite OnSprite;

    private SpriteRenderer spriteRenderer;
    
    public enum ActivationType
    {
        Activate,
        Deactivate
    }
    
    [Header("Activate or Deactivate the Target(s)?")]
    public ActivationType targetActivationType;
    
    [Header("How many objects would you like this interactable to affect? Drag in the objects")]
    public GameObject[] target;
    //
    private bool triggerEntered = false;

    [Header("What conditionals activates this object? Must have 'Interactable' Script")]
    [SerializeField, SerializeReference]
    public GameObject [] interactables;

    [Header(
        "Is this interactable able to be triggered because all conditional interactables are active? - DO NOT CLICK")]
    public bool interactable = false;
    
    [Header("This interactable's interaction state. Active True = Triggered - DO NOT CLICK")]
    [SerializeField]
    private bool active = false;

    [Header("What key should the player press to use this interactable?")]
    public KeyCode keycode;
    void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

        for (int i = 0; i < target.Length; i++)
        {
            // deactivate if you want to activate when interacted with
            target[i].SetActive(targetActivationType == ActivationType.Deactivate);
        }
        interactable = false;
        active = false;
    }

    void Update()
    {
        interactable = isInteractable();

        // new input system
        if(Keyboard.current.spaceKey.wasPressedThisFrame && triggerEntered == true && interactable == true)
        {
            interact();
            Change();
        }
        
        // old input system
        if (Input.GetKeyDown(keycode) && triggerEntered == true && interactable == true)
        {
            interact();
            Change();
        }
    }

    public bool isActive()
    {
        return active;
    }

    public bool isInteractable() //Bool for object being interactable
    {
         if (checkInteractables())
         {
             return true;
         } return false;
    }

    public void interact() //parameteres for interactable objects
    {
        if (interactable)
        {
            for (int i = 0; i < target.Length; i++)
            {
             target[i].SetActive(!target[i].activeSelf);
            }
        }
        else Debug.Log("cannot interact");
        //
        active = !active;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player"))
        {
            triggerEntered = true;
            Debug.Log("trigger entered");
        }
    }

    private void OnTriggerExit2D()
    {

        triggerEntered = false;
    }

    public void Change()
    {
        spriteRenderer.sprite = spriteRenderer.sprite == OnSprite ? OffSprite : OnSprite;
    }
    public bool checkInteractables()
    {
        bool checker = false;
        for(int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i].GetComponent<IInteractable>() != null)
            {
                checker = interactables[i].GetComponent<IInteractable>().isActive();
                if (!checker)
                {
                    return checker;
                }
            } else
            {
                Debug.Log("Not all objects in the interactles contain an interactable script on object: " + gameObject.name);
                return false;
            }
        }
        return checker;
    }
}
