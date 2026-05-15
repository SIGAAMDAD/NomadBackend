using Godot;
using Nomad.Core.Events;

// Declare the payload struct.
public readonly struct DamagePlayerEventArgs
{
    public readonly int Amount;

    public DamagePlayerEventArgs(int amount)
    {
        Amount = amount;
    }
}

public partial class Enemy : Node2D
{
    // Declare the event.
    private IGameEvent<DamagePlayerEventArgs> _damagePlayer;

    public override void _Ready()
    {
        base._Ready();

        // Retrieve the event from the global registry, its name is "TakeDamage", and it's within the player's
        // namespace, so "Player".
        _damagePlayer = GameEventRegistry.GetEvent("TakeDamage", "Player");

        // Don't subscribe to the event because we're the publisher, and the player is the client.
        // We are not catching the event.
    }

    private void AttackPlayer()
    {
        // This will make sure the player's "OnTakeDamage" is called, and any other subscribers will
        // also be called whenever this event is published.
        _damagePlayer.Publish(new DamagePlayerEventArgs(10));
        GD.Print("Hit the player for 10 damage!");
    }
}
