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
			//_shootingPatterns.Add(ShootSpray); // Add more patterns here
			//_shootingPatterns.Add(ShootTargeted);
        }
    }
	private void OnPatternTimerTimeout()
    {
        _shootingPatterns[_currentPatternIndex]?.Invoke();
        _currentPatternIndex = (_currentPatternIndex + 1) % _shootingPatterns.Count;
    }
	
	[Rpc(MultiplayerApi.RpcMode.Authority)]
    private void SpawnBullet(Vector2 direction)
    {
        if (bulletScene == null || bulletSpawn == null)
        {
            GD.PrintErr("Assign bullet and bulletSpawn in editor");
            return;
        }

        Area2D bulletInstance = bulletScene.Instantiate<Area2D>();
        bulletInstance.GlobalPosition = bulletSpawn.GlobalPosition;

        var bulletScript = bulletInstance as Bullet;
        if (bulletScript != null)
        {
            bulletScript.HolderID = Multiplayer.GetUniqueId();
            bulletScript.SetDirection(direction);
        }
        GetTree().Root.AddChild(bulletInstance);
        BulletManager.Instance.HandleBulletSpawned(bulletInstance, bulletInstance.GlobalPosition, direction);
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
        int bulletCount = 16;
        float angleIncrement = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = Mathf.DegToRad(i * angleIncrement);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnBullet(direction);
        }
    }

    private void ShootSpray()
    {
        int bulletCount = 5;
        float angleSpread = Mathf.DegToRad(30);

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = Mathf.Lerp(-angleSpread / 2, angleSpread / 2, (float)GD.RandRange(0.0f, 1.0f));
            Vector2 direction = Vector2.Right.Rotated(angle);

            SpawnBullet(direction);
        }
    }
    
    private void ShootTargeted()
    {
        BasicCharacter player = FindPlayer();
        if (player != null)
        {
            Vector2 direction = bulletSpawn.GlobalPosition.DirectionTo(player.GlobalPosition).Normalized();
            SpawnBullet(direction);
        }
    }

    #endregion Shooting Patterns

}
