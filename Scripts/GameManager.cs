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
        GD.Print("Game ended");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ShowEndGameScreen()
    {
        var endGameInstance = _gameOverScreen.Instantiate();
        GetTree().Root.AddChild(endGameInstance);
        GetTree().Paused = true;
        GD.Print("Game over spawned");
        
    }
}