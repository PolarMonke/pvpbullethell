using Godot;
using System;

public partial class MainMenu : Control
{
    [Export] private PackedScene _nextScene; 
    public void _OnHostButtonPressed()
    {
        NetworkingManager.Instance.CreateServer();
        GetTree().ChangeSceneToPacked(_nextScene);
    }

    public void _OnJoinButtonPressed()
    {
        NetworkingManager.Instance.JoinServer("localhost");
        GetTree().ChangeSceneToPacked(_nextScene);
    }
}
