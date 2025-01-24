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
                    GD.Print($"Host player {Multiplayer.GetUniqueId()} is shooting locally");
                    Shoot(bulletSpawn.GlobalPosition, GetGlobalMousePosition());
                } 
                else
                {
                    GD.Print($"Client player {Multiplayer.GetUniqueId()} is calling Rpc");
                    RpcId(Multiplayer.GetUniqueId(), nameof(Shoot), bulletSpawn.GlobalPosition, GetGlobalMousePosition());
                }
            }
        }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void Shoot(Vector2 startPosition, Vector2 targetPosition)
    {
        GD.Print($"Shoot function called by: {Multiplayer.GetUniqueId()}");
        if (bulletScene == null || bulletSpawn == null)
        {
            GD.PrintErr("Assign bullet and bulletSpawn in editor");
            return;
        }

        Area2D bulletInstance = bulletScene.Instantiate<Area2D>();
        if (bulletInstance == null)
        {
            GD.PrintErr("Could not get bullet scene as a Area2D");
            return;
        }
        
        Vector2 directionToMouse = startPosition.DirectionTo(targetPosition).Normalized();
        bulletInstance.GlobalPosition = startPosition;

        Variant scriptVariant = bulletInstance.GetScript();
        if (scriptVariant.Obj is Bullet bulletScript)
        {
            bulletScript.SetDirection(directionToMouse);
        }
        GD.Print($"Bullet fired by {Multiplayer.GetUniqueId()}");
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