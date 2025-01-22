using Godot;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public partial class BasicCharacter : CharacterBody2D
{
    [Export] private float _speed = 400.0f;
    [Export] private PackedScene bullet;

    [Export] private Marker2D bulletSpawn;
	private Vector2 _previousPosition;



    public override void _EnterTree()
    {
        if (Name != null)
        {
            SetMultiplayerAuthority(Int32.Parse(Name));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            Walk();
            Rotate();
            
        }
    }
    protected void Walk()
    {
        _previousPosition = Position;
        Vector2 velocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * _speed;
        Velocity = velocity;
        MoveAndSlide();

        for (int i = 0; i < GetSlideCollisionCount(); i++) { //TODO: Optimize
            KinematicCollision2D collision = GetSlideCollision(i);
            if (collision.GetCollider() is BasicCharacter otherPlayer)
            {
                Vector2 pushVector = Position - _previousPosition;
                Position -= pushVector;
            }
        }
        Rpc(nameof(SetPlayerPosition), Position);
    }
    private void Rotate()
    {
        LookAt(GetGlobalMousePosition());
        Rpc(nameof(SetPlayerRotation), Rotation);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("shoot"))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Node2D bulletInstance = bullet.Instantiate<Node2D>();
        AddChild(bulletInstance);
        bulletInstance.GlobalPosition = bulletSpawn.GlobalPosition;

        Vector2 target = GetGlobalMousePosition();
        Vector2 directionToMouse = bulletInstance.GlobalPosition.DirectionTo(target).Normalized();
        Bullet bulletScript = bulletInstance as Bullet;
        bulletScript.SetDirection(directionToMouse);
    }

   [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SetPlayerPosition(Vector2 position)
    {
		if (!IsMultiplayerAuthority())
		{
			Position = position;
		}
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SetPlayerRotation(float rotation)
    {
		if (!IsMultiplayerAuthority())
		{
			Rotation = rotation;
		}
    }
}
