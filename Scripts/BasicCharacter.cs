using Godot;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public partial class BasicCharacter : CharacterBody2D
{
    [Export] private float _speed = 400.0f;
    [Export] private PackedScene bulletScene;
    [Export] private Marker2D bulletSpawn;
    [Signal]
    public delegate void PlayerFiredBulletEventHandler(Area2D bullet, Vector2 postition, Vector2 direction);
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
            if (IsMultiplayerAuthority())
            {
                if (Multiplayer.IsServer())
                {
                    Shoot(bulletSpawn.GlobalPosition, GetGlobalMousePosition());
                }
                else
                {
                    RpcId(1, nameof(ServerShoot), bulletSpawn.GlobalPosition, GetGlobalMousePosition());
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ServerShoot(Vector2 startPosition, Vector2 targetPosition)
    {
        if (Multiplayer.IsServer())
        {
            int senderId = Multiplayer.GetRemoteSenderId();
            if (senderId == GetMultiplayerAuthority())
            {
                Shoot(startPosition, targetPosition);
            }
        }
    }

    private void Shoot(Vector2 startPosition, Vector2 targetPosition)
    {
        GD.Print($"Server shooting bullet for player {Name}");
        if (bulletScene == null || bulletSpawn == null)
        {
            GD.PrintErr("Assign bullet and bulletSpawn in editor");
            return;
        }

        Area2D bulletInstance = bulletScene.Instantiate<Area2D>();
        Vector2 directionToMouse = startPosition.DirectionTo(targetPosition).Normalized();
        bulletInstance.GlobalPosition = startPosition;

        var bulletScript = bulletInstance as Bullet;
        if(bulletScript != null)
        {
            bulletScript.SetDirection(directionToMouse);
        }

        EmitSignal(SignalName.PlayerFiredBullet, bulletInstance, startPosition, directionToMouse);
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