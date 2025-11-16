using System.Xml.Serialization;
using UnityEngine;

public class EnemyState 
{
    protected Enemys enemy;
    protected EnemyStateMachine stateMachine;

    public EnemyState(Enemys enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public virtual void EnterState() { }

    public virtual void UpdateState() { }

    public virtual void ExitState() { }

    public virtual void PhysicsUpdate() { }

    public virtual void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType) { }




}
