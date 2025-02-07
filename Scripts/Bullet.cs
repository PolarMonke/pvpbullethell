using Godot;
using System;
using System.Runtime;

public partial class Bullet : Area2D
{
	[Export] public int Damage = 10;
	[Export] public int Speed = 10;
	[Export] public float LifeTime = 1.0f;
    private Timer _lifeTimer;
	private Vector2 direction = Vector2.Zero;

	public long HolderID;
	private bool _damageApplied = false;

	public override void _Ready()
    {
        _lifeTimer = new Timer();
        _lifeTimer.WaitTime = LifeTime;
        _lifeTimer.OneShot = true;
        AddChild(_lifeTimer);
        _lifeTimer.Start();
        _lifeTimer.Timeout += DestroyBulletAfterTime;

		//BodyEntered += OnBodyEntered;
    }

	public override void _PhysicsProcess(double delta)
	{
		if (direction != Vector2.Zero)
		{
			var velocity = direction * Speed;
			GlobalPosition += velocity;
		}

		if (Multiplayer.IsServer() && _lifeTimer.TimeLeft % 0.1f < delta)
		{
			Rpc(nameof(SetBulletPosition), Position);
		}
	}
	public void SetDirection(Vector2 direction)
	{
		this.direction = direction;
	}

	private void OnBodyEntered(Node body)
    {
		GD.Print("Bullet collided");
        if (_damageApplied) return;
        if (body is BasicCharacter player)
        {
            GD.Print($"Bullet: Collision with player, HolderID = {HolderID}, Player Authority = {player.GetMultiplayerAuthority()}");

            if (player.GetMultiplayerAuthority() != HolderID)
            {
                GD.Print("Bullet: Applying damage");
                player.Rpc(nameof(player.TakeDamage), Damage);
                _damageApplied = true;
            }
            else
            {
                GD.Print("Bullet: Not applying damage (self-hit)");
            }
            this.QueueFree();
			//SetDeferred("free", 1);
        }
    }

	private void DestroyBulletAfterTime()
	{
		QueueFree();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetBulletPosition(Vector2 position)
    {
		if (!IsMultiplayerAuthority())
		{
			Position = position;
		}
    }
}
