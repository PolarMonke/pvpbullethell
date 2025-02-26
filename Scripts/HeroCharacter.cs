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
        base._EnterTree();
        if (Name != null)
        {
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
        GD.Print("Run timer ran out");
        _speed /= 2;
        _canRun = false;
        _canBeHurt = true;
        _runCooldownTimer.Start();
    }

    private void OnRunCooldownTimerTimeOut()
    {
        GD.Print("Run cooldown timer ran out");
        _canRun = true;
    }

}
