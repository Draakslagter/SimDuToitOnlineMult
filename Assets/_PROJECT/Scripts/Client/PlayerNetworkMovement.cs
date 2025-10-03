using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class PlayerNetworkMovement : NetworkBehaviour
{
    private CharacterInput _inputMap;
    
    [Header("Player Data")]
    private NetworkVariable<float> _playerHealth = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<CustomPlayerData> _playerData = new NetworkVariable<CustomPlayerData>(new CustomPlayerData
    {
        playerID = 5,
        isdead = false
    });

    private struct CustomPlayerData
    {
      public int playerID;
      public bool isdead;
    }
    
    [Header("Movement")]
    private Rigidbody _characterRb;
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

    public override void OnNetworkSpawn()
    {
        _playerHealth.Value = Random.Range(0, 100);
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        _characterRb.transform.Translate(_movementVector * (Time.deltaTime * _actualSpeed), Space.World);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        _movementVector = new Vector3(context.ReadValue<Vector2>().x, 0, context.ReadValue<Vector2>().y);
    }

    private void ResetMovement(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        _movementVector = Vector3.zero;
        _actualSpeed = moveSpeedMultiplier;
    }
    private void OnDash(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        _actualSpeed = moveSpeedMultiplier * dashSpeedMultiplier;
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        var groundArray = Physics.OverlapSphere(groundCheckTransform.position, groundCheckRadius, groundLayer);
        if (groundArray.Length == 0) return;
        var jumpVector = new Vector3(0, jumpMultiplier, 0);
        _characterRb.AddForce(jumpVector, ForceMode.Impulse);
    }
    private void OnPause(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Debug.Log($"{OwnerClientId}'s health is at {_playerHealth.Value}");
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}
