using Godot;

public partial class ExplosiveBullet : Bullet
{
    [Export] public float ExplosionRadius = 100f;
    [Export] public int ExplosionDamage = 20;

    public override void _Ready()
    {
        base._Ready();
        Damage = 15;
        Speed = 150;
        LifeTime = 5.0f;
        //BulletTexture = GD.Load<Texture2D>("res://Textures/Bullets/ExplosiveBullet.png");
    }

    protected override void OnBodyEntered(Node body)
    {
        if (_damageApplied) return;

        if (body is BasicCharacter player)
        {
            if (player.GetMultiplayerAuthority() != HolderID)
            {
                Explode();
                _damageApplied = true;
                QueueFree();
            }
        }
    }

    private void Explode()
    {
        var explosionArea = GetNode<Area2D>("ExplosionArea");
        explosionArea.GlobalPosition = GlobalPosition;
        explosionArea.Monitoring = true;

        foreach (var body in explosionArea.GetOverlappingBodies())
        {
            if (body is BasicCharacter character)
            {
                character.Rpc(nameof(character.TakeDamage), ExplosionDamage);
            }
        }
    }

    protected override void DestroyBulletAfterTime()
    {
        Explode();
        QueueFree();
    }
}