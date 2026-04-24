using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(100)]
public sealed class PlayerMovement : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private Animator animator;

    private CharacterController characterController;
    private PlayerImpactReceiver impactReceiver;
    private PlayerAttack playerAttack;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        impactReceiver = GetComponent<PlayerImpactReceiver>();
        playerAttack = GetComponent<PlayerAttack>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        if ((impactReceiver != null && impactReceiver.IsMovementBlocked)
            || (playerAttack != null && playerAttack.IsMovementLocked))
        {
            input = Vector2.zero;
        }

        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        ApplyGravity();
        Move(moveDirection);
        RotateTowards(moveDirection);
        UpdateAnimator(moveDirection);
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed)
        {
            input.y += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            input.y -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            input.x += 1f;
        }

        if (keyboard.aKey.isPressed)
        {
            input.x -= 1f;
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += Physics.gravity.y * Time.deltaTime;
    }

    private void Move(Vector3 moveDirection)
    {
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void RotateTowards(Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void UpdateAnimator(Vector3 moveDirection)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, moveDirection != Vector3.zero);
    }
}
