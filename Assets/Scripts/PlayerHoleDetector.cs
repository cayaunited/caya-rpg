using UnityEngine;

public class PlayerHoleDetector : MonoBehaviour {
  [SerializeField] private float _recoverDistance = 1;
  [SerializeField] private LayerMask _holeLayer = 0;
  [SerializeField] private Collider2D _collider = null;
  [SerializeField] private Player _player = null;
  
  private Vector3 _safePosition = new();
  
  private void Update()
  {
    // If safe, update the safe position
    if (!_collider.IsTouchingLayers(_holeLayer))
      _safePosition = _player.transform.position;
  }
  
  private void OnTriggerEnter2D(Collider2D collision) {
    if (!_collider.IsTouchingLayers(_holeLayer)
      || _player.Role == PlayerRole.Decoy || _player.Role == PlayerRole.Support) return;
    // If hit a hole, send the fall message
    _player.OnHole();
    
    // Send the player back to a safe place if still alive
    if (_player.Alive) _player.transform.position += _recoverDistance
      * (_safePosition - _player.transform.position).normalized;
  }
}
