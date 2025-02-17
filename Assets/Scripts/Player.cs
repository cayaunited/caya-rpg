using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Hurtable {
  [Header("Movement Variables")]
  [SerializeField] private LayerMask _wallLayer = 0;
  [SerializeField] private Vector2 _linkSearchSize = new(1, 1);
  [SerializeField] private LayerMask _playerLayer = 0;
  
  [Header("Object References")]
  [SerializeField] private PlayerData _data = null;
  [SerializeField] private PlayerInput _input = null;
  [SerializeField] private Rigidbody2D _rigidbody = null;
  [SerializeField] private Transform _cursorAnchor = null;
  [SerializeField] private Camera _camera = null;
  [SerializeField] private PhysicsMaterial2D _normalPhysics = null;
  [SerializeField] private PhysicsMaterial2D _bouncyPhysics = null;
  [SerializeField] private CapsuleCollider2D _collider = null;
  [SerializeField] private SpriteRenderer _hookRenderer = null;
  [SerializeField] private SpriteRenderer _linkRenderer = null;
  
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
  private bool _steadying = false;
  private float _steadyTimer = 0;
  private float _steadyCooldownTimer = 0;
  private bool _linking = false;
  private Player _linkedPlayer = null;
  private bool _recoveringFromFall = false;
  private float _fallRecoverTimer = 0;
  
  private int _mobility = 0;
  private int _intelligence = 0;
  private int _strength = 0;
  private int _steadfastness = 0;
  
  private void Start() {
    // Initialize the player stats
    _mobility = _data.BaseMobility;
    _intelligence = _data.BaseIntelligence;
    _strength = _data.BaseStrength;
    _steadfastness = _data.BaseSteadfastness;
    // The character should be able to immediately sprint
    _stamina = _data.SprintSpeed[_mobility];
    // Start off with max health, and make sure to calculate the max health
    Health = MaxHealth = _data.MaxHealth[_steadfastness];
  }
  
  private void Update() {
    // Don't update if dead
    if (!Alive) return;
    
    // If moving and using the mouse to aim, update the aim
    if (_moving && _input.currentControlScheme == "Keyboard & Mouse") {
      UpdateMouseAim();
      UpdateAimCursor();
    }
    
    if (_sprinting && !_dashing && !_bouncing && !_grappling) {
      // Decrease stamina over time
      _stamina = Mathf.Clamp(_stamina - Time.deltaTime, 0, _data.SprintDuration[_mobility]);
      
      // Once stamina runs out, the player must wait
      // for a full recharge before sprinting again
      if (Mathf.Approximately(_stamina, 0)) {
        _sprinting = false;
        _canSprint = false;
        // If moving, update the velocity to walking speed
        if (_moving) _rigidbody.linearVelocity = _data.WalkSpeed[_mobility] * _moveDirection;
      }
    } else if (!_dashing && !_bouncing && !_grappling) {
      // Recharge stamina over time
      _stamina = Mathf.Clamp(_stamina + Time.deltaTime, 0, _data.SprintDuration[_mobility]);
      
      // Allow for sprinting once at full stamina
      if (Mathf.Approximately(_stamina, _data.SprintDuration[_mobility])) {
        _canSprint = true;
        // Switch to sprinting if trying to
        TryToSprint();
      }
    }
    
    if (_dashing) {
      // Decrease dash timer over time
      _dashTimer = Mathf.Clamp(_dashTimer - Time.deltaTime, 0, _data.DashDuration[_mobility]);
      
      // Start the cooldown if done dashing and reset the velocity
      if (Mathf.Approximately(_dashTimer, 0)) {
        _dashing = false;
        _dashCooldownTimer = _data.DashCooldown[_mobility];
        _rigidbody.linearVelocity = _moving ? _data.WalkSpeed[_mobility] * _moveDirection : new();
        // Switch to sprinting if trying to
        TryToSprint();
      }
    } else if (!Mathf.Approximately(_dashCooldownTimer, 0)) {
      // Decrease the dash cooldown timer over time
      _dashCooldownTimer = Mathf.Clamp(_dashCooldownTimer - Time.deltaTime, 0, _data.DashCooldown[_mobility]);
    }
    
    // Charge up the teleport over time if trying to
    if (_chargingTeleport && !Mathf.Approximately(_teleportCharge, _data.TeleportMaxCharge[_intelligence])) {
      _teleportCharge = Mathf.Clamp(_teleportCharge + Time.deltaTime, _data.TeleportMinCharge, _data.TeleportMaxCharge[_intelligence]);
    }
    
    // Decrease the teleport cooldown timer over time
    if (!Mathf.Approximately(_teleportCooldownTimer, 0)) {
      _teleportCooldownTimer = Mathf.Clamp(_teleportCooldownTimer - Time.deltaTime, 0, _data.TeleportCooldown[_intelligence]);
    }
    
    if (_bouncing) {
      // Decrease bounce timer over time
      _bounceTimer = Mathf.Clamp(_bounceTimer - Time.deltaTime, 0, _data.BounceDuration[_mobility]);
      // Make sure to keep the bouncing at the same speed
      _rigidbody.linearVelocity = _data.BounceSpeed[_mobility] * _rigidbody.linearVelocity.normalized;
      
      // Start the cooldown if done bouncing and reset the velocity
      if (Mathf.Approximately(_bounceTimer, 0)) {
        _bouncing = false;
        _bounceCooldownTimer = _data.BounceCooldown[_mobility];
        _rigidbody.sharedMaterial = _normalPhysics;
        _rigidbody.linearVelocity = _moving ? _data.WalkSpeed[_mobility] * _moveDirection : new();
        // Switch to sprinting if trying to
        TryToSprint();
      }
    } else if (!Mathf.Approximately(_bounceCooldownTimer, 0)) {
      // Decrease the bounce cooldown timer over time
      _bounceCooldownTimer = Mathf.Clamp(_bounceCooldownTimer - Time.deltaTime, 0, _data.BounceCooldown[_mobility]);
    }
    
    // Charge up the bounce over time if trying to
    if (_chargingBounce && !Mathf.Approximately(_bounceCharge, _data.BounceMaxCharge[_mobility])) {
      _bounceCharge = Mathf.Clamp(_bounceCharge + Time.deltaTime, _data.BounceMinCharge, _data.BounceMaxCharge[_mobility]);
    }
    
    if (_grappling) {
      // Decrease grapple timer over time
      _grappleTimer = Mathf.Clamp(_grappleTimer - Time.deltaTime, 0, _data.GrappleDuration[_strength]);
      // Update the grappling hook visual
      UpdateHook();
      
      // Start the cooldown if done grappling and reset the velocity
      if (Mathf.Approximately(_grappleTimer, 0)) StopGrappling();
    } else if (!Mathf.Approximately(_grappleCooldownTimer, 0)) {
      // Decrease the grapple cooldown timer over time
      _grappleCooldownTimer = Mathf.Clamp(_grappleCooldownTimer - Time.deltaTime, 0, _data.GrappleCooldown[_strength]);
    }
    
    if (_steadying) {
      // Decrease steady timer over time
      _steadyTimer = Mathf.Clamp(_steadyTimer - Time.deltaTime, 0, _data.SteadyDuration[_steadfastness]);
      // Start the cooldown if done steadying and reset the velocity
      if (Mathf.Approximately(_steadyTimer, 0)) StopSteadying();
    } else if (!Mathf.Approximately(_steadyCooldownTimer, 0)) {
      // Decrease the steady cooldown timer over time
      _steadyCooldownTimer = Mathf.Clamp(_steadyCooldownTimer - Time.deltaTime, 0, _data.SteadyCooldown[_steadfastness]);
    }
    
    if (_linking) {
      // Update visual and unlink if out of range
      var distanceSquared = (_linkedPlayer.transform.position - transform.position).sqrMagnitude;
      if (distanceSquared > _data.LinkRange * _data.LinkRange) StopLinking();
      else UpdateLink();
    }
    
    if (_recoveringFromFall) {
      // Decrease recovery timer over time
      _fallRecoverTimer = Mathf.Clamp(_fallRecoverTimer - Time.deltaTime, 0, _data.FallRecoverTime[_steadfastness]);
      
      // End recovery when done
      if (Mathf.Approximately(_fallRecoverTimer, 0)) {
        _recoveringFromFall = false;
        _rigidbody.linearVelocity = _moving ? _data.WalkSpeed[_mobility] * _moveDirection : new();
        TryToSprint();
      }
    }
  }
  
  private void OnCollisionEnter2D(Collision2D collision) {
    // If grappling and hit a wall, stop grappling
    if (_grappling && _collider.IsTouchingLayers(_wallLayer)) StopGrappling();
  }
  
  public void OnMove(InputAction.CallbackContext context) {
    // Exit if dead
    if (!Alive) return;
    
    // Read the move direction and determine if the player is moving
    _moveDirection = context.ReadValue<Vector2>();
    _moving = !Mathf.Approximately(_moveDirection.sqrMagnitude, 0);
    // If moving, set the last move direction
    if (_moving) _lastMoveDirection = _moveDirection;
    
    // If dashing, bouncing, grappling, or steadying, ignore the rest
    if (_dashing || _bouncing || _grappling || _steadying || _recoveringFromFall) return;
    
    // Update the velocity based on the speed, either sprint or walk speed
    _rigidbody.linearVelocity = _moving
      ? (_sprinting ? _data.SprintSpeed[_mobility] : _data.WalkSpeed[_mobility]) * _moveDirection
      : new();
    
    // If no longer moving, stop sprinting
    if (!_moving && _sprinting) _sprinting = false;
  }
  
  public void OnAim(InputAction.CallbackContext context) {
    // Exit if dead
    if (!Alive) return;
    
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
    // Exit if dead
    if (!Alive) return;
    
    // Track if the player is wanting to sprint
    if (context.started) _shouldSprint = true;
    
    // If the button was pressed and the player is moving
    // the player isn't waiting for a full stamina recharge,
    // the player isn't dashing, and there is stamina left, then start sprinting
    if (context.started && _moving && _canSprint
      && !_dashing && !_bouncing && !_grappling && !_steadying && !_recoveringFromFall && _stamina > 0) {
      _sprinting = true;
      _rigidbody.linearVelocity = _data.SprintSpeed[_mobility] * _moveDirection;
    }
    // If the button was released, then stop sprinting
    else if (context.canceled) {
      _shouldSprint = false;
      _sprinting = false;
      
      // If moving and not dashing, update the velocity to walking speed
      if (_moving && !_dashing && !_bouncing && !_grappling && !_steadying && !_recoveringFromFall)
        _rigidbody.linearVelocity = _data.WalkSpeed[_mobility] * _moveDirection;
    }
  }
  
  public void OnDash(InputAction.CallbackContext context) {
    // A dash can only happen when the dash button is pressed,
    // if the role allows for dashing, if the dash timer is not running,
    // and if the dash cooldown is done
    if (!Alive || !context.started
      || (_data.Role != PlayerRole.Decoy && _data.Role != PlayerRole.Support)
      || _chargingTeleport || _chargingBounce || _bouncing
      || _grappling || _recoveringFromFall
      || !Mathf.Approximately(_dashTimer, 0)
      || !Mathf.Approximately(_dashCooldownTimer, 0)) return;
    
    // Start dashing and start the timer
    _dashing = true;
    _dashTimer = _data.DashDuration[_mobility];
    // Dash in the last move direction
    _rigidbody.linearVelocity = _data.DashSpeed[_mobility] * _lastMoveDirection;
  }
  
  public void OnTeleport(InputAction.CallbackContext context) {
    // A teleport can only happen if the role allows for teleporting,
    // if the teleport cooldown is done, and the player isn't dashing
    if (!Alive || (_data.Role != PlayerRole.Observer && _data.Role != PlayerRole.Support)
      || _dashing || _chargingBounce || _bouncing
      || _grappling || _recoveringFromFall
      || !Mathf.Approximately(_teleportCooldownTimer, 0)) return;
    
    // Start charging
    if (context.started) {
      _chargingTeleport = true;
      _teleportCharge = _data.TeleportMinCharge;
    } else if (context.canceled && _chargingTeleport) {
      // Calculate the new position based on the charge level
      Vector3 newPosition = transform.position + _data.TeleportDistance[_intelligence]
        * (_teleportCharge / _data.TeleportMaxCharge[_intelligence]) * (Vector3) _lastMoveDirection;
      
      // Check if there is a wall in the way, and exit if so
      if (Physics2D.OverlapBox(newPosition, _collider.size, 0, _wallLayer)) return;
      
      // Teleport and start the cooldown
      _teleportCooldownTimer = _data.TeleportCooldown[_intelligence];
      _chargingTeleport = false;
      // Teleport in the last move direction
      transform.position = newPosition;
      _teleportCharge = _data.TeleportMinCharge;
    }
  }
  
  public void OnBounce(InputAction.CallbackContext context) {
    // A bounce can only happen when the bounce button is pressed,
    // if the role allows for bouncing, if the bounce timer is not running,
    // and if the bounce cooldown is done
    if (!Alive || (_data.Role != PlayerRole.Decoy && _data.Role != PlayerRole.Observer)
      || _dashing || _chargingTeleport || _grappling || _recoveringFromFall
      || !Mathf.Approximately(_bounceTimer, 0)
      || !Mathf.Approximately(_bounceCooldownTimer, 0)) return;
    
    // Start charging
    if (context.started) {
      _chargingBounce = true;
      _bounceCharge = _data.BounceMinCharge;
    } else if (context.canceled && _chargingBounce) {
      // Start bouncing
      _chargingBounce = false;
      _bouncing = true;
      _bounceTimer = _data.BounceDuration[_mobility] * (_bounceCharge / _data.BounceMaxCharge[_mobility]);
      _rigidbody.sharedMaterial = _bouncyPhysics;
      _rigidbody.linearVelocity = _data.BounceSpeed[_mobility] * _lastMoveDirection;
      _bounceCharge = _data.BounceMinCharge;
    }
  }
  
  public void OnGrapple(InputAction.CallbackContext context) {
    // A grapple can only happen when the grapple button is released,
    // if the role allows for grappling, if the grapple timer is not running,
    // and if the grapple cooldown is done
    if (!Alive || !context.canceled
      || (_data.Role != PlayerRole.Decoy && _data.Role != PlayerRole.Tank)
      || _dashing || _chargingTeleport || _chargingBounce
      || _bouncing || _recoveringFromFall
      || !Mathf.Approximately(_grappleTimer, 0)
      || !Mathf.Approximately(_grappleCooldownTimer, 0)) return;
    
    // Try grappling in the aim direction and exit if there is no wall in range
    var wallHit = Physics2D.Raycast(transform.position, _aimDirection,
      _data.GrappleRange[_strength], _wallLayer);
    if (!wallHit) return;
    // Start grappling and start the timer
    _grappling = true;
    _grappleTimer = _data.GrappleDuration[_strength];
    // Stop steadying if needed
    if (_steadying) StopSteadying();
    
    // Grapple in the aim direction
    _rigidbody.linearVelocity = _data.GrappleSpeed[_strength] * _aimDirection;
    // Connect the grappling hook to the wall
    _grapplePoint = wallHit.point;
    _hookRenderer.gameObject.SetActive(true);
    UpdateHook();
  }
  
  public void OnSteady(InputAction.CallbackContext context) {
    // A steady can only happen if the role allows for steadying,
    // and if the steady cooldown is done
    if (!Alive || !context.started
      || (_data.Role != PlayerRole.Observer && _data.Role != PlayerRole.Tank)
      || _dashing || _chargingTeleport || _chargingBounce
      || _bouncing || _grappling || _recoveringFromFall
      || !Mathf.Approximately(_steadyCooldownTimer, 0)) return;
    
    if (_steadying) {
      // Stop steadying
      _steadying = false;
      _steadyCooldownTimer = _data.SteadyCooldown[_steadfastness];
    } else {
      // Start steadying and start the timer
      _steadying = true;
      _steadyTimer = _data.SteadyDuration[_steadfastness];
      // Stop moving
      _rigidbody.linearVelocity = new();
    }
  }
  
  public void OnLink(InputAction.CallbackContext context) {
    // A link can only happen if the role allows for linking
    if (!Alive || !context.started
      || (_data.Role != PlayerRole.Tank && _data.Role != PlayerRole.Support)
      || _recoveringFromFall) return;
    
    // Stop linking
    if (_linking) StopLinking();
    else {
      // Start linking if there is someone in the aim direction to link
      var playerHits = Physics2D.BoxCastAll(transform.position, _linkSearchSize,
        0, _aimDirection, _data.LinkRange, _playerLayer);
      if (playerHits.Length == 0) return;
      
      // Search for someone to link with
      foreach (var hit in playerHits) {
        if (hit.collider.gameObject == gameObject) continue;
        var linkedPlayer = hit.collider.GetComponent<Player>();
        if (!linkedPlayer) continue;
        
        // Link with the other player
        _linking = true;
        _linkedPlayer = linkedPlayer;
        _linkRenderer.gameObject.SetActive(true);
        UpdateLink();
        
        // Found someone to link with, so stop looking
        break;
      }
    }
  }
  
  public void OnHole() {
    // If a decoy or support, hover over the hole
    // If grappling, grapple over the hole
    if (_data.Role == PlayerRole.Decoy || _data.Role == PlayerRole.Support
      || _grappling) return;
    // Hurt the player with fall damage
    Hurt(_data.FallDamage[_steadfastness]);
    // Freeze temporarily
    _recoveringFromFall = true;
    _fallRecoverTimer = _data.FallRecoverTime[_steadfastness];
    _rigidbody.linearVelocity = new();
    // If linking, stop
    if (_linking) StopLinking();
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
      _rigidbody.linearVelocity = _data.SprintSpeed[_mobility] * _moveDirection;
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
    // Start the cooldown and reset the velocity
    _grappling = false;
    _grappleTimer = 0;
    _grappleCooldownTimer = _data.GrappleCooldown[_strength];
    _rigidbody.linearVelocity = _moving ? _data.WalkSpeed[_mobility] * _moveDirection : new();
    _hookRenderer.gameObject.SetActive(false);
    // Switch to sprinting if trying to
    TryToSprint();
  }
  
  private void StopSteadying() {
    // Start the cooldown and reset the velocity
    _steadying = false;
    _steadyCooldownTimer = _data.SteadyCooldown[_steadfastness];
    _rigidbody.linearVelocity = _moving ? _data.WalkSpeed[_mobility] * _moveDirection : new();
    // Switch to sprinting if trying to
    TryToSprint();
  }
  
  private void UpdateLink() {
    // Calculate the angle and distance between the player and linked player
    var difference = _linkedPlayer.transform.position - transform.position;
    float angle = Mathf.Rad2Deg * Mathf.Atan2(difference.y, difference.x);
    float distance = difference.magnitude;
    // Update the link visual
    _linkRenderer.transform.localEulerAngles = new(0, 0, angle);
    _linkRenderer.size = new Vector2(distance, _linkRenderer.size.y);
  }
  
  private void StopLinking() {
    // Stop linking
    _linking = false;
    _linkedPlayer = null;
    _linkRenderer.gameObject.SetActive(false);
  }
}
