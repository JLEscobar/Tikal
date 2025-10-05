using UnityEngine;

// Requiere los otros componentes para funcionar.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInputHandler))] // Depende del script de input.
public class CharacterMovement : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 2.0f;
    public float sprintSpeed = 5.0f;

    // Referencias a los componentes que necesita para trabajar.
    private CharacterController _controller;
    private Animator _animator;
    private PlayerInputHandler _input; // Referencia al script que lee los comandos.

    // Variables para optimizar el acceso a los parámetros del Animator.
    private int _animIDSpeed;
    private int _animIDMotionSpeed;

    private void Awake()
    {
        // Obtenemos las referencias a los componentes en el mismo GameObject.
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _input = GetComponent<PlayerInputHandler>();

        // Asignamos los nombres de los parámetros del Animator a IDs.
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Leemos los valores del input desde nuestro script especializado.
        Vector2 moveInput = _input.MoveInput;
        bool isSprinting = _input.SprintInput;

        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        float currentSpeed = moveDirection.magnitude > 0.1f ? targetSpeed : 0f;

        // Rotamos el personaje para que mire en la dirección del movimiento.
        if (moveDirection.magnitude >= 0.1f)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * 15.0f);
        }

        // Calculamos el vector de movimiento final.
        Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;

        // Le decimos al CharacterController que mueva el personaje.
        _controller.Move(movement);

        // Enviamos la información de velocidad al Animator.
        if (_animator != null)
        {
            _animator.SetFloat(_animIDSpeed, currentSpeed);
            _animator.SetFloat(_animIDMotionSpeed, moveDirection.magnitude);
        }
    }
}