using Godot;
using System;
using System.Collections;

public partial class MainScene : Node2D
{
	public override void _Ready()
	{
		
		//RunCoroutine(SpawnPlayers());
	}
	private IEnumerator SpawnPlayers()
	{
		yield return ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
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
