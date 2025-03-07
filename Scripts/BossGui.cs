using Godot;
using System;

public partial class BossGui : Control
{
	public override void _Ready()
	{
		if (NetworkingManager.Instance.PlayerClass == true)
		{
			Visible = true;
		}
	}
}
