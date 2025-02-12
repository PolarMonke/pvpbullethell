using Godot;
using Godot.NativeInterop;
using System;

public partial class CharacterPickMenu : Control
{
	[Export] private PackedScene _nextScene; //game scene

	[Export] private ItemList _playersList;
	[Export] private Control _radioButtonsHolder;
	[Export] private Button _readyButton;

	private bool _characterChosen = false;

	public override void _Ready()
	{
		foreach (Node child in _radioButtonsHolder.GetChildren())
        {
            if (child is RadioButton radioButton)
            {
                radioButton.Toggled += (bool pressed) => { if (pressed) HandleOptionChosen(radioButton); }; 
			}
		}

		_readyButton.Pressed += OnReadyButtonPressed;

        UpdatePlayersList();
	}

    public override void _Process(double delta)
    {
        UpdatePlayersList();
    }

    private void UpdatePlayersList()
    {
        _playersList.Clear();
        foreach (int peerId in NetworkingManager.Instance.GetConnectedPeers())
        {
            _playersList.AddItem($"Player {peerId}");
        }
    }
	public void HandleOptionChosen(RadioButton chosenButton)
    {
        if (chosenButton == null)
        {
            GD.PrintErr("No option chosen");
			_characterChosen = false;
            return;
        }
        if (chosenButton.Name == "Boss")
        {
			NetworkingManager.Instance.PlayerClass = true; //false - hero, true - boss for now
            GD.Print("Boss chosen");
			_characterChosen = true;
        }
        if (chosenButton.Name == "Hero")
        {
			NetworkingManager.Instance.PlayerClass = false;
            GD.Print("Hero chosen");
			_characterChosen = true;
        }
		
    }

	public void OnReadyButtonPressed()
	{
		if (_characterChosen)
		{

			GetTree().ChangeSceneToPacked(_nextScene);
		}
	}

	
}
