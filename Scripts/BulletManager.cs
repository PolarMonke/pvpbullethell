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
    public void HandleBulletSpawned(Area2D bullet, Vector2 position, Vector2 direction)
    {
        if (bullet == null)
        {
            GD.PrintErr("Trying to add null bullet");
            return;
        }

        //AddChild(bullet); // Remove this line
        //bullet.GlobalPosition = position; // Remove this line

        var bulletScript = bullet.GetNode<Bullet>(".");
        if (bulletScript != null)
        {
            bulletScript.HolderID = Multiplayer.GetUniqueId();
            bulletScript.SetDirection(direction);
        }
        else
        {
            GD.PrintErr("Bullet script not found");
        }

        Rpc(nameof(SpawnBulletOnClients), position, direction);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SpawnBulletOnClients(Vector2 position, Vector2 direction)
    {
        var bulletInstance = bulletScene.Instantiate<Area2D>();
        GetTree().Root.AddChild(bulletInstance);

        bulletInstance.GlobalPosition = position;

        var bulletScript = bulletInstance.GetNode<Bullet>(".");
        if (bulletScript != null)
        {
            bulletScript.SetDirection(direction);
        }
    }
}

