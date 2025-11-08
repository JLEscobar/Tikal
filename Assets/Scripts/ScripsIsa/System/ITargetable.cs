using UnityEngine;

public interface ITargetable
{
    Team Team { get; }
    IHealth Health { get; }
    Transform GetTransform();
}
