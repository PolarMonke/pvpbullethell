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

        AddChild(bullet);
        bullet.GlobalPosition = position;

        var bulletScript = bullet.GetNode<Bullet>(".");
        if (bulletScript != null)
        {
            bulletScript.SetDirection(direction);
        }
        else
        {
            GD.PrintErr("Bullet script not found");
        }

        if (Multiplayer.IsServer())
        {
            Rpc(nameof(ClientSpawnBullet), position, direction);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientSpawnBullet(Vector2 position, Vector2 direction)
    {
        if (Multiplayer.IsServer()) return;

        var bulletInstance = bulletScene.Instantiate<Area2D>();
        AddChild(bulletInstance);
        bulletInstance.GlobalPosition = position;

        var bulletScript = bulletInstance.GetNode<Bullet>(".");
        if (bulletScript != null)
        {
            bulletScript.SetDirection(direction);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]

    public void SpawnBullet(Area2D bulletInstance, Vector2 position, Vector2 direction)
    {
        AddChild(bulletInstance);
        bulletInstance.GlobalPosition = position;
        if (bulletInstance is Bullet bulletScript)
        {
            bulletScript.SetDirection(direction);
        }
    }
}