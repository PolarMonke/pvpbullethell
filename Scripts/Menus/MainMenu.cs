using Godot;
using System;

public partial class MainMenu : Control
{
	public void _OnHostButtonPressed()
    {
        NetworkingManager.Instance.CreateServer();
    }

    public void _OnJoinButtonPressed()
    {
        NetworkingManager.Instance.JoinServer("localhost");
    }
}
