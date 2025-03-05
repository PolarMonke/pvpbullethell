using Godot;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Collections.Generic;
using System.Collections;

public partial class BossCharacter : BasicCharacter
{
	[Export] public float PatternCooldown = 1.5f;
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

			// _shootingPatterns.Add(ShootCircle);
			// _shootingPatterns.Add(ShootCross);
			// _shootingPatterns.Add(ShootSpiral);
			// _shootingPatterns.Add(ShootSineWave);
			// _shootingPatterns.Add(ShootSun);
			// _shootingPatterns.Add(ShootExplosiveBullets);
        }
    }
	protected override void HandleShooting()
    {
        // if (Input.IsActionPressed("shoot") && bulletCooldownNode.IsStopped() && IsMultiplayerAuthority())
        // {
        //     bulletCooldownNode.Start(bulletCooldown);
        //     Shoot();
        // }
		// TODO: Implement some kind of abilities
    }
	private void OnPatternTimerTimeout()
    {
        _shootingPatterns[_currentPatternIndex]?.Invoke();
		_currentPatternIndex = new Random().Next(0, _shootingPatterns.Count);
    }
	
	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void SpawnBullet(Vector2 direction, string type)
	{
		if (GetMultiplayerAuthority() == 1)
		{
			RequestShoot(type, bulletSpawn.GlobalPosition, direction, GetMultiplayerAuthority());
		}
		else
		{
			RpcId(1, nameof(RequestShoot), type, bulletSpawn.GlobalPosition, direction, GetMultiplayerAuthority());
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    protected void RequestShoot(string type, Vector2 position, Vector2 direction, long id, int speed)
    {
        if (Multiplayer.IsServer())
        {
            BulletManager.Instance.HandleBulletSpawned(type, position, direction, id, speed);
        }
    }
	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void SpawnBullet(Vector2 direction, string type, int speed)
	{
		if (GetMultiplayerAuthority() == 1)
		{
			RequestShoot("Boss", bulletSpawn.GlobalPosition, direction, GetMultiplayerAuthority(), speed);
		}
		else
		{
			RpcId(1, nameof(RequestShoot), "Boss", bulletSpawn.GlobalPosition, direction, GetMultiplayerAuthority(), speed);
		}
	}

	#region Shooting Patterns

    private void ShootCircle()
    {
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);

        int bulletCount = 16;
        float angleIncrement = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = Mathf.DegToRad(i * angleIncrement);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (IsMultiplayerAuthority())
            {
                SpawnBullet(dir, "Boss");
            }
        }
    }

	private void ShootCross()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);
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
				SpawnBullet(dir, "Boss");
			}	
			yield return ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void ShootSpiral()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);
		RunCoroutine(SpiralPattern());
	}
	private IEnumerator SpiralPattern()
	{
		int bulletCount = 36;
		for (int i = 0; i < bulletCount; i++)
		{
			float angle = Mathf.DegToRad(i * 10);
			Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			SpawnBullet(dir, "Boss");
			yield return ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void ShootSun()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);
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
				SpawnBullet(dir, "Boss");
			}
			yield return ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
		}
	}


	private void ShootSineWave()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);
		RunCoroutine(SineWavePattern());
	}

	private IEnumerator SineWavePattern()
	{
		float amplitude = 0.2f;
		float frequency = 2.0f;
		float time = 0.0f;
		int bulletCount = 20;


		Vector2 targetPosition = GetOtherPlayerPosition();
		Vector2 baseDirection = (targetPosition - GlobalPosition).Normalized();

		for (int i = 0; i < bulletCount; i++)
		{
			time += 0.1f;
			float angleOffset = Mathf.Sin(time * frequency) * amplitude;
			Vector2 dir = baseDirection.Rotated(angleOffset);
			SpawnBullet(dir, "Boss");
			yield return ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
		}
	}	

	private void ShootExplosiveBullets()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);

        int bulletCount = 8;
        float angleIncrement = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = Mathf.DegToRad(i * angleIncrement);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (IsMultiplayerAuthority())
            {
                SpawnBullet(dir, "Explosive");
            }
        }
	}

    #endregion Shooting Patterns

	#region Special Patternss

	private void ShootOrtodoxCross()
	{
		SetAnimationState(AnimationState.Attack);
        Rpc(nameof(SyncAnimationState), (int)AnimationState.Attack);
		RunCoroutine(OrtodoxCrossPattern());
	}
	private IEnumerator OrtodoxCrossPattern()
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
				SpawnBullet(dir, "Boss");
			}	
			yield return ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
		}
	}


	#endregion Special Patterns
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

	public Vector2 GetOtherPlayerPosition()
	{
		long playerId = 1;
		foreach (long val in NetworkingManager.Instance.playerIds)
		{
			if (val != GetMultiplayerAuthority())
			{
				playerId = val;
				break;
			}
		}
		var otherPlayerNode = GetTree().Root.GetNodeOrNull<Node2D>($"/root/MainScene/{playerId}");
		if (otherPlayerNode != null)
		{
			return otherPlayerNode.Position;
		}
		GD.PrintErr($"Player with ID {playerId} not found!");
		return Vector2.Zero;
	}
}
