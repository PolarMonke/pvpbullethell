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
    protected bool _isDead = false;

    [Export] protected ProgressBar healthBar;
    [Export] protected AudioStreamPlayer2D audioPlayer;
    [Export] protected Marker2D bulletSpawn;
    [Export] protected Timer bulletCooldownNode;
    [Export] protected float bulletCooldown = 0.3f;

    [Export] public AnimatedSprite2D animatedSprite;

    [Signal] 
    public delegate void PlayerFiredBulletEventHandler(string bullet, Vector2 postition, Vector2 direction, long holderId);
    protected Vector2 _previousPosition;

    protected bool _facingRight = true;

    protected enum AnimationState {Idle, Walk, Attack, Run, Hurt, Die}    
    protected AnimationState currentAnimationState = AnimationState.Idle;

    [Export] protected Area2D _hitbox;
    [Signal]
    public delegate void HitboxHitEventHandler(int damage);
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

            _hitbox = GetNode<Area2D>("HitboxArea");
            _hitbox.Connect("area_entered", Callable.From((Area2D area) => OnHitboxAreaEntered(area)));
            this.Connect(nameof(HitboxHit), Callable.From((int damage) => TakeDamage(damage)));

            audioPlayer = GetNode<AudioStreamPlayer2D>("AudioStreamPlayer2D");

            GD.Print($"Player {Name} entered tree with authority: {IsMultiplayerAuthority()}");
        }
        else
        {
            GD.PrintErr("Player name is null!");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority() && !_isDead)
        {
            Walk();
            UpdateFacingDirection();
            UpdateAnimation();
            HandleShooting();
        }
    }
    protected virtual void Walk()
    {
        bool isMoving = Velocity.Length() > 0;
        if (currentAnimationState != AnimationState.Attack && currentAnimationState != AnimationState.Hurt && currentAnimationState != AnimationState.Die)
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
            if (collision.GetCollider() is CollisionPolygon2D)
            {
                GD.Print("Collided with wall");
                Vector2 pushVector = Position - _previousPosition;
                Position -= pushVector;
            }
        }

        Rpc(nameof(SetPlayerPosition), Position);
    }
    
    private void OnHitboxAreaEntered(Area2D area)
    {
        if (area is Bullet bullet && bullet.HolderID.ToString() != Name)
        {
            EmitSignal(nameof(HitboxHit), bullet.Damage);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void TakeDamage(int damage)
    {
        if (_canBeHurt && !_isDead)
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
        if (!_isDead)
        {
            SetAnimationState(AnimationState.Die);
            _isDead = true;
            GameManager.Instance.Lose();
            Rpc(nameof(DeathEffects));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    protected void DeathEffects()
    {
        SetAnimationState(AnimationState.Die);
        _isDead = true;
        Rpc(nameof(EndGame));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    protected void EndGame()
    {
        GameManager.Instance.Rpc(nameof(GameManager.EndGame));
    }

    protected virtual void HandleShooting()
    {
        if (Input.IsActionPressed("shoot") && bulletCooldownNode.IsStopped() && IsMultiplayerAuthority())
        {
            bulletCooldownNode.Start(bulletCooldown);
            Shoot();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    protected virtual void Shoot()
    {
        SetAnimationState(AnimationState.Attack);
        var direction = (GetGlobalMousePosition() - GlobalPosition).Normalized();

        if (GetMultiplayerAuthority() == 1)
		{
			RequestShoot("Default", bulletSpawn.GlobalPosition, direction, GetMultiplayerAuthority());
		}
		else
		{
			RpcId(1, nameof(RequestShoot), "Default", bulletSpawn.GlobalPosition, direction, GetMultiplayerAuthority());
		}
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    protected void RequestShoot(string type, Vector2 position, Vector2 direction, long id)
    {
        if (Multiplayer.IsServer())
        {
            audioPlayer.Play();
            BulletManager.Instance.HandleBulletSpawned(type, position, direction, id);
        }
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
            AnimationState.Die => "Dead",
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