using UnityEngine;

public class CameraMusic : MonoBehaviour
{
    public AudioClip music;

    void Start()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = music;
        audioSource.loop = true;
        audioSource.Play();
    }
}

