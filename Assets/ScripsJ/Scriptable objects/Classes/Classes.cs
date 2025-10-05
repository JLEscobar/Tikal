using UnityEngine;

[CreateAssetMenu(fileName = "New Class", menuName = "Classes/New Class")]
public class Classes : ScriptableObject
{
    [Tooltip("Nombre de la clase que se esta implementando")]
    public string className;

    [Tooltip("Cantidad de vida maxima que va a tener la clase cuando incie la partida")]
    public float maxHealth;

    [Tooltip("Nombre de la habilidad basica")]
    public string NormalAttackName;
    public float attackNormal;
    public float cooldownNormal;
    //en duda si esto va aqui

    [Tooltip("Nombre de la habilidad especial")]
    public string SpecialAttackName;
    public float attackSpecial;
    public float cooldownSpecial;

    [Tooltip("Velocidad: cantidad de area disponible para el jugador, se calcula en metros.")]
    public float movementRange;
}
