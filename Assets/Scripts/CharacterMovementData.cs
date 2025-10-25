using UnityEngine;

[CreateAssetMenu(fileName = "New Movement Data", menuName = "QijTikal/Movement Data")]
public class CharacterMovementData : ScriptableObject
{
    [Tooltip("El radio en metros del área de movimiento permitida por turno.")]
    public float movementRange;
}