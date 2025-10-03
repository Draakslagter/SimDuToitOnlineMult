using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerNetworkMovement : NetworkBehaviour
{
    private CharacterInput _inputMap;
    
    [Header("Movement")]
    private Rigidbody _characterRb;
    private Transform _playerTransform;
    private Vector3 _movementVector;
    [SerializeField] private float moveSpeedMultiplier = 5f, dashSpeedMultiplier = 1.5f;
    private float _actualSpeed;
   
    [Header("Jump")]
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.5f, jumpMultiplier = 5f;
    private void Awake()
    {
        _inputMap = new CharacterInput();
        _inputMap.Enable();
        _inputMap.PlayerMap.Pause.performed += OnPause;
        _inputMap.PlayerMap.Jump.performed += OnJump;
        _inputMap.PlayerMap.RunDash.performed += OnDash;
        _inputMap.PlayerMap.Movement.performed += OnMove;
        _inputMap.PlayerMap.Movement.canceled += ResetMovement;

        if (_characterRb == null)
        {
            _characterRb = GetComponent<Rigidbody>();
        }

        if (_playerTransform == null)
        {
            _playerTransform = GetComponent<Transform>();
        }
    }

    private void OnDisable()
    {
        _inputMap.PlayerMap.Pause.performed -= OnPause;
        _inputMap.PlayerMap.Jump.performed -= OnJump;
        _inputMap.PlayerMap.RunDash.performed -= OnDash;
        _inputMap.PlayerMap.Movement.performed -= OnMove;
        _inputMap.PlayerMap.Movement.canceled -= ResetMovement;
        _inputMap.Disable();
    }

    private void Start()
    {
        _actualSpeed = moveSpeedMultiplier;
    }
    
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        _characterRb.transform.Translate(_movementVector * (Time.deltaTime * _actualSpeed), Space.World);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _movementVector = new Vector3(context.ReadValue<Vector2>().x, 0, context.ReadValue<Vector2>().y);
    }

    private void ResetMovement(InputAction.CallbackContext context)
    {
        _movementVector = Vector3.zero;
        _actualSpeed = moveSpeedMultiplier;
    }
    private void OnDash(InputAction.CallbackContext context)
    {
        _actualSpeed = moveSpeedMultiplier * dashSpeedMultiplier;
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        var groundArray = Physics.OverlapSphere(groundCheckTransform.position, groundCheckRadius, groundLayer);
        if (groundArray.Length == 0) return;
        var jumpVector = new Vector3(0, jumpMultiplier, 0);
        _characterRb.AddForce(jumpVector, ForceMode.Impulse);
    }
    private void OnPause(InputAction.CallbackContext context)
    {
        //Pause Game
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}
