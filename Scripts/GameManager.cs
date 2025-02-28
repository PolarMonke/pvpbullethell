using System.Xml.Serialization;
using Godot;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    [Export] PackedScene _gameOverScreen;
    private bool _lost = false;
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ShowEndGameScreen()
    {
        var endGameInstance = _gameOverScreen.Instantiate();
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
        GetTree().Root.AddChild(endGameInstance);
        GetTree().Paused = true;
    }
}