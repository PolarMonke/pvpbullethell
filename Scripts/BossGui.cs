using Godot;
using System;

public partial class BossGui : Control
{
	private Area2D _area;
	public override void _Ready()
	{
		if (NetworkingManager.Instance.PlayerClass == true)
		{
			Visible = true;
			_area = GetNode<Area2D>("Area2D");
			_area.BodyEntered += OnPlayerEntered;
			_area.BodyExited += OnPlayerExited;
		}
		
	}
	private void OnPlayerEntered(Node2D body)
	{
		if (body is BasicCharacter)
		{
			Modulate = new Color(1f, 1f, 1f, 0.2f);
		}
	}
	private void OnPlayerExited(Node2D body)
	{
		if (body is BasicCharacter)
		{
			Modulate = new Color(1f, 1f, 1f, 1f);
		}
	}
}
