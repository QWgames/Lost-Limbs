using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public enum teleportRole
{
    origin,
    destination,
    both
};

public enum teleportHeading
{
    A, 
    B,
    C,
    D,
    E,
}

public enum InteractionMode
{
    onCollisionEnter,
    onInteract,
}

public class teleport : MonoBehaviour
{
    public String playerTag = "Player";
    public Transform teleportLocation;
    public teleportRole teleportRole = teleportRole.origin;
    public teleportHeading teleportHeading = teleportHeading.A;
    public InteractionMode interactionMode = InteractionMode.onCollisionEnter;
    public Transform playerTransform;
    public float inAreaStayDuration = 0.5f;
    public float inAreaStayCurrentDur = 0;
    public float teleportDelayInSeconds = 0.2f;
    public bool growShrinkAnimation = true;
    public float growShrinkAnimationDuration = 0.2f;

    public KeyCode interactKey = KeyCode.E;
    public teleport[] teleportDestinations;

    //
    public bool inArea = false;

    private void Start()
    {
        GetTeleportDestinations();
    }

    private void Update()
    {
        if(interactKey == KeyCode.None) return;
        if (Input.GetKeyDown(interactKey) && inArea && interactionMode == InteractionMode.onInteract && playerTransform != null)
        {
            Teleport();
        }
    }

    //
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            inArea = true;
            playerTransform =  other.gameObject.transform;
            inAreaStayCurrentDur = 0;
        }

        
        if (interactionMode != InteractionMode.onCollisionEnter) return;
        Teleport();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            inArea = false;
        }
    }
    
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            inArea = true;
            playerTransform =  other.gameObject.transform;
            inAreaStayCurrentDur = 0;
        }

        if (interactionMode != InteractionMode.onCollisionEnter) return;
        Teleport();
    }

    // private void OnCollisionStay2D(Collision2D other)
    // {
    //     
    //     if (other.gameObject.CompareTag(playerTag) && interactionMode == InteractionMode.onCollisionStay && playerTransform != null)
    //     {
    //         print("stay");
    //         inAreaStayCurrentDur  += Time.deltaTime;
    //         if (inAreaStayCurrentDur > inAreaStayDuration)
    //         {
    //             inArea = false;
    //             Teleport();
    //             // inAreaStayCurrentDur = 0;
    //         }
    //     }
    // }
    
    // private void OnTriggerStay2D(Collider2D other)
    // {
    //     
    //     if (other.gameObject.CompareTag(playerTag) && interactionMode == InteractionMode.onCollisionStay && playerTransform != null)
    //     {
    //         print("stay");
    //         inAreaStayCurrentDur  += Time.deltaTime;
    //         if (inAreaStayCurrentDur > inAreaStayDuration)
    //         {
    //             inArea = false;
    //             Teleport();
    //             // inAreaStayCurrentDur = 0;
    //         }
    //     }
    // }


    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            inArea = false;
            // playerTransform =  null;
        }
    }

    private void Teleport()
    {
        print("teleporting");
        if (teleportDestinations.Length is 0 or > 1)
        {
            print("Unable to find specific destinations: " + teleportDestinations.Length + " destinations.");
            if (teleportDestinations.Length > 1)
            {
                foreach (var dest in teleportDestinations)
                {
                    print("Destination: " + dest.gameObject.name);
                }
            }
            return;
        }

        Vector3 destination = (!teleportDestinations[0].teleportLocation)
            ? teleportDestinations[0].gameObject.transform.position
            : teleportDestinations[0].teleportLocation.position;

        StartCoroutine(TeleportDelay(destination));
    }

    IEnumerator TeleportDelay(Vector3 destination)
    {
        yield return new WaitForSeconds(teleportDelayInSeconds);
        var currentScale = playerTransform.localScale.x;
        var increment = currentScale / (growShrinkAnimationDuration * .5f);
        if (growShrinkAnimation)
        {
            
            while (playerTransform.localScale.x > 0)
            {
                playerTransform.position = playerTransform.position;
                playerTransform.localScale -= new Vector3(1,1,1) * (Time.deltaTime * increment);
                yield return null;
            }
        }
        playerTransform.position = destination;
        if (growShrinkAnimation)
        {
            
            while (playerTransform.localScale.x < 1)
            {
                playerTransform.position = playerTransform.position;
                playerTransform.localScale += new Vector3(1,1,1) * (Time.deltaTime * increment);
                yield return null;
            }
        }
    }

    private void GetTeleportDestinations()
    {
        teleportDestinations = Object.FindObjectsByType<teleport>(FindObjectsSortMode.None)
            .Where(t => t.gameObject != gameObject).ToArray()
            .Where(t => t.teleportRole is teleportRole.destination or teleportRole.both).ToArray()
            .Where(t => t.teleportHeading == this.teleportHeading).ToArray();
    }
}
