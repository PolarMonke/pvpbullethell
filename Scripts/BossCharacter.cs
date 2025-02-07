using Godot;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Collections.Generic;

public partial class BossCharacter : BasicCharacter
{
	[Export] public float PatternCooldown = 1.0f;
    private Timer _patternTimer;
    private List<Action> _shootingPatterns = new List<Action>();
    private int _currentPatternIndex = 0;


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

			_patternTimer = new Timer();
			_patternTimer.WaitTime = PatternCooldown;
			_patternTimer.OneShot = false;
			AddChild(_patternTimer);
			_patternTimer.Timeout += OnPatternTimerTimeout;
			_patternTimer.Start();

			_shootingPatterns.Add(ShootCircle);
			_shootingPatterns.Add(ShootCross);
			_shootingPatterns.Add(ShootSpiral);
        }
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
			bulletScript.HolderID = Multiplayer.GetUniqueId();
			bulletScript.SetDirection(direction);
		}

		BulletManager.Instance.HandleBulletSpawned(bulletInstance, bulletInstance.GlobalPosition, direction, Multiplayer.GetUniqueId());
	}
    private BasicCharacter FindPlayer()
    {
        foreach (var node in GetTree().GetNodesInGroup("player"))
        {
            if (node is BasicCharacter player)
            {
                return player;
            }
        }
        return null;
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

	private float _spiralAngle = 0f;
	private void ShootSpiral()
	{
		GD.Print("Shooting Spiral");
		int bulletCount = 8;
		_spiralAngle += Mathf.DegToRad(45);

		for (int i = 0; i < bulletCount; i++)
		{
			float angle = _spiralAngle + Mathf.DegToRad(i * 45);
			Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			SpawnBullet(dir);
		}
		Rpc(nameof(SyncSpiralAngle), _spiralAngle);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void SyncSpiralAngle(float angle)
	{
		_spiralAngle = angle;
	}

	private void ShootCross()
	{
		GD.Print("Shooting Cross");

		int rowCount = 8;
		int sideCount = 4;

		for (int i = 0; i < sideCount; i++)
		{
			float angle = Mathf.DegToRad(i * 90);
			for (int j = 0; j < rowCount; j++)
			{
				Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
				SpawnBullet(dir);
			}	
		}
	}

    #endregion Shooting Patterns

}
