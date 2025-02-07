using Godot;

public partial class BulletManager : Node2D
{
    public static BulletManager Instance;
    [Export] private PackedScene bulletScene;

    [Signal]
    public delegate void PlayerFiredBulletEventHandler(Area2D bullet, Vector2 position, Vector2 direction);
    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
        }
        else
        {
            Instance = this;
        }
    }
    public void HandleBulletSpawned(Area2D bullet, Vector2 position, Vector2 direction, long holderId)
    {
        if (Multiplayer.IsServer())
        {
            Rpc(nameof(SpawnBulletOnClients), position, direction, holderId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SpawnBulletOnClients(Vector2 position, Vector2 direction, long holderId)
    {
        var bulletInstance = bulletScene.Instantiate<Area2D>();
        GetTree().Root.AddChild(bulletInstance);

        bulletInstance.GlobalPosition = position;

        var bulletScript = bulletInstance.GetNode<Bullet>(".");
        if (bulletScript != null)
        {
            bulletScript.HolderID = holderId;
            bulletScript.SetDirection(direction);
        }
    }
}

