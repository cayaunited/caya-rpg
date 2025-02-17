using UnityEngine;

[CreateAssetMenu(fileName = "New Player", menuName = "RPG/Player")]
public class PlayerData : ScriptableObject {
  [Header("Character Variables")]
  public PlayerRole Role = PlayerRole.Decoy;
  public int BaseMobility = 0;
  public int BaseIntelligence = 0;
  public int BaseStrength = 0;
  public int BaseSteadfastness = 0;
  
  [Header("Movement Variables")]
  public float[] WalkSpeed = new float[1];
  public float[] SprintSpeed = new float[1];
  public float[] SprintDuration = new float[1];
  public float[] DashSpeed = new float[1];
  public float[] DashDuration = new float[1];
  public float[] DashCooldown = new float[1];
  public float[] TeleportDistance = new float[1];
  public float[] TeleportCooldown = new float[1];
  public float TeleportMinCharge = 1;
  public float[] TeleportMaxCharge = new float[1];
  public float[] BounceSpeed = new float[1];
  public float[] BounceDuration = new float[1];
  public float[] BounceCooldown = new float[1];
  public float BounceMinCharge = 1;
  public float[] BounceMaxCharge = new float[1];
  public float[] GrappleRange = new float[1];
  public float[] GrappleSpeed = new float[1];
  public float[] GrappleDuration = new float[1];
  public float[] GrappleCooldown = new float[1];
  public float[] SteadyDuration = new float[1];
  public float[] SteadyCooldown = new float[1];
  public float LinkRange = 1;
}
