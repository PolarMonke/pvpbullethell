using Godot;

public partial class EndGameMenu : Control
{
    [Export] private PackedScene _characterMenuScene;
    [Export] private PackedScene _mainMenuScene;

    public override void _EnterTree()
    {
        _characterMenuScene = GD.Load<PackedScene>("res://Scenes/characterPickMenu.tscn");
        _mainMenuScene = GD.Load<PackedScene>("res://Scenes/mainScene.tscn");

        if (_characterMenuScene == null || _mainMenuScene == null)
        {
            GD.PrintErr("One or more scenes failed to load!");
            return;
        }

        ConnectButton("Panel/CharacterMenuButton", nameof(OnCharacterMenuButtonPressed));
        ConnectButton("Panel/MainMenuButton", nameof(OnMainMenuButtonPressed));
    }

    private void ConnectButton(string buttonPath, string methodName)
    {
        var button = GetNodeOrNull<Button>(buttonPath);
        if (button == null)
        {
            GD.PrintErr($"{buttonPath} not found!");
            return;
        }

        var callable = new Callable(this, methodName);
        if (!HasMethod(methodName))
        {
            GD.PrintErr($"Method {methodName} does not exist!");
            return;
        }
        var error = button.Connect("pressed", callable);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to connect {buttonPath} to {methodName}: {error}");
        }
    }

    public void OnCharacterMenuButtonPressed()
    {
        GD.Print("CharacterMenuButton pressed");
        GetTree().ChangeSceneToPacked(_characterMenuScene);
    }

    public void OnMainMenuButtonPressed()
    {
        NetworkingManager.Instance.LeaveServer();
        GetTree().ChangeSceneToPacked(_mainMenuScene);
    }
}