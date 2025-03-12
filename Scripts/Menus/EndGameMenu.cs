using System.Collections;
using Godot;

public partial class EndGameMenu : Control
{
    [Export] private PackedScene _characterMenuScene;
    [Export] private PackedScene _mainMenuScene;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;
        _characterMenuScene = GD.Load<PackedScene>("res://Scenes/characterPickMenu.tscn");
        _mainMenuScene = GD.Load<PackedScene>("res://Scenes/mainMenu.tscn");

        if (_characterMenuScene == null || _mainMenuScene == null)
        {
            GD.PrintErr("One or more scenes failed to load!");
            return;
        }

        //ConnectButton("Panel/CharacterMenuButton", nameof(OnCharacterMenuButtonPressed));
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
        button.Pressed += () => GD.Print($"{buttonPath} pressed directly");
    }

    public void OnCharacterMenuButtonPressed()
    {
        GD.Print("CharacterMenuButton pressed");
        GetTree().ChangeSceneToPacked(_characterMenuScene);
    }

    public void OnMainMenuButtonPressed()
    {
        GD.Print("MainMenuButton pressed");
        GetTree().Paused = false;
        NetworkingManager.Instance.LeaveServer();
        RunCoroutine(ToMainMenu());
    }
    private IEnumerator ToMainMenu()
    {
        yield return ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout); 
        GetTree().ChangeSceneToFile("res://Scenes/mainMenu.tscn");
        
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