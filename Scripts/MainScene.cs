using Godot;
using System;

public partial class MainScene : Node2D
{
	public override void _Ready()
	{
		long id = Multiplayer.GetUniqueId();
		if (id == 1)
		{
			SpawnManager.Instance.SpawnForHost();
			GD.Print("Host spawned");
		}
		else
		{
			SpawnManager.Instance.SpawnForConnected(id);
			GD.Print("Player spawned");
		}
	}
}
