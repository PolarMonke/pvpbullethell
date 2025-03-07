using Godot;
using System;
using System.Collections;

public partial class BossGui : Control
{
	private Area2D _area;
	public override void _Ready()
	{
		RunCoroutine(Spawn());
	}
	private IEnumerator Spawn()
	{
		yield return ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
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
