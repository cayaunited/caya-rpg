using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
  [Header("Movement Variables")]
  [SerializeField] private float _walkSpeed = 1;
  [SerializeField] private float _sprintSpeed = 2;
  [SerializeField] private float _sprintDuration = 1;
  
  [Header("Object References")]
  [SerializeField] private PlayerInput _input = null;
  [SerializeField] private Rigidbody2D _rigidbody = null;
  [SerializeField] private Transform _cursorAnchor = null;
  [SerializeField] private Camera _camera = null;
  
  private bool _moving = false;
  private Vector2 _moveDirection = new();
  private Vector2 _aimDirection = new(1, 0);
  private bool _sprinting = false;
  private bool _shouldSprint = false;
  private float _stamina = 0;
  private bool _canSprint = true;
  
  private void Start() {
    // The character should be able to immediately sprint
    _stamina = _sprintSpeed;
  }
  
  private void Update() {
    // If moving and using the mouse to aim, update the aim
    if (_moving && _input.currentControlScheme == "Keyboard & Mouse") {
      UpdateMouseAim();
      UpdateAimCursor();
    }
    
    if (_sprinting) {
      // Decrease stamina over time
      _stamina = Mathf.Clamp(_stamina - Time.deltaTime, 0, _sprintDuration);
      
      // Once stamina runs out, the player must wait
      // for a full recharge before sprinting again
      if (Mathf.Approximately(_stamina, 0)) {
        _sprinting = false;
        _canSprint = false;
        // If moving, update the velocity to walking speed
        if (_moving) _rigidbody.linearVelocity = _walkSpeed * _moveDirection;
      }
    } else {
      // Recharge stamina over time
      _stamina = Mathf.Clamp(_stamina + Time.deltaTime, 0, _sprintDuration);
      
      // Allow for sprinting once at full stamina
      if (Mathf.Approximately(_stamina, _sprintDuration)) {
        _canSprint = true;
        
        // Switch to sprinting if trying to
        if (_shouldSprint && _moving) {
          _sprinting = true;
          _rigidbody.linearVelocity = _sprintSpeed * _moveDirection;
        }
      }
    }
  }
  
  public void OnMove(InputAction.CallbackContext context) {
    // Read the move direction and determine if the player is moving
    _moveDirection = context.ReadValue<Vector2>();
    _moving = !Mathf.Approximately(_moveDirection.sqrMagnitude, 0);
    
    // Update the velocity based on the speed, either sprint or walk speed
    _rigidbody.linearVelocity = _moving
      ? (_sprinting ? _sprintSpeed : _walkSpeed) * _moveDirection
      : new();
    
    // If no longer moving, stop sprinting
    if (!_moving && _sprinting) _sprinting = false;
  }
  
  public void OnAim(InputAction.CallbackContext context) {
    // Read the input direction and exit if there is no input
    var inputDirection = context.ReadValue<Vector2>();
    if (Mathf.Approximately(inputDirection.sqrMagnitude, 0)) return;
    
    // Read the aim direction, either the vector between
    // the mouse and player or the right joystick vector
    if (_input.currentControlScheme == "Keyboard & Mouse") UpdateMouseAim();
    else _aimDirection = inputDirection;
    
    UpdateAimCursor();
  }
  
  public void OnSprint(InputAction.CallbackContext context) {
    // Track if the player is wanting to sprint
    if (context.started) _shouldSprint = true;
    
    // If the button was pressed and the player is moving
    // the player isn't waiting for a full stamina recharge,
    // and there is stamina left, then start sprinting
    if (context.started && _moving && _canSprint && _stamina > 0) {
      _sprinting = true;
      // If moving, update the velocity to sprinting speed
      _rigidbody.linearVelocity = _sprintSpeed * _moveDirection;
    }
    // If the button was released, then stop sprinting
    else if (context.canceled) {
      _shouldSprint = false;
      _sprinting = false;
      // If moving, update the velocity to walking speed
      if (_moving) _rigidbody.linearVelocity = _walkSpeed * _moveDirection;
    }
  }
  
  private void UpdateMouseAim() {
    // Find the mouse's position in the world
    var mousePosition = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    mousePosition.z = 0;
    // Get the difference between the mouse and player position,
    // then normalize that and use it for the aim direction
    _aimDirection = (mousePosition - transform.position).normalized;
  }
  
  private void UpdateAimCursor() {
    // Update the cursor based on the aim direction
    _cursorAnchor.localEulerAngles = new Vector3(0, 0,
      Mathf.Rad2Deg * Mathf.Atan2(_aimDirection.y, _aimDirection.x));
  }
}
