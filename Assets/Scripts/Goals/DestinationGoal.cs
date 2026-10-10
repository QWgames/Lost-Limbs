using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Can be placed on Player or Third-party Object

[System.Serializable]
public enum activationType
{
    None = 0,
    Activate,
    Deactivate
}


public class DestinationGoal : MonoBehaviour, ICompletible
{

    //public static DestinationGoal destinationGoal;

    [Header("Place this script on the object at the Destination. Use Trigger Collider!")]
    public bool playerArrived = false;

    [Header("-------GOAL OUTCOMES-------", order = 0)] [Header("0. Pause on Complete")]
    public bool PauseOnComplete;
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }
    
    [Header("A. Play a sound when goal met?", order =1), Space(30)]
    public bool playSound;
    public AudioClip sound;
    [Range(0, 1f)]
    public float volume = 1f;

    [Header("B. Change Scenes when goal met?"), Space(30)]
    public bool changeScene;
    [Range(0, 5f)]
    public float sceneChangeDelay;
    [Header("Type in name of scene")]
    public string sceneName;

    [Header("C. Enable an Object when goal met?"), Space(30)]
    // public bool enableAnObject;
    public activationType activationType;
    public GameObject TargetObject;

    // event delegate
    public delegate void GoalCompleted();
    // event called when Goal is completed. Other scripts can 
    public static event GoalCompleted goalCompleted;

    public bool completed = false;

    //private void Awake()
    //{
    //    if (destinationGoal == null) destinationGoal = this;
    //    // don't destory gameObject, please;
    //    else Destroy(this);
    //}

    // Start is called before the first frame update
    void Start()
    {
        if(TargetObject != null && activationType != activationType.None)
        {
            TargetObject.SetActive(activationType != activationType.Activate);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerArrived = true;
            Debug.Log("Arrived");

            Debug.Log("Destination GOAL MET ---------");
            // execute goal outcomes

            // play a sound
            if (playSound)
            {
                AudioManager.audioManager?.playAudio(sound, volume);
            }

            // change scene
            if (changeScene)
            {
                SceneController.sceneController?.delayedSceneLoad(sceneName, sceneChangeDelay);
            }

            // enable or disable object
            if(TargetObject != null)
            {
                if (activationType == activationType.Activate)
                {
                    TargetObject.SetActive(true);
                }
                else if (activationType == activationType.Deactivate)
                {
                    TargetObject.SetActive(false);
                }
            }
            
            if (PauseOnComplete)
            {
                PauseGame();
            }
            //

            completed = true;
        }
    }


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerArrived = false;
            Debug.Log("departed");

            completed = false;
        }
    }

    public bool Completed()
    {
        return completed;
    }


}
