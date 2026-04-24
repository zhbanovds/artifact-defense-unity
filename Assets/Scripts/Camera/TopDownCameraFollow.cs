using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [SerializeField] private float followSpeed = 8f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime);

        transform.LookAt(target.position);
    }
}
