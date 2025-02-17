using UnityEngine;

public class PlayerHoleDetector : MonoBehaviour {
  [SerializeField] private LayerMask _holeLayer = 0;
  [SerializeField] private Collider2D _collider = null;
  [SerializeField] private Player _player = null;

  private void OnTriggerEnter2D(Collider2D collision) {
    // If hit a hole, send the fall message
    if (_collider.IsTouchingLayers(_holeLayer)) _player.OnHole();
  }
}
