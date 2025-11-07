using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _lookSensitivity = 1.5f;

    [Header("Sprint Settings")]
    [SerializeField] private float _sprintSpeedMultiplier = 1.8f;
    [SerializeField] private float _sprintTransitionSpeed = 10f;
    private bool _isSprinting = false;
    private float _baseMoveSpeed;
    private float _currentSpeed;

    [Header("Interaction Data")]
    [SerializeField] private float _interactionRange;
    [SerializeField] private LayerMask _interactibleMask;
    private IInteractible _currentInteractible;

    [Header("Component References")]
    [SerializeField] private Transform _cameraTransform;
    private Rigidbody _rb;
    private PlayerInput _playerInput;
    private Player _player;

    [Header("Input State")]
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpPressed;

    [Header("Runtime State")]
    private float _cameraPitch = 0f;
    private bool _isGrounded;


    [Header("Block Player Input")]
    public bool _blockInput { get; private set; } = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (TryGetComponent(out Rigidbody rb))
        {
            _rb = rb;
            _rb.freezeRotation = true;
        }

        _playerInput = GetComponent<PlayerInput>();
        _player = GetComponent<Player>();

        _baseMoveSpeed = _moveSpeed;
        _currentSpeed = _moveSpeed;

        if (!IsOwner)
        {
            if (_cameraTransform != null)
                _cameraTransform.gameObject.SetActive(false);

            if (_playerInput != null)
                _playerInput.enabled = false;

            return;
        }

        if (_cameraTransform != null)
        {
            _cameraTransform.gameObject.SetActive(true);
            _cameraTransform.tag = "MainCamera";
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !Application.isFocused) return;

        CheckGrounded();
        MovePlayer();
    }

    private void LateUpdate()
    {
        if (!IsOwner || !Application.isFocused) return;

        RotatePlayerAndCamera();
    }

    public void BlockPlayerInput(bool value)
    {
        _blockInput = value;
        _rb.constraints = value ? RigidbodyConstraints.FreezePosition : RigidbodyConstraints.FreezeRotation;
    }

    private void MovePlayer()
    {
        float targetSpeed = _isSprinting && _isGrounded
            ? _baseMoveSpeed * _sprintSpeedMultiplier
            : _baseMoveSpeed;

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.fixedDeltaTime * _sprintTransitionSpeed);

        Vector3 moveDir = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        Vector3 targetVelocity = new Vector3(moveDir.x * _currentSpeed, _rb.linearVelocity.y, moveDir.z * _currentSpeed);
        _rb.linearVelocity = targetVelocity;

        if (_jumpPressed && _isGrounded)
        {
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _jumpPressed = false;
        }
    }

    private void RotatePlayerAndCamera()
    {
        float yaw = _lookInput.x * _lookSensitivity;
        transform.Rotate(Vector3.up * yaw);

        _cameraPitch -= _lookInput.y * _lookSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -80f, 80f);
        _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    private void CheckGrounded()
    {
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    #region Input Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner || _blockInput) return;
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!IsOwner || _blockInput) return;
        _lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner || _blockInput) return;
        if (context.performed)
            _jumpPressed = true;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!IsOwner || _blockInput) return;

        if (context.started)
            _isSprinting = true;

        if (context.canceled)
            _isSprinting = false;
    }


    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!IsOwner || _blockInput) return;
        if (!context.started) return;
        if (_currentInteractible == null) return;

        NetworkObject netObj = ((MonoBehaviour)_currentInteractible).GetComponent<NetworkObject>();
        if (netObj != null)
        {
            InteractServerRpc(netObj);
        }
    }

    [ServerRpc]
    private void InteractServerRpc(NetworkObjectReference interactibleRef)
    {
        if (interactibleRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out IInteractible interactible))
            {
                interactible.Interact(_player);
            }
        }
    }
    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (context.started)
        {
            if (_player._equipiedItem is ConsumableItem consumableItem)
            {
                consumableItem.Use();
            }
        }
    }
    #endregion


    #region Trigger Detection

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (other.TryGetComponent(out IInteractible interactible))
        {
            _currentInteractible = interactible;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;
        if (other.TryGetComponent(out IInteractible interactible))
        {
            if (_currentInteractible == interactible)
                _currentInteractible = null;
        }
    }

    #endregion
}
