using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SanityBar : MonoBehaviour
{
    public Image Bar;
    public float sanity, maxsanity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Bar.fillAmount = sanity / maxsanity;
    }
}
