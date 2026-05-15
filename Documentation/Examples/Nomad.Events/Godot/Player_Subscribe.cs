using Godot;
using Nomad.Core.Events;

// Declare the payload struct
public readonly struct DamagePlayerEventArgs
{
    public readonly int Amount;

    public DamagePlayerEventArgs(int amount)
    {
        Amount = amount;
    }
}

public partial class Player : Node2D
{
    private int _health = 100;

    // Declare the event.
    private IGameEvent<DamagePlayerEventArgs> _takeDamage;

    public override void _Ready()
    {
        base._Ready();

        // Retrieve the event from the global registry, its name is "TakeDamage", and it's within the player's
        // namespace, so "Player".
        _takeDamage = GameEventRegistry.GetEvent("TakeDamage", "Player");

        // Hook the subscription callbacks. "Subscribe" to the event, the "TakeDamage" method will be
        // called into every time the event is published.
        _takeDamage.Subscribe(this, OnTakeDamage);
    }

    // This will be called whenever the TakeDamage event is published.
    private void OnTakeDamage(in DamagePlayerEventArgs args)
    {
        _health -= args.Amount;
        GD.Print("Took Damage!");
    }
}
