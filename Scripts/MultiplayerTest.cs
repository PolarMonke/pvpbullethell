using Godot;
using System;

public partial class MultiplayerTest : Node2D
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