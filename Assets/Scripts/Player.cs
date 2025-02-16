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
  [SerializeField] private float _teleportDistance = 2;
  [SerializeField] private float _teleportCooldown = 1;
  [SerializeField] private float _teleportMinCharge = 1;
  [SerializeField] private float _teleportMaxCharge = 2;
  [SerializeField] private LayerMask _thickWallLayer = 0;
  [SerializeField] private float _bounceSpeed = 3;
  [SerializeField] private float _bounceDuration = 1;
  [SerializeField] private float _bounceCooldown = 1;
  [SerializeField] private float _bounceMinCharge = 1;
  [SerializeField] private float _bounceMaxCharge = 2;
  [SerializeField] private float _grappleRange = 2;
  [SerializeField] private float _grappleSpeed = 3;
  [SerializeField] private float _grappleDuration = 1;
  [SerializeField] private float _grappleCooldown = 1;
  [SerializeField] private LayerMask _grappleWallLayer = 0;
  
  [Header("Character Variables")]
  [SerializeField] private PlayerRole _role = PlayerRole.Decoy;
  
  [Header("Object References")]
  [SerializeField] private PlayerInput _input = null;
  [SerializeField] private Rigidbody2D _rigidbody = null;
  [SerializeField] private Transform _cursorAnchor = null;
  [SerializeField] private Camera _camera = null;
  [SerializeField] private PhysicsMaterial2D _normalPhysics = null;
  [SerializeField] private PhysicsMaterial2D _bouncyPhysics = null;
  [SerializeField] private CapsuleCollider2D _collider = null;
  [SerializeField] private SpriteRenderer _hookRenderer = null;
  
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
  private bool _chargingTeleport = false;
  private float _teleportCharge = 0;
  private float _teleportCooldownTimer = 0;
  private bool _bouncing = false;
  private float _bounceTimer = 0;
  private float _bounceCooldownTimer = 0;
  private bool _chargingBounce = false;
  private float _bounceCharge = 0;
  private bool _grappling = false;
  private float _grappleTimer = 0;
  private float _grappleCooldownTimer = 0;
  private Vector3 _grapplePoint = new();
  
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
    
    if (_sprinting && !_dashing && !_bouncing && !_grappling) {
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
    } else if (!_dashing && !_bouncing && !_grappling) {
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
    
    // Charge up the teleport over time if trying to
    if (_chargingTeleport && !Mathf.Approximately(_teleportCharge, _teleportMaxCharge)) {
      _teleportCharge = Mathf.Clamp(_teleportCharge + Time.deltaTime, _teleportMinCharge, _teleportMaxCharge);
    }
    
    // Decrease the teleport cooldown timer over time
    if (!Mathf.Approximately(_teleportCooldownTimer, 0)) {
      _teleportCooldownTimer = Mathf.Clamp(_teleportCooldownTimer - Time.deltaTime, 0, _teleportCooldown);
    }
    
    if (_bouncing) {
      // Decrease bounce timer over time
      _bounceTimer = Mathf.Clamp(_bounceTimer - Time.deltaTime, 0, _bounceDuration);
      // Make sure to keep the bouncing at the same speed
      _rigidbody.linearVelocity = _bounceSpeed * _rigidbody.linearVelocity.normalized;
      
      // Start the cooldown if done bouncing and reset the velocity
      if (Mathf.Approximately(_bounceTimer, 0)) {
        _bouncing = false;
        _bounceCooldownTimer = _bounceCooldown;
        _rigidbody.sharedMaterial = _normalPhysics;
        _rigidbody.linearVelocity = _moving ? _walkSpeed * _moveDirection : new();
        // Switch to sprinting if trying to
        TryToSprint();
      }
    } else if (!Mathf.Approximately(_bounceCooldownTimer, 0)) {
      // Decrease the bounce cooldown timer over time
      _bounceCooldownTimer = Mathf.Clamp(_bounceCooldownTimer - Time.deltaTime, 0, _bounceCooldown);
    }
    
    // Charge up the bounce over time if trying to
    if (_chargingBounce && !Mathf.Approximately(_bounceCharge, _bounceMaxCharge)) {
      _bounceCharge = Mathf.Clamp(_bounceCharge + Time.deltaTime, _bounceMinCharge, _bounceMaxCharge);
    }
    
    if (_grappling) {
      // Decrease grapple timer over time
      _grappleTimer = Mathf.Clamp(_grappleTimer - Time.deltaTime, 0, _grappleDuration);
      // Update the grappling hook visual
      UpdateHook();
      
      // Start the cooldown if done grappling and reset the velocity
      if (Mathf.Approximately(_grappleTimer, 0)) StopGrappling();
    } else if (!Mathf.Approximately(_grappleCooldownTimer, 0)) {
      // Decrease the grapple cooldown timer over time
      _grappleCooldownTimer = Mathf.Clamp(_grappleCooldownTimer - Time.deltaTime, 0, _grappleCooldown);
    }
  }
  
  private void OnCollisionEnter2D(Collision2D collision)
  {
    // If grappling and hit a wall, stop grappling
    if (_grappling && _collider.IsTouchingLayers(_grappleWallLayer)) StopGrappling();
  }
  
  public void OnMove(InputAction.CallbackContext context) {
    // Read the move direction and determine if the player is moving
    _moveDirection = context.ReadValue<Vector2>();
    _moving = !Mathf.Approximately(_moveDirection.sqrMagnitude, 0);
    // If moving, set the last move direction
    if (_moving) _lastMoveDirection = _moveDirection;
    
    // If dashing, bouncing, or grappling, ignore the rest
    if (_dashing || _bouncing || _grappling) return;
    
    // Update the velocity based on the speed, either sprint or walk speed
    _rigidbody.linearVelocity = _moving
      ? (_sprinting ? _sprintSpeed : _walkSpeed) * _moveDirection
      : new();
    
    // If no longer moving, stop sprinting
    if (!_moving && _sprinting) _sprinting = false;
  }
  
  public void OnAim(InputAction.CallbackContext context) {
    // Read the input direction and exit if there is no input
    var inputDirection = context.ReadValue<Vector2>().normalized;
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
      || _chargingTeleport || _chargingBounce || _bouncing || _grappling
      || !Mathf.Approximately(_dashTimer, 0)
      || !Mathf.Approximately(_dashCooldownTimer, 0)) return;
    
    // Start dashing and start the timer
    _dashing = true;
    _dashTimer = _dashDuration;
    // Dash in the last move direction
    _rigidbody.linearVelocity = _dashSpeed * _lastMoveDirection;
  }
  
  public void OnTeleport(InputAction.CallbackContext context) {
    // A teleport can only happen if the role allows for teleporting,
    // if the teleport cooldown is done, and the player isn't dashing
    if ((_role != PlayerRole.Observer && _role != PlayerRole.Support)
      || _dashing || _chargingBounce || _bouncing || _grappling
      || !Mathf.Approximately(_teleportCooldownTimer, 0)) return;
    
    // Start charging
    if (context.started) {
      _chargingTeleport = true;
      _teleportCharge = _teleportMinCharge;
    } else if (context.canceled && _chargingTeleport) {
      // Calculate the new position based on the charge level
      Vector3 newPosition = transform.position + _teleportDistance
        * (_teleportCharge / _teleportMaxCharge) * (Vector3) _lastMoveDirection;
      
      // Check if there is a wall in the way, and exit if so
      if (Physics2D.OverlapBox(newPosition, _collider.size, 0, _thickWallLayer)) return;
      
      // Teleport and start the cooldown
      _teleportCooldownTimer = _teleportCooldown;
      _chargingTeleport = false;
      // Teleport in the last move direction
      transform.position = newPosition;
      _teleportCharge = _teleportMinCharge;
    }
  }
  
  public void OnBounce(InputAction.CallbackContext context) {
    // A bounce can only happen when the bounce button is pressed,
    // if the role allows for bouncing, if the bounce timer is not running,
    // and if the bounce cooldown is done
    if ((_role != PlayerRole.Decoy && _role != PlayerRole.Observer)
      || _dashing || _chargingTeleport || _grappling
      || !Mathf.Approximately(_bounceTimer, 0)
      || !Mathf.Approximately(_bounceCooldownTimer, 0)) return;
    
    // Start charging
    if (context.started) {
      _chargingBounce = true;
      _bounceCharge = _bounceMinCharge;
    } else if (context.canceled && _chargingBounce) {
      // Start bouncing
      _chargingBounce = false;
      _bouncing = true;
      _bounceTimer = _bounceDuration * (_bounceCharge / _bounceMaxCharge);
      _rigidbody.sharedMaterial = _bouncyPhysics;
      _rigidbody.linearVelocity = _bounceSpeed * _lastMoveDirection;
      _bounceCharge = _bounceMinCharge;
    }
  }
  
  public void OnGrapple(InputAction.CallbackContext context) {
    // A grapple can only happen when the grapple button is released,
    // if the role allows for grappling, if the grapple timer is not running,
    // and if the grapple cooldown is done
    if (!context.canceled
      || (_role != PlayerRole.Decoy && _role != PlayerRole.Observer)
      || _dashing || _chargingTeleport || _chargingBounce || _bouncing
      || !Mathf.Approximately(_grappleTimer, 0)
      || !Mathf.Approximately(_grappleCooldownTimer, 0)) return;
    
    // Try grappling in the aim direction and exit if there is no wall in range
    var wallHit = Physics2D.Raycast(transform.position, _aimDirection, _grappleRange, _grappleWallLayer);
    if (!wallHit) return;
    // Start grappling and start the timer
    _grappling = true;
    _grappleTimer = _grappleDuration;
    // Grapple in the aim direction
    _rigidbody.linearVelocity = _grappleSpeed * _aimDirection;
    // Connect the grappling hook to the wall
    _grapplePoint = wallHit.point;
    _hookRenderer.gameObject.SetActive(true);
    UpdateHook();
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
    _cursorAnchor.localEulerAngles = new(0, 0,
      Mathf.Rad2Deg * Mathf.Atan2(_aimDirection.y, _aimDirection.x));
  }
  
  private void TryToSprint() {
    // If trying to sprint and moving, start sprinting
    if (_shouldSprint && _moving) {
      _sprinting = true;
      _rigidbody.linearVelocity = _sprintSpeed * _moveDirection;
    }
  }
  
  private void UpdateHook() {
    // Calculate the angle and distance between the player and grapple point
    var difference = _grapplePoint - transform.position;
    float angle = Mathf.Rad2Deg * Mathf.Atan2(difference.y, difference.x);
    float distance = difference.magnitude;
    // Update the hook visual
    _hookRenderer.transform.localEulerAngles = new(0, 0, angle);
    _hookRenderer.size = new Vector2(distance, _hookRenderer.size.y);
  }
  
  private void StopGrappling() {
    // Start the cooldown if done grappling and reset the velocity
    _grappling = false;
    _grappleTimer = 0;
    _grappleCooldownTimer = _grappleCooldown;
    _rigidbody.linearVelocity = _moving ? _walkSpeed * _moveDirection : new();
    _hookRenderer.gameObject.SetActive(false);
    // Switch to sprinting if trying to
    TryToSprint();
  }
}
