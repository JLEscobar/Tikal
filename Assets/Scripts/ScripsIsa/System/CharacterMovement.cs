using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    private CharacterController _controller;
    private Vector3 _targetPosition;
    private bool _isMoving;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _targetPosition = transform.position;
    }

    void Update()
    {
        if (_isMoving)
        {
            MoveTowardsTarget();
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }

    public void MoveToPosition(Vector3 position)
    {
        if (_controller == null)
        {
            Debug.LogError($"[ENEMY_MOVEMENT] {gameObject.name}: CharacterController is null!");
            return;
        }

        if (!_controller.enabled)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: CharacterController is disabled! Enabling it...");
            _controller.enabled = true;
        }

        _targetPosition = position;
        _isMoving = true;
        Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Moving to position {position}. Distance: {Vector3.Distance(transform.position, position):F2}");
    }

    public void Stop()
    {
        _isMoving = false;
        _targetPosition = transform.position;
    }

    private void MoveTowardsTarget()
    {
        if (_controller == null || !_controller.enabled)
        {
            _isMoving = false;
            return;
        }

        Vector3 direction = (_targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, _targetPosition);

        if (distance < 0.1f)
        {
            _isMoving = false;
            Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Reached target position.");
            return;
        }

        // Move
        Vector3 move = direction * moveSpeed * Time.deltaTime;
        _controller.Move(move);

        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public bool IsMoving => _isMoving;
}
