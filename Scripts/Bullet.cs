using Godot;
using System;

public partial class Bullet : Area2D
{
	[Export] public int speed = 10;
	private Vector2 direction = Vector2.Zero;
	public override void _PhysicsProcess(double delta)
	{
		if (direction != Vector2.Zero)
		{
			var velocity = direction * speed;
			GlobalPosition += velocity;
		}
	}
	public void SetDirection(Vector2 direction)
	{
		this.direction = direction;
	}
}
