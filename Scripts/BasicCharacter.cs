using Godot;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public partial class BasicCharacter : CharacterBody2D
{
    [Export] private float _speed = 200.0f;

    [Export] public int MaxHealth = 100;
    [Export] public int Health = 100;

    [Export] ProgressBar healthBar;

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

            healthBar.MaxValue = MaxHealth;
            Health = MaxHealth;
            UpdateHealthDisplay();

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
    

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void TakeDamage(int damage)
    {
        //if (!IsMultiplayerAuthority()) { return; }
        GD.Print($"Taken {damage} damage");
        Health -= damage;
        GD.Print($"Current HP: {Health}");
        UpdateHealthDisplay();

        if (Health <= 0)
        {
            GD.Print("Player died");
            Die();
        }
    }
    
    private void UpdateHealthDisplay()
    {
        if (healthBar != null)
        {
            healthBar.Value = Health;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SyncHealth(int newHealth)
    {
        Health = newHealth;
        healthBar.Value = Health;
        UpdateHealthDisplay();
    }
    private void Die()
    {
        Rpc(nameof(DeathEffects));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void DeathEffects()
    {
        // Анімацыя смерці і знікненне
        QueueFree();
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
                    Shoot();
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void Shoot()
    {
        GD.Print($"Server shooting bullet for player {Name}");
        if (bulletScene == null || bulletSpawn == null)
        {
            GD.PrintErr("Assign bullet and bulletSpawn in editor");
            return;
        }
        SetAnimationState(AnimationState.Attack);
        Area2D bulletInstance = bulletScene.Instantiate<Area2D>();
        Vector2 directionToMouse = bulletSpawn.GlobalPosition.DirectionTo(GetGlobalMousePosition()).Normalized();
        bulletInstance.GlobalPosition = bulletSpawn.GlobalPosition;

        var bulletScript = bulletInstance as Bullet;
        if(bulletScript != null)
        {
            bulletScript.HolderID = Multiplayer.GetUniqueId();
            bulletScript.SetDirection(directionToMouse);
        }
        GetParent().AddChild(bulletInstance);
    }
    

   [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SetPlayerPosition(Vector2 position)
    {
        if (!IsMultiplayerAuthority())
        {
            Position = position;
        }
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
        bulletSpawn.Position = _facingRight ? new Vector2(16, 16) : new Vector2(-20, 16);
        Rpc(nameof(SetFacingDirection), animatedSprite.FlipH, bulletSpawn.Position);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SetFacingDirection(bool direction, Vector2 position)
    {
        if (!IsMultiplayerAuthority())
        {
            bulletSpawn.Position = position;
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