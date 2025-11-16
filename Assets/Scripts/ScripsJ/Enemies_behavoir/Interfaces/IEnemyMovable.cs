using UnityEngine;

public interface IEnemyMovable 
{
    CharacterController controller { get; set; }

    bool canMove { get; set; }

    bool facingPlayer { get; set; }

    float moveSpeed { get; set; }
    void moveEnemy(Vector3 direction);

    void CheckFacing(Vector3 direction);


}
