using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(IMove))]
public class PlayerInputController : MonoBehaviour
{
    public enum inputMethod {oldInputSystem, newInputSystem}
    [Tooltip("Choose which input system to use. The old input system uses the Input class, while the new input system uses the Input System package.")]
    [Header("This script is used to control the player's movement and jumping.")]
    public inputMethod inputSystem = inputMethod.newInputSystem;
    public InputActionAsset newInputActionAsset;
    Vector2 movementInput;
    
    
    IMove motor;
    IJump jumpMotor;

    private void OnEnable()
    {
        // Enable the "Movement" action in the "Player" action map when the script is enabled
        newInputActionAsset.FindActionMap("Player").FindAction("Movement").Enable();
        // enable the "Jump" action in the "Player" action map when the script is enabled
        newInputActionAsset.FindActionMap("Player").FindAction("Jump").Enable();
    }
    
    private void OnDisable()
    {
        // Disable the "Movement" action in the "Player" action map when the script is disabled
        newInputActionAsset.FindActionMap("Player").FindAction("Movement").Disable();
        // disable the "Jump" action in the "Player" action map when the script is disabled
        newInputActionAsset.FindActionMap("Player").FindAction("Jump").Disable(); 
    }
    
    // This method is called when the "Movement" action is triggered and uses 'Send Message' to pass the input value to this method.
    public void OnMovement(InputValue inputValue){
        Vector2 direction = inputValue.Get<Vector2>();
        print("Input received: " + direction);
        motor?.Move(direction);
    }

    public void OnJump(InputValue inputValue)
    {
        print("New Input System Jump Input received: " + inputValue);
        if(inputValue.isPressed) jumpMotor?.Jump();
    }
    
    void Start(){
        motor = GetComponent<IMove>();
        jumpMotor = GetComponent<IJump>();
        if(motor is null){
            Debug.LogWarning("No Motor component found on " + gameObject.name);
        }
        if(jumpMotor is null){
            Debug.LogWarning("No JumpMotor component found on " + gameObject.name);
        }
    }

    void Update(){
        // Old Input System
        if(inputMethod.oldInputSystem == inputSystem)
        {
            motor?.Move(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
            if (jumpMotor != null) {
                if (Input.GetKeyDown(KeyCode.Space)) {
                    jumpMotor.Jump();
                }
            }
        }
        // new Input System
        if (newInputActionAsset.FindActionMap("Player").FindAction("Movement") != null && inputMethod.newInputSystem == inputSystem) {
            movementInput = newInputActionAsset.FindActionMap("Player").FindAction("Movement").ReadValue<Vector2>();
            motor?.Move(movementInput);
            print("Movement received: " + movementInput);
        }
        if (newInputActionAsset.FindActionMap("Player").FindAction("Jump") != null && inputMethod.newInputSystem == inputSystem) {
            if (newInputActionAsset.FindActionMap("Player").FindAction("Jump").IsPressed()) {
                jumpMotor?.Jump();
                print("Jump Input received");
            }
        }
    }
}
