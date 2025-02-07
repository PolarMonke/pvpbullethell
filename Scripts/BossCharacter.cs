using Godot;
using System;

public partial class BossCharacter : BasicCharacter
{
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
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    protected override void Shoot()
    {
        GD.Print($"Server shooting bullet for player {Name}");
        if (bulletScene == null || bulletSpawn == null)
        {
            GD.PrintErr("Assign bullet and bulletSpawn in editor");
            return;
        }
        SetAnimationState(AnimationState.Attack);
        Area2D bulletInstance = bulletScene.Instantiate<Area2D>();
        Vector2 directionToMouse = bulletSpawn.GlobalPosition.DirectionTo(GetGlobalMousePosition()).Normalized();
        bulletInstance.GlobalPosition = bulletSpawn.GlobalPosition;

        var bulletScript = bulletInstance as Bullet;
        if(bulletScript != null)
        {
            bulletScript.HolderID = Multiplayer.GetUniqueId();
            bulletScript.SetDirection(directionToMouse);
        }
        GetParent().AddChild(bulletInstance);
    }
}
