using Godot;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    [Export] PackedScene _gameOverScreen;
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

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void EndGame()
    {
        Rpc(nameof(ShowEndGameScreen));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ShowEndGameScreen()
    {
        var endGameInstance = _gameOverScreen.Instantiate();
        bool lost = NetworkingManager.Instance.PlayerStatuses[GetMultiplayerAuthority()];
        if (endGameInstance.GetNode("Panel").GetNode("Label") is Label label)
        {
            if (lost)
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