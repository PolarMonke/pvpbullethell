using System.Collections;
using System.Xml.Serialization;
using Godot;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    [Export] PackedScene _gameOverScreen;
    private bool _lost = false;
    private bool _endGameScreenShown = false;
    public override void _EnterTree()
    {
        if (Instance != null)
        {
            QueueFree();
        }
        else
        {
            Instance = this;
        }
    }

    public void Lose()
    {
        _lost = true;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void EndGame()
    {
        Rpc(nameof(ShowEndGameScreen));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)] //, CallLocal = true
    private void ShowEndGameScreen()
    {
        if (_endGameScreenShown)
        {
            return;
        }
        _endGameScreenShown = true;

        var endGameInstance = GetNode<Control>("/root/MainScene/GameOverMenu");
        endGameInstance.Visible = true;
        if (endGameInstance.GetNode("Panel").GetNode("Label") is Label label)
        {
            if (_lost)
            {
                label.Text = "You lost!";
            }
            else
            {
                label.Text = "You won!";
            }
        }
        var mainScene = GetTree().Root.GetNode<Node>("MainScene");
        GetTree().Paused = true;
    }
}