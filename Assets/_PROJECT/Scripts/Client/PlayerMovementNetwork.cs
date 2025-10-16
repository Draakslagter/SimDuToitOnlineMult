using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementNetwork : NetworkBehaviour
{
    private CharacterInput _inputMap;
    
    [SerializeField] private GameObject spawnedItemPrefab;
        
    [Header("Player Data")]
    private NetworkVariable<PlayerCustomData> _playerData = new NetworkVariable<PlayerCustomData>(new PlayerCustomData
    {
        _int = 5,
        _bool = true,
        _color = Color.red
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public struct PlayerCustomData: INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public Color _color;
        public FixedString128Bytes _message;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref _color);
            serializer.SerializeValue(ref _message);
        }
    }
    
    [Header("Movement")]
    private Rigidbody _characterRb;
    private Vector3 _movementVector;
    [SerializeField] private float moveSpeedMultiplier = 5f;
    private float _actualSpeed;
    
    [Header("Jump")]
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.5f, jumpMultiplier = 5f;
    
    [Header("Dash")]
    private Vector3 _dashVector;
    [SerializeField] private float dashSpeedMultiplier = 1.5f;
    private int _dashToken;
    [SerializeField] private int maxDashToken = 1;

    [Header("Colour")]
    [SerializeField] private Color playerColour;
    private Renderer _playerRenderer;
    
    private void Awake()
    {
        _inputMap = new CharacterInput();
        _inputMap.Enable();
        _inputMap.PlayerMap.Pause.performed += OnPause;
        _inputMap.PlayerMap.Jump.performed += OnJump;
        _inputMap.PlayerMap.RunDash.performed += OnDash;
        _inputMap.PlayerMap.Movement.performed += OnMove;
        _inputMap.PlayerMap.Movement.canceled += ResetMovement;
        _inputMap.PlayerMap.Interact.performed += OnInteract;

        if (_characterRb == null)
        {
            _characterRb = GetComponent<Rigidbody>();
        }

        if (_playerRenderer == null)
        {
            _playerRenderer = GetComponent<Renderer>();
        }
    }

    private void OnDisable()
    {
        _inputMap.PlayerMap.Pause.performed -= OnPause;
        _inputMap.PlayerMap.Jump.performed -= OnJump;
        _inputMap.PlayerMap.RunDash.performed -= OnDash;
        _inputMap.PlayerMap.Movement.performed -= OnMove;
        _inputMap.PlayerMap.Movement.canceled -= ResetMovement;
        _inputMap.PlayerMap.Interact.performed -= OnInteract;
        _inputMap.Disable();
    }

    private void Start()
    {
        _actualSpeed = moveSpeedMultiplier;
        _dashToken = maxDashToken;
        _playerRenderer.material.color = playerColour;
    }

    public override void OnNetworkSpawn()
    {
        _playerData.OnValueChanged += (PlayerCustomData previousValue, PlayerCustomData newValue) =>
        {
            Debug.Log($"{OwnerClientId}; player health: {newValue._int}");
        };
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        _characterRb.transform.Translate(_movementVector * (Time.deltaTime * _actualSpeed), Space.World);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        _movementVector = new Vector3(context.ReadValue<Vector2>().x, 0, 0);
        _dashVector = new Vector3(context.ReadValue<Vector2>().x, context.ReadValue<Vector2>().y, 0);
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
        var groundArray = Physics.OverlapSphere(groundCheckTransform.position, groundCheckRadius, groundLayer);
        if (groundArray.Length == 0)
        {
            if (_dashToken == 0)
            {
                return;
            }
            _dashToken--;
        }
        else
        {
            _dashToken = maxDashToken;
        }
        _actualSpeed = moveSpeedMultiplier * dashSpeedMultiplier;
        _characterRb.AddForce(_dashVector*_actualSpeed, ForceMode.Impulse);
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Debug.Log($"Dash Token: {_dashToken}");
        var groundArray = Physics.OverlapSphere(groundCheckTransform.position, groundCheckRadius, groundLayer);
        if (groundArray.Length == 0) return;
        _dashToken = maxDashToken;
        var jumpVector = new Vector3(0, jumpMultiplier, 0);
        _characterRb.AddForce(jumpVector, ForceMode.Impulse);
    }
    private void OnPause(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        GameObject tempHolder = Instantiate(spawnedItemPrefab);
        tempHolder.GetComponent<NetworkObject>().Spawn(true);

    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.GetComponent<IColourable>() == null) return;
        var colourable = other.gameObject.GetComponent<IColourable>();
        colourable.ChangeCubeColourClientRpc(playerColour);
    }

    [ServerRpc]
    private void TestServerRpc()
    {
        Debug.Log($"Testing ServerRPC: {OwnerClientId}");
    }

    [ClientRpc]
    private void TestClientRpc()
    {
        Debug.Log($"Testing ClientRPC: {OwnerClientId}");
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}
