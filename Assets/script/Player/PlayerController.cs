using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f; //Get Move Speed of PlayerStats
    [SerializeField] private float _jumpForce = 5f; //same 
    [SerializeField] private float _lookSensitivity = 1.5f;

    [Header("Sprint Settings")] //Mettre tout ça dans le playerStats
    [SerializeField] private float _sprintSpeedMultiplier = 1.8f;
    [SerializeField] private float _sprintTransitionSpeed = 10f;
    private bool _isSprinting = false;
    private float _baseMoveSpeed;
    private float _currentSpeed;



    [Header("Component References")]
    [SerializeField] private Transform _cameraTransform;
    private Rigidbody _rb;
    private Player _playerComponent;

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
        if (IsOwner)
        {
            if (TryGetComponent(out Rigidbody rb))
            {
                _rb = rb;
                _rb.freezeRotation = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;


            _baseMoveSpeed = _moveSpeed;
            _currentSpeed = _moveSpeed;
        }
        if (!IsOwner)
        {
            _cameraTransform.gameObject.SetActive(false);
            GetComponent<PlayerInput>().enabled = false;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void LateUpdate()
    {
        RotatePlayerAndCamera();
    }

    private void MovePlayer()
    {
        // Détermine la vitesse actuelle (transition douce)
        float targetSpeed = _isSprinting && _isGrounded
            ? _baseMoveSpeed * _sprintSpeedMultiplier
            : _baseMoveSpeed;

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.fixedDeltaTime * _sprintTransitionSpeed);

        // Direction du mouvement
        Vector3 moveDir = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        Vector3 targetVelocity = new Vector3(moveDir.x * _currentSpeed, _rb.linearVelocity.y, moveDir.z * _currentSpeed);
        _rb.linearVelocity = targetVelocity;

        // Saut
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

    #region Input Callbacks

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            _jumpPressed = true;
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
            _isSprinting = true;

        if (context.canceled)
            _isSprinting = false;
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        //_playerComponent.GetNeareastInteractibleObject().interact();
    }
    public void OnUseItem(InputAction.CallbackContext context)
    {
        //_playerComponent.GetEquipiedItem().Use();
    }
    #endregion

    private void OnCollisionStay(Collision collision)
    {
        _isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        _isGrounded = false;
    }
}
