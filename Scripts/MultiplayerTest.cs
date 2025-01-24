using Godot;
using System;

public partial class MultiplayerTest : Node2D
{
    public void _OnHostButtonPressed()
    {
        //if (Multiplayer.IsServer()) return;
        NetworkingManager.Instance.CreateServer();
    }

    public void _OnJoinButtonPressed()
    {
        //if (Multiplayer.IsServer()) return;
        NetworkingManager.Instance.JoinServer("localhost");
    }
}