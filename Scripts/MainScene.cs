using Godot;
using System;

public partial class MainScene : Node2D
{
	public override void _Ready()
	{
		long id = Multiplayer.GetUniqueId();
		if (id == 1)
		{
			SpawnManager.Instance.RpcId(1, nameof(SpawnManager.SpawnPlayer), id);
		}
		else
		{
			SpawnManager.Instance.SpawnExistingPlayersForNewcomer(id);
			SpawnManager.Instance.RpcId(id, nameof(SpawnManager.SpawnPlayer), id);
		}
	}
}
