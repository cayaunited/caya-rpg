using UnityEngine;

[CreateAssetMenu(fileName = "New Player", menuName = "RPG/Player")]
public class PlayerData : ScriptableObject {
  [Header("Character Variables")]
  public PlayerRole Role = PlayerRole.Decoy;
  
  [Header("Movement Variables")]
  public float WalkSpeed = 1;
  public float SprintSpeed = 2;
  public float SprintDuration = 1;
  public float DashSpeed = 3;
  public float DashDuration = 1;
  public float DashCooldown = 1;
  public float TeleportDistance = 2;
  public float TeleportCooldown = 1;
  public float TeleportMinCharge = 1;
  public float TeleportMaxCharge = 2;
  public float BounceSpeed = 3;
  public float BounceDuration = 1;
  public float BounceCooldown = 1;
  public float BounceMinCharge = 1;
  public float BounceMaxCharge = 2;
  public float GrappleRange = 2;
  public float GrappleSpeed = 3;
  public float GrappleDuration = 1;
  public float GrappleCooldown = 1;
  public float SteadyDuration = 2;
  public float SteadyCooldown = 2;
  public float LinkRange = 2;
}
