using Godot;

public partial class BulletManager : Node2D
{
    public static BulletManager Instance;
    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
        }
        else
        {
            Instance = this;
        }
    }
    public void HandleBulletSpawned(Area2D bullet, Vector2 position, Vector2 direction)
    {
        AddChild(bullet);
        bullet.GlobalPosition = position;

        if (bullet is Bullet bulletScript)
        {
        	bulletScript.SetDirection(direction);
        }
        else
        {
        	GD.PrintErr("Error: Could not get Bullet Script");
        }
    }
}