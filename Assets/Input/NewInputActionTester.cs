using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewInputActionTester : MonoBehaviour
{
    public InputActionAsset inputActionAsset;
    Vector2 movementInput;
    
    private void OnEnable()
    {
        print("NewInputActionTester started");
        print("InputActionAsset: " + inputActionAsset);
        print(inputActionAsset.FindActionMap("Player"));
        print(inputActionAsset.FindActionMap("Player").FindAction("Movement"));
        inputActionAsset.FindActionMap("Player").Enable();
        // inputActionAsset.actionMaps.
    }
    
    private void OnDisable()
    {
        print("NewInputActionTester started");
        inputActionAsset.FindActionMap("Player").Disable();
        // inputActionAsset.actionMaps.
    }

    private void Awake()
    {
        print("NewInputActionTester awake");
        movementInput = inputActionAsset.FindActionMap("Player").FindAction("Movement").ReadValue<Vector2>();
    }
    
    // This method is called when the "Movement" action is triggered and uses 'Unity Events' to pass the input value to this method.    
    public void Movement(InputAction.CallbackContext context){
        print("Movement Input received: " + context.ReadValue<Vector2>());
    }
    
    // This method is called when the "Movement" action is triggered and uses 'Unity Events' to pass the input value to this method.    
    public void Jump(InputAction.CallbackContext context){
        print("Jump Input received: " + context.ReadValue<float>());
    }
    
    // This method is called when the "Movement" action is triggered and uses 'Send Message' to pass the input value to this method.
    public void OnMovement(InputValue inputValue){
        print("Movement Input received: " + inputValue.Get<Vector2>());
    }

    // This method is called when the "Movement" action is triggered and uses 'Send Message' to pass the input value to this method.
    public void OnJump(InputValue inputValue)
    {
        print("Jump Input received: " + inputValue);
    }
    
    // public void OnMovement(InputAction.CallbackContext context)
    // {
    //     if (context.performed)
    //     {
    //         Vector2 moveVector = context.ReadValue<Vector2>();
    //         Debug.Log($"Moving: {moveVector}");
    //     }
    // }

    private void Update()
    {
        print("Current movement input: " + movementInput);
    }
}
