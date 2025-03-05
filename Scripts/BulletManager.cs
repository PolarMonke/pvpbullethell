using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class BulletManager : Node2D
{
    public static BulletManager Instance;
    private Godot.Collections.Dictionary<string, PackedScene> _bulletScenes = new Godot.Collections.Dictionary<string, PackedScene>();

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
        _bulletScenes.Add("Default", GD.Load<PackedScene>("res://Prefabs/Bullets/bullet.tscn"));
        _bulletScenes.Add("Explosive", GD.Load<PackedScene>("res://Prefabs/Bullets/explosive_bullet.tscn"));
        _bulletScenes.Add("Boss", GD.Load<PackedScene>("res://Prefabs/Bullets/boss_bullet.tscn"));
    }

    public void HandleBulletSpawned(string bulletType, Vector2 position, Vector2 direction, long holderId)
    {
        if (Multiplayer.IsServer())
        {
            Rpc(nameof(SpawnBulletOnClients), bulletType, position, direction, holderId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SpawnBulletOnClients(string bulletType, Vector2 position, Vector2 direction, long holderId)
    {
        if (_bulletScenes.TryGetValue(bulletType, out var bulletScene))
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
}