using Godot;
using System;

public partial class HeroCharacter : BasicCharacter
{
    private bool _canRun = true;
    private float _runTime = 0.5f;
    private float _runCoolDown = 5.0f;
    private Timer _runTimer;
    private Timer _runCooldownTimer;

    public override void _EnterTree()
    {
        if (Name != null)
        {
            ///////////////////////////
            animatedSprite.Animation = "Idle";
            animatedSprite.Play();
            SetMultiplayerAuthority(Int32.Parse(Name));

            healthBar.MaxValue = MaxHealth;
            Health = MaxHealth;
            UpdateHealthDisplay();

            animatedSprite.AnimationFinished += OnAnimationFinished;
            ///////////////////////////


            _runTimer = new Timer();
            _runTimer.WaitTime = _runTime;
            _runTimer.OneShot = true;
            _runTimer.Timeout += OnRunTimerTimeOut;
            AddChild(_runTimer);

            _runCooldownTimer = new Timer();
            _runCooldownTimer.WaitTime = _runCoolDown;
            _runCooldownTimer.OneShot = true;
            _runCooldownTimer.Timeout += OnRunCooldownTimerTimeOut;
            AddChild(_runCooldownTimer);
        }
    }

    protected override void Walk()
    {
        
        bool isMoving = Velocity.Length() > 0;
        if (_runTimer.IsStopped())
        {
            if (currentAnimationState != AnimationState.Attack && currentAnimationState != AnimationState.Hurt && currentAnimationState != AnimationState.Run)
            {
                SetAnimationState(isMoving ? AnimationState.Walk : AnimationState.Idle);
            }
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
        if (@event.IsActionPressed("run") && _canRun)
        {
            Run();
        }
    }

    private void Run()
    {
        SetAnimationState(AnimationState.Run);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Run);
        _runTimer.Start();
        _speed *= 2;
        _canBeHurt = false;
    }

    private void OnRunTimerTimeOut()
    {
        GD.Print("Таймер бегу скончыўся");
        _speed /= 2;
        _canRun = false;
        _canBeHurt = true;
        _runCooldownTimer.Start();
    }

    private void OnRunCooldownTimerTimeOut()
    {
        GD.Print("Таймер аднаўлення бегу скончыўся");
        _canRun = true;
    }
}
