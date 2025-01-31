using Godot;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public partial class BasicCharacter : CharacterBody2D
{
    [Export] private float _speed = 200.0f;

    [Export] private PackedScene bulletScene;
    [Export] private Marker2D bulletSpawn;
    [Export] private Timer bulletCooldownNode;
    [Export] private float bulletCooldown = 0.3f;

    [Export] private AnimatedSprite2D animatedSprite;

    [Signal]
    public delegate void PlayerFiredBulletEventHandler(Area2D bullet, Vector2 postition, Vector2 direction);
    private Vector2 _previousPosition;

    private bool _facingRight = true;

    private enum AnimationState {Idle, Walk, Attack}    
    private AnimationState currentAnimationState = AnimationState.Idle;

    public override void _EnterTree()
    {
        if (Name != null)
        {
            animatedSprite.Animation = "Idle";
            animatedSprite.Play();
            SetMultiplayerAuthority(Int32.Parse(Name));

            animatedSprite.AnimationFinished += OnAnimationFinished;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            Walk();
            UpdateFacingDirection();
            UpdateAnimation();
        }
    }
    protected void Walk()
    {
        bool isMoving = Velocity.Length() > 0;
        if (currentAnimationState != AnimationState.Attack)
        {
            SetAnimationState(isMoving ? AnimationState.Walk : AnimationState.Idle);
        }
        _previousPosition = Position;
        Vector2 velocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * _speed;
        Velocity = velocity;


        MoveAndSlide();

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            if (collision.GetCollider() is BasicCharacter otherPlayer)
            {
                Vector2 pushVector = Position - _previousPosition;
                Position -= pushVector;
            }
        }

        Rpc(nameof(SetPlayerPosition), Position);
    }
    private void UpdateFacingDirection()
    {
        Vector2 mousePosition = GetGlobalMousePosition();
        float moveInput = Input.GetAxis("ui_left", "ui_right");

        if (moveInput != 0)
        {
            bool movingRight = moveInput > 0;

            bool mouseOpposite = (movingRight && mousePosition.X < GlobalPosition.X) ||
                                (!movingRight && mousePosition.X > GlobalPosition.X);

            if (!mouseOpposite)
            {
                _facingRight = movingRight;
            }
        }
        else
        {
            _facingRight = mousePosition.X > GlobalPosition.X;
        }
        animatedSprite.FlipH = !_facingRight;
        Rpc(nameof(SetFacingDirection), animatedSprite.FlipH);
    }
    private void SetAnimationState(AnimationState newState)
    {
        if (currentAnimationState == newState) return;
        currentAnimationState = newState;

        if (IsMultiplayerAuthority())
        {
            if (Multiplayer.IsServer())
            {
                Rpc(nameof(SyncAnimationState), (int)newState);
            }
            else
            {
                RpcId(1, nameof(ServerSyncAnimation), (int)newState);
            }
        }
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        string targetAnimation = currentAnimationState switch
        {
            AnimationState.Walk => "Walk",
            AnimationState.Attack => "Attack",
            _ => "Idle"
        };

        if (animatedSprite.Animation != targetAnimation || !animatedSprite.IsPlaying())
        {
            animatedSprite.Play(targetAnimation);
        }
    }

    private void OnAnimationFinished()
    {
        currentAnimationState = Velocity.Length() > 0 ? AnimationState.Walk : AnimationState.Idle;
        UpdateAnimation();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("shoot") && bulletCooldownNode.IsStopped())
        {
            bulletCooldownNode.Start(bulletCooldown);
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
        SetAnimationState(AnimationState.Attack);

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
    private void SetFacingDirection(bool direction)
    {
        if (!IsMultiplayerAuthority())
        {
            animatedSprite.FlipH = direction;
        }
    }

    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ServerSyncAnimation(int state)
    {
        if (Multiplayer.IsServer())
        {
            int senderId = Multiplayer.GetRemoteSenderId();
            if (senderId == GetMultiplayerAuthority())
            {
                Rpc(nameof(SyncAnimationState), state);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SyncAnimationState(int state)
    {
        if (!IsMultiplayerAuthority())
        {
            currentAnimationState = (AnimationState)state;
            UpdateAnimation();
        }
    }
}