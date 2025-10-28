public interface IAbility
{
    string Id { get; }
    string DisplayName { get; }
    int CostAP { get; }
    float Range { get; }

    bool CanExecute(CharacterActor user, ITargetable target);
    void Execute(CharacterActor user, ITargetable target);
}
