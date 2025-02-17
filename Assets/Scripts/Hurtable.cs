using UnityEngine;

public class Hurtable : MonoBehaviour {
  public float Health { get; protected set; }
  public float MaxHealth { get; protected set; }
  public bool Alive { get; protected set; } = true;
  
  public void Heal(float amount) {
    // Heal the given amount and cap at max
    Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
    Debug.Log($"HEALED TO {Health} / {MaxHealth}");
  }
  
  public void Hurt(float amount) {
    // Take the given damage and die if at 0
    Health = Mathf.Clamp(Health - amount, 0, MaxHealth);
    if (Mathf.Approximately(Health, 0)) Die();
    else Debug.Log($"HURT TO {Health} / {MaxHealth}");
  }
  
  public void Die() {
    Alive = false;
    Debug.Log("DEAD");
  }
}
