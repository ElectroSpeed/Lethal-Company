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

    [Header("Component References")]
    [SerializeField] private Transform _cameraTransform;
    private Rigidbody _rb;
    private PlayerInput _playerInput;

    [Header("Input State")]
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpPressed;

    [Header("Runtime State")]
    private float _cameraPitch = 0f;
    private bool _isGrounded;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (TryGetComponent(out Rigidbody rb))
        {
            _rb = rb;
            _rb.freezeRotation = true;
        }

        _playerInput = GetComponent<PlayerInput>();

        _baseMoveSpeed = _moveSpeed;
        _currentSpeed = _moveSpeed;

        // Désactiver les entrées et la caméra pour les joueurs distants
        if (!IsOwner)
        {
            if (_cameraTransform != null)
                _cameraTransform.gameObject.SetActive(false);

            if (_playerInput != null)
                _playerInput.enabled = false;

            return;
        }

        // Initialisation pour le joueur local uniquement
        if (_cameraTransform != null)
        {
            _cameraTransform.gameObject.SetActive(true);
            _cameraTransform.tag = "MainCamera";
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"Spawned player {OwnerClientId}, IsOwner={IsOwner}");
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
        if (!IsOwner) return;
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        _lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (context.performed)
            _jumpPressed = true;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.started)
            _isSprinting = true;

        if (context.canceled)
            _isSprinting = false;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        // Exemple : _playerComponent.GetNearestInteractibleObject()?.Interact();
    }

    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        // Exemple : _playerComponent.GetEquippedItem()?.Use();
    }
    #endregion
}
