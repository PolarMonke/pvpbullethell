using Godot;
using System;

public partial class BasicCharacter : CharacterBody2D
{
    [Export] private float _speed = 400.0f;
	private Vector2 _previousPosition;

    public override void _EnterTree()
    {
        if (Name != null)
        {
            SetMultiplayerAuthority(Int32.Parse(Name));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
			_previousPosition = Position;
            Vector2 velocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * _speed;
            Velocity = velocity;
            MoveAndSlide();

			for (int i = 0; i < GetSlideCollisionCount(); i++) { //TODO: Optimize
				KinematicCollision2D collision = GetSlideCollision(i);
				if (collision.GetCollider() is BasicCharacter otherPlayer)
				{
					Vector2 pushVector = Position - _previousPosition;
					Position -= pushVector;
				}
			}

            Rpc(nameof(SetPlayerPosition), Position);
        }
    }

   [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SetPlayerPosition(Vector2 position)
    {
		if (!IsMultiplayerAuthority())
		{
			Position = position;
		}
    }
}
