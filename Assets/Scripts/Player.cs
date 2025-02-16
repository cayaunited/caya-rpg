using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
  [Header("Movement Variables")]
  [SerializeField] private float _walkSpeed = 1;
  [SerializeField] private float _sprintSpeed = 2;
  [SerializeField] private float _sprintDuration = 1;
  [SerializeField] private float _dashSpeed = 3;
  [SerializeField] private float _dashDuration = 1;
  [SerializeField] private float _dashCooldown = 1;
  
  [Header("Character Variables")]
  [SerializeField] private PlayerRole _role = PlayerRole.Decoy;
  
  [Header("Object References")]
  [SerializeField] private PlayerInput _input = null;
  [SerializeField] private Rigidbody2D _rigidbody = null;
  [SerializeField] private Transform _cursorAnchor = null;
  [SerializeField] private Camera _camera = null;
  
  private bool _moving = false;
  private Vector2 _moveDirection = new();
  private Vector2 _lastMoveDirection = new(1, 0);
  private Vector2 _aimDirection = new(1, 0);
  private bool _sprinting = false;
  private bool _shouldSprint = false;
  private float _stamina = 0;
  private bool _canSprint = true;
  private bool _dashing = false;
  private float _dashTimer = 0;
  private float _dashCooldownTimer = 0;
  
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
    
    if (_sprinting && !_dashing) {
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
    } else if (!_dashing) {
      // Recharge stamina over time
      _stamina = Mathf.Clamp(_stamina + Time.deltaTime, 0, _sprintDuration);
      
      // Allow for sprinting once at full stamina
      if (Mathf.Approximately(_stamina, _sprintDuration)) {
        _canSprint = true;
        // Switch to sprinting if trying to
        TryToSprint();
      }
    }
    
    if (_dashing) {
      // Decrease dash timer over time
      _dashTimer = Mathf.Clamp(_dashTimer - Time.deltaTime, 0, _dashDuration);
      
      // Start the cooldown if done dashing and reset the velocity
      if (Mathf.Approximately(_dashTimer, 0)) {
        _dashing = false;
        _dashCooldownTimer = _dashCooldown;
        _rigidbody.linearVelocity = _moving ? _walkSpeed * _moveDirection : new();
        // Switch to sprinting if trying to
        TryToSprint();
      }
    } else if (!Mathf.Approximately(_dashCooldownTimer, 0)) {
      // Decrease the dash cooldown timer over time
      _dashCooldownTimer = Mathf.Clamp(_dashCooldownTimer - Time.deltaTime, 0, _dashCooldown);
    }
  }
  
  public void OnMove(InputAction.CallbackContext context) {
    // Read the move direction and determine if the player is moving
    _moveDirection = context.ReadValue<Vector2>();
    _moving = !Mathf.Approximately(_moveDirection.sqrMagnitude, 0);
    // If moving, set the last move direction
    if (_moving) _lastMoveDirection = _moveDirection;
    
    // If dashing, ignore the rest
    if (_dashing) return;
    
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
    // the player isn't dashing, and there is stamina left, then start sprinting
    if (context.started && _moving && _canSprint && !_dashing && _stamina > 0) {
      _sprinting = true;
      // If moving, update the velocity to sprinting speed
      _rigidbody.linearVelocity = _sprintSpeed * _moveDirection;
    }
    // If the button was released, then stop sprinting
    else if (context.canceled) {
      _shouldSprint = false;
      _sprinting = false;
      // If moving and not dashing, update the velocity to walking speed
      if (_moving && !_dashing) _rigidbody.linearVelocity = _walkSpeed * _moveDirection;
    }
  }
  
  public void OnDash(InputAction.CallbackContext context) {
    // A dash can only happen when the dash button is pressed,
    // if the role allows for dashing, if the dash timer is not running,
    // and if the dash cooldown is done
    if (!context.started
      || (_role != PlayerRole.Decoy && _role != PlayerRole.Support)
      || !Mathf.Approximately(_dashTimer, 0)
      || !Mathf.Approximately(_dashCooldownTimer, 0)) return;
    
    // Start dashing and start the timer
    _dashing = true;
    _dashTimer = _dashDuration;
    // Dash in the last move direction
    _rigidbody.linearVelocity = _dashSpeed * _lastMoveDirection;
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
  
  private void TryToSprint() {
    // If trying to sprint and moving, start sprinting
    if (_shouldSprint && _moving) {
      _sprinting = true;
      _rigidbody.linearVelocity = _sprintSpeed * _moveDirection;
    }
  }
}
