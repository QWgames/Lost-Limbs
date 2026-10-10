using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public class PlatformChaser : MonoBehaviour
{
    [Tooltip("The target we try to chase.")]
    public GameObject target;

    [Tooltip("The distance at which we start chasing the target.")]
    [SerializeField] float agroDistance = 10f;

    [Tooltip("The distance at which we stop chasing the target.")]
    [SerializeField] float stoppingDistance = .1f;

    [Tooltip("The minimum distance we try to be from the target.")]
    [SerializeField] float minimumDistance = .05f;
    
    public enum PursuitType
    {
        Chase,
        Run
    }
    
    public PursuitType pursuitType;

    IMove motor;
    IJump jumpMotor;
    bool inAggroRange = false;
    float lastUpdate = 0f;

    void Start()
    {
        motor = GetComponent<IMove>();
        jumpMotor = GetComponent<IJump>();
        if (!target)
        {
            target = Utility.Utility.FindPlayer();
        }
    }

    void FixedUpdate(){
        if (!target)
        {
            target = Utility.Utility.FindPlayer();
        }
        
        //Check to see if we should go aggresive
        if (!inAggroRange && (target.transform.position - transform.position).magnitude <= agroDistance){
            inAggroRange = true;
        }

        //if within aggro range, lets move towards / away our target
        if (inAggroRange && Time.time > lastUpdate + .1f){
            lastUpdate = Time.time;
            if (target){
                // We are closer than we want, so lets back up to our minimum distance
                if (Mathf.Abs(target.transform.position.x - transform.position.x) <= minimumDistance)
                {
                    float retreatDirection = transform.position.x - target.transform.position.x;
                    retreatDirection = retreatDirection / Mathf.Abs(retreatDirection);

                    motor.Move(new Vector2(retreatDirection, 0));
                }
                // We are closer than the stopping distance, so lets stop moving
                else if (Mathf.Abs(target.transform.position.x - transform.position.x) <= stoppingDistance) {
                    motor.Move(Vector2.zero);
                }
                else
                {
                    if(pursuitType == PursuitType.Run)
                    {
                        // move away from target
                        motor.Move(new Vector2(transform.position.x - target.transform.position.x, 0).normalized);
                    }
                    else
                    {
                        // move towards target
                        motor.Move(new Vector2(target.transform.position.x - transform.position.x, 0).normalized);
                    }
                }

            }
            else{
                Debug.Log(gameObject.name + " is agro, but has no target assigned");
            }

            if (jumpMotor != null) {
                if (jumpMotor.CheckEdge() || jumpMotor.CheckWall()) {
                    jumpMotor.Jump();
                }
            }
        }
    }
}
