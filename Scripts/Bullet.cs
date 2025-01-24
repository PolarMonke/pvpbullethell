using Godot;
using System;
using System.Runtime;

public partial class Bullet : Area2D
{
	[Export] public int speed = 10;
	[Export] public float LifeTime = 4.0f;
    private Timer _lifeTimer;
	private Vector2 direction = Vector2.Zero;

	public override void _Ready()
    {
        _lifeTimer = new Timer();
        _lifeTimer.WaitTime = LifeTime;
        _lifeTimer.OneShot = true;
        AddChild(_lifeTimer);
        _lifeTimer.Start();
        _lifeTimer.Timeout += DestroyBulletAfterTime;
    }

	public override void _PhysicsProcess(double delta)
	{
		if (direction != Vector2.Zero)
		{
			var velocity = direction * speed;
			GlobalPosition += velocity;
		}
		//Rpc(nameof(this.SetBulletPosition), Position);
	}
	public void SetDirection(Vector2 direction)
	{
		this.direction = direction;
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
