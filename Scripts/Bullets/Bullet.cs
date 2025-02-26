using Godot;
using System;
using System.Collections;
using System.Runtime;

public partial class Bullet : Area2D
{
    [Export] public int Damage = 10;
    [Export] public int Speed = 500; // Хуткасць у пікселях за секунду
    [Export] public float LifeTime = 1.0f;
    [Export] public Texture2D BulletTexture; // Тэкстура кулі

    protected Timer _lifeTimer;
    protected Vector2 direction = Vector2.Zero;
    public long HolderID;
    protected bool _damageApplied = false;

    public override void _Ready()
    {
        _lifeTimer = new Timer();
        _lifeTimer.WaitTime = LifeTime;
        _lifeTimer.OneShot = true;
        AddChild(_lifeTimer);
        _lifeTimer.Start();
        _lifeTimer.Timeout += DestroyBulletAfterTime;

        // Усталёўваем тэкстуру
        if (BulletTexture != null)
        {
            var sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.Texture = BulletTexture;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (direction != Vector2.Zero)
        {
            // Рухаем кулю з улікам часу (delta)
            var velocity = direction * Speed * (float)delta;
            GlobalPosition += velocity;
        }

        // Сінхранізацыя пазіцыі на серверы
        if (Multiplayer.IsServer())
        {
            Rpc(nameof(SetBulletPosition), GlobalPosition);
        }
    }

    public void SetDirection(Vector2 direction)
    {
        this.direction = direction;
    }

    protected virtual void OnBodyEntered(Node body)
    {
        if (_damageApplied) return;
        GD.Print(HolderID.ToString() + " - " + body.Name); 
        if (body.Name != HolderID.ToString())
        {
            if (body is BasicCharacter player)
            {
                if (body.Name != HolderID.ToString())
                {
                    player.Rpc(nameof(player.TakeDamage), Damage);
                    _damageApplied = true;
                    QueueFree();
                }
            }
        }
    }

    protected virtual void DestroyBulletAfterTime()
    {
        QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetBulletPosition(Vector2 position)
    {
        if (!IsMultiplayerAuthority())
        {
            GlobalPosition = position;
        }
    }
}