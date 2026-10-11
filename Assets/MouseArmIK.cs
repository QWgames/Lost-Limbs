using UnityEngine;
using UnityEngine.InputSystem;

public class MouseArmIK : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform elbowPivot;
    [SerializeField] private Transform handTarget;
    [SerializeField] private Camera gameCamera;

    [Header("Arm Lengths")]
    [SerializeField, Min(0.01f)] private float upperArmLength = 0.2f;
    [SerializeField, Min(0.01f)] private float forearmLength = 0.2f;

    [Header("Elbow")]
    [SerializeField] private float elbowBendDirection = 1f;

    // External target controlled by ClimbingController2D.
    private bool externalTargetActive;
    private Vector2 externalTargetPosition;

    public void SetExternalHandTarget(Vector2 worldPosition)
    {
        externalTargetPosition = worldPosition;
        externalTargetActive = true;
    }

    public void ClearExternalHandTarget()
    {
        externalTargetActive = false;
    }

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (elbowPivot == null || gameCamera == null)
            return;

        if (Mouse.current == null && !externalTargetActive)
            return;

        Vector2 targetPosition;

        if (externalTargetActive)
        {
            targetPosition = externalTargetPosition;
        }
        else
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();

            Vector3 screenPosition = new Vector3(
                mouseScreen.x,
                mouseScreen.y,
                -gameCamera.transform.position.z
            );

            targetPosition =
                gameCamera.ScreenToWorldPoint(screenPosition);
        }

        Vector2 shoulderPosition = transform.position;
        Vector2 toTarget = targetPosition - shoulderPosition;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Vector2 direction = toTarget.normalized;

        // Reverse the elbow bend when the character faces left.
        bool facingLeft =
            transform.parent != null &&
            transform.parent.lossyScale.x < 0f;

        float bendDirection = facingLeft
            ? -Mathf.Sign(elbowBendDirection)
            : Mathf.Sign(elbowBendDirection);

        float distance = toTarget.magnitude;

        // Clamp the target to the arm's reachable range.
        float maxReach =
            upperArmLength + forearmLength - 0.001f;

        float minReach =
            Mathf.Abs(upperArmLength - forearmLength) + 0.001f;

        distance = Mathf.Clamp(distance, minReach, maxReach);

        float along =
            (upperArmLength * upperArmLength
            - forearmLength * forearmLength
            + distance * distance)
            / (2f * distance);

        float heightSquared =
            upperArmLength * upperArmLength - along * along;

        float height = Mathf.Sqrt(Mathf.Max(0f, heightSquared));

        Vector2 perpendicular =
            new Vector2(-direction.y, direction.x);

        Vector2 elbowPosition =
            shoulderPosition
            + direction * along
            + perpendicular * height * bendDirection;

        // Rotate the upper arm toward the calculated elbow position.
        Vector2 upperArmDirection =
            elbowPosition - shoulderPosition;

        Vector2 upperArmLocalDirection =
            transform.parent != null
                ? (Vector2)transform.parent.InverseTransformDirection(
                    upperArmDirection.normalized
                )
                : upperArmDirection.normalized;

        float upperArmAngle =
            Mathf.Atan2(
                upperArmLocalDirection.y,
                upperArmLocalDirection.x
            ) * Mathf.Rad2Deg;

        transform.localRotation =
            Quaternion.Euler(0f, 0f, upperArmAngle);

        // Rotate the forearm toward the target.
        Vector2 forearmDirection =
            targetPosition - elbowPosition;

        Vector2 forearmLocalDirection =
            transform.InverseTransformDirection(
                forearmDirection.normalized
            );

        float forearmAngle =
            Mathf.Atan2(
                forearmLocalDirection.y,
                forearmLocalDirection.x
            ) * Mathf.Rad2Deg;

        elbowPivot.localRotation =
            Quaternion.Euler(0f, 0f, forearmAngle);
    }

    private void OnDisable()
    {
        ClearExternalHandTarget();
    }
}