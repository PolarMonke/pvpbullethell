using Godot;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Collections.Generic;
using System.Collections;

public partial class BossCharacter : BasicCharacter
{
	[Export] public float PatternCooldown = 3.0f;
    private Timer _patternTimer;
    private List<Action> _shootingPatterns = new List<Action>();
    private int _currentPatternIndex = 0;


	public override void _EnterTree()
    {
		base._EnterTree();
        if (Name != null)
        {
			_patternTimer = new Timer();
			_patternTimer.WaitTime = PatternCooldown;
			_patternTimer.OneShot = false;
			AddChild(_patternTimer);
			_patternTimer.Timeout += OnPatternTimerTimeout;
			_patternTimer.Start();

			_shootingPatterns.Add(ShootCircle);
			_shootingPatterns.Add(ShootCross);
			_shootingPatterns.Add(ShootSpiral);
			_shootingPatterns.Add(ShootSineWave);
			_shootingPatterns.Add(ShootSun);
        }
    }
	protected override void HandleShooting()
    {
        // if (Input.IsActionPressed("shoot") && bulletCooldownNode.IsStopped() && IsMultiplayerAuthority())
        // {
        //     bulletCooldownNode.Start(bulletCooldown);
        //     Shoot();
        // }
    }
	private void OnPatternTimerTimeout()
    {
        _shootingPatterns[_currentPatternIndex]?.Invoke();
        _currentPatternIndex = (_currentPatternIndex + 1) % _shootingPatterns.Count;
    }
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SpawnBullet(Vector2 direction)
	{
		var bulletInstance = bulletScene.Instantiate<Area2D>();
		bulletInstance.GlobalPosition = bulletSpawn.GlobalPosition;

		var bulletScript = bulletInstance as Bullet;
		if (bulletScript != null)
		{
			bulletScript.HolderID = GetMultiplayerAuthority();
			bulletScript.SetDirection(direction);
		}

		GetTree().Root.AddChild(bulletInstance);

		// if (Multiplayer.GetUniqueId() == 1)
		// {
		// 	RequestShoot("Default", bulletInstance.GlobalPosition, direction, bulletScript.HolderID);
		// }
		// else
		// {
		// 	RpcId(1, nameof(RequestShoot), "Default", bulletInstance.GlobalPosition, direction, bulletScript.HolderID);
		// }
		RequestShoot("Default", bulletInstance.GlobalPosition, direction, bulletScript.HolderID);
	}

	#region Shooting Patterns

    private void ShootCircle()
    {
		GD.Print("Shooting Circle");
        int bulletCount = 16;
        float angleIncrement = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = Mathf.DegToRad(i * angleIncrement);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (IsMultiplayerAuthority())
            {
                SpawnBullet(dir);
            }
        }
    }

	private void ShootCross()
	{
		GD.Print("Shooting Cross");
		RunCoroutine(CrossPattern());
	}
	private IEnumerator CrossPattern()
	{
		int rowCount = 8;
		int sideCount = 4;

		for (int i = 0; i < rowCount; i++)
		{
			float angle = 0.0f;
			for (int j = 0; j < sideCount; j++)
			{
				angle = Mathf.DegToRad(j * 90);
				Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
				SpawnBullet(dir);
			}	
			yield return ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void ShootSpiral()
	{
		GD.Print("Shooting Spiral");
		RunCoroutine(SpiralPattern());
	}
	private IEnumerator SpiralPattern()
	{
		int bulletCount = 36;
		for (int i = 0; i < bulletCount; i++)
		{
			float angle = Mathf.DegToRad(i * 10);
			Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			SpawnBullet(dir);
			yield return ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void ShootSun()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);
		GD.Print("Shooting Sun");
		RunCoroutine(SunPattern());
	}
	private IEnumerator SunPattern() 
	{
		int bulletCount = 18;
		int sides = 4;
		for (int i = 0; i < bulletCount; i++)
		{
			for (int j = 0; j < sides; j++)
			{
				float angle = Mathf.DegToRad(i * 10 + j * 90);
				Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
				SpawnBullet(dir);
			}
			yield return ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
		}
	}


	private void ShootSineWave()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);
		GD.Print("Shooting Sine Wave");
		RunCoroutine(SineWavePattern());
	}

	private IEnumerator SineWavePattern()
	{
		float amplitude = 0.2f;
		float frequency = 2.0f;
		float time = 0.0f;
		int bulletCount = 20;

		Vector2 globalMousePosition = GetGlobalMousePosition();
		Vector2 baseDirection = (globalMousePosition - GlobalPosition).Normalized();

		for (int i = 0; i < bulletCount; i++)
		{
			time += 0.1f;
			float angleOffset = Mathf.Sin(time * frequency) * amplitude;
			Vector2 dir = baseDirection.Rotated(angleOffset);
			SpawnBullet(dir);
			yield return ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
		}
	}	
    #endregion Shooting Patterns
	private async void RunCoroutine(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is Godot.SignalAwaiter signalAwaiter)
            {
                await signalAwaiter;
            }
        }
    }
}
