
using UnityEngine;

public class WaterSwimmingZone2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerSwimming2D swimmer =
            other.GetComponentInParent<PlayerSwimming2D>();

        if (swimmer != null)
            swimmer.EnterWater();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerSwimming2D swimmer =
            other.GetComponentInParent<PlayerSwimming2D>();

        if (swimmer != null)
            swimmer.ExitWater();
    }
}
