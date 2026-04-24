using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerImpactReceiver : MonoBehaviour
{
    [SerializeField] private float knockbackDamping = 12f;

    private CharacterController characterController;
    private Vector3 externalVelocity;
    private float movementBlockedUntil;

    public bool IsMovementBlocked => Time.time < movementBlockedUntil;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f || externalVelocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        characterController.Move(externalVelocity * Time.deltaTime);
        externalVelocity = Vector3.MoveTowards(
            externalVelocity,
            Vector3.zero,
            knockbackDamping * Time.deltaTime);
    }

    public void ReceiveImpact(Vector3 sourcePosition, float knockbackForce, float stunDuration)
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        Vector3 knockbackDirection = transform.position - sourcePosition;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude <= 0.001f)
        {
            knockbackDirection = -transform.forward;
        }

        externalVelocity = knockbackDirection.normalized * Mathf.Max(knockbackForce, 0f);
        movementBlockedUntil = Mathf.Max(
            movementBlockedUntil,
            Time.time + Mathf.Max(stunDuration, 0f));
    }
}
