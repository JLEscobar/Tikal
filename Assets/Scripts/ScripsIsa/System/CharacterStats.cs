using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "QijTikal/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "Character";
    public Team team = Team.Player;

    [Header("Combat Stats")]
    public int maxHealth = 100;
    public int attackPower = 10;
    public int actionPointsPerTurn = 2;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float movementRange = 5f;

    [Header("Abilities")]
    public AbilityBase[] abilities;
}
