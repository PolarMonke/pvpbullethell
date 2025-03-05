using System.Collections;
using Godot;

public partial class ExplosiveBullet : Bullet
{
    [Export] public float ExplosionRadius = 10f;
    [Export] public int ExplosionDamage = 20;
    private float _explosionTime = 5;
    private float _areaIncrement = 0f;
    public override void _Ready()
    {
        LifeTime = 0.1f;
        _lifeTimer = new Timer();
        _lifeTimer.WaitTime = LifeTime;
        _lifeTimer.Autostart = true;
        AddChild(_lifeTimer);
        _lifeTimer.Start();
        _lifeTimer.Timeout += DestroyBulletAfterTime;

        if (BulletTexture != null)
        {
            var sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.Texture = BulletTexture;
        }
        Damage = 15;
        Speed = 60;

        _areaIncrement = ExplosionRadius / (0.3f / LifeTime);
    }

    protected override void OnBodyEntered(Node body)
    {
        if (_damageApplied) return;
        if (body.Name != HolderID.ToString())
        {
            if (body is BasicCharacter player)
            {
                if (body.Name != HolderID.ToString())
                {
                    player.Rpc(nameof(player.TakeDamage), Damage);
                    _damageApplied = true;
                    QueueFree();
                }
            }
        }
    }

    private IEnumerator Explode()
    {
        var explosionArea = GetNode<Area2D>("ExplosionArea");
        explosionArea.GlobalPosition = GlobalPosition;
        explosionArea.Monitoring = true;
        explosionArea.BodyEntered += OnBodyEntered;
        for (int i = 0; i < 3; i++)
        {
            explosionArea.Scale += new Vector2(_areaIncrement, _areaIncrement);
            yield return ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        }
        QueueFree();
    }

    protected override void DestroyBulletAfterTime()
    {
        _explosionTime -= LifeTime;
        AnimatedSprite2D sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        if ((int)_explosionTime % 2 == 0)
        {
            sprite.Modulate = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            sprite.Modulate = new Color(1f, 1f, 1f, 0.5f);
        }

        if (_explosionTime <= 0)
        {
            _lifeTimer.Stop();
            RunCoroutine(Explode());
        }
    }

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