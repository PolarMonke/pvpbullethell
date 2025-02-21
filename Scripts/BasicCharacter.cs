using Godot;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public partial class BasicCharacter : CharacterBody2D
{
    [Export] protected float _speed = 200.0f;

    [Export] public int MaxHealth = 100;
    [Export] public int Health = 100;
    protected bool _canBeHurt = true;

    [Export] protected ProgressBar healthBar;

    [Export] protected PackedScene bulletScene;
    [Export] protected Marker2D bulletSpawn;
    [Export] protected Timer bulletCooldownNode;
    [Export] protected float bulletCooldown = 0.3f;

    [Export] public AnimatedSprite2D animatedSprite;

    [Signal]
    public delegate void PlayerFiredBulletEventHandler(Area2D bullet, Vector2 postition, Vector2 direction, long holderId);
    protected Vector2 _previousPosition;

    protected bool _facingRight = true;

    protected enum AnimationState {Idle, Walk, Attack, Run, Hurt, Die}    
    protected AnimationState currentAnimationState = AnimationState.Idle;

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

            GD.Print($"Player {Name} entered tree with authority: {IsMultiplayerAuthority()}");
        }
        else
        {
            GD.PrintErr("Player name is null!");
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
    protected virtual void Walk()
    {
        bool isMoving = Velocity.Length() > 0;
        if (currentAnimationState != AnimationState.Attack && currentAnimationState != AnimationState.Hurt)
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
        if (_canBeHurt)
        {
            SetAnimationState(AnimationState.Hurt);

            if (!IsMultiplayerAuthority()) return;

            Health -= damage;
            Health = Mathf.Clamp(Health, 0, MaxHealth);

            Rpc(nameof(SyncHealth), Health); 

            if (Health <= 0) Die();
        }
        
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    protected void SyncHealth(int newHealth)
    {
        Health = newHealth;
        UpdateHealthDisplay(); 
    }

    protected void UpdateHealthDisplay()
    {
        if (healthBar != null)
        {
            healthBar.Value = Health; 
        }
    }

    protected void Die()
    {
        Rpc(nameof(DeathEffects));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    protected void DeathEffects()
    {
        QueueFree();
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
    protected virtual void Shoot()
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
        GetTree().Root.AddChild(bulletInstance);
        BulletManager.Instance.HandleBulletSpawned(bulletInstance, bulletInstance.GlobalPosition, directionToMouse, Multiplayer.GetUniqueId());
    }

    protected void SetAnimationState(AnimationState newState)
    {
        if (currentAnimationState == newState) return;
        currentAnimationState = newState;
        UpdateAnimation();
        if (IsMultiplayerAuthority())
        {
            Rpc(nameof(SyncAnimationState), (int)newState);
        }
    }

    protected void UpdateAnimation()
    {
        string targetAnimation = currentAnimationState switch
        {
            AnimationState.Walk => "Walk",
            AnimationState.Attack => "Attack",
            AnimationState.Run => "Run",
            AnimationState.Hurt => "Hurt",
            AnimationState.Die => "Die",
            _ => "Idle"
        };
        if (animatedSprite.Animation != targetAnimation || !animatedSprite.IsPlaying())
        {
            animatedSprite.Play(targetAnimation);
        }
    }

    protected void OnAnimationFinished()
    {
        currentAnimationState = Velocity.Length() > 0 ? AnimationState.Walk : AnimationState.Idle;
        UpdateAnimation();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    protected void ServerSyncAnimation(int state)
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
    protected void SyncAnimationState(int state)
    {
        if (!IsMultiplayerAuthority())
        {
            currentAnimationState = (AnimationState)state;
            UpdateAnimation();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    protected void SetPlayerPosition(Vector2 position)
    {
        if (!IsMultiplayerAuthority())
        {
            Position = position;
        }
    }

    protected void UpdateFacingDirection()
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
    protected void SetFacingDirection(bool direction, Vector2 position)
    {
        if (!IsMultiplayerAuthority())
        {
            bulletSpawn.Position = position;
            animatedSprite.FlipH = direction;
        }
    }
}