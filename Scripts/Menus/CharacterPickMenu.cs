using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CharacterPickMenu : Control
{
	[Export] private PackedScene _nextScene; //game scene

	[Export] private ItemList _playersList;
	[Export] private Control _radioButtonsHolder;
	[Export] private Button _readyButton;

	private bool _characterChosen = false;

    private bool _thisPlayerReady = false;
    private Godot.Collections.Dictionary<long, bool> _playersStates = new Godot.Collections.Dictionary<long, bool>();

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
        //GD.Print(string.Join( " ", NetworkingManager.Instance.playerIds ));
        UpdatePlayersList();
    }

    private void UpdatePlayersList()
    {
        _playersList.Clear();  
        foreach (long peerId in NetworkingManager.Instance.playerIds)
        {
            _playersList.AddItem($"Player {peerId} Ready: {(_playersStates.ContainsKey(peerId) ? _playersStates[peerId] : false)}");
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
            NetworkingManager.Instance.PlayerClasses[Multiplayer.GetUniqueId()] = true;
            foreach (var player in NetworkingManager.Instance.PlayerClasses)
            {
                if (player.Key != Multiplayer.GetUniqueId())
                {
                    NetworkingManager.Instance.PlayerClasses[player.Key] = false;
                }
            }
            GD.Print("Boss chosen");
			_characterChosen = true;
        }
        if (chosenButton.Name == "Hero")
        {
			NetworkingManager.Instance.PlayerClass = false;
            NetworkingManager.Instance.PlayerClasses[Multiplayer.GetUniqueId()] = false;
            foreach (var player in NetworkingManager.Instance.PlayerClasses)
            {
                if (player.Key != Multiplayer.GetUniqueId())
                {
                    NetworkingManager.Instance.PlayerClasses[player.Key] = true;
                }
            }
            GD.Print("Hero chosen");
			_characterChosen = true;
        }
        NetworkingManager.Instance.Rpc(nameof(NetworkingManager.Instance.SyncPlayerClasses), NetworkingManager.Instance.PlayerClasses);
    }

	public void OnReadyButtonPressed()
	{
		if (_characterChosen)
		{
            _thisPlayerReady = true;
            _playersStates.Add(Multiplayer.GetUniqueId(), _thisPlayerReady);
            Rpc(nameof(SyncPlayerStates), _playersStates);
            SyncPlayerStates(_playersStates);
            bool allReady = NetworkingManager.Instance.playerIds.All(peerId => _playersStates.ContainsKey(peerId) && _playersStates[peerId]);
            if (allReady)
            {
                Rpc(nameof(BeginGame));
                BeginGame();
            }
		}
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void BeginGame()
    {
        GetTree().ChangeSceneToPacked(_nextScene);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SyncPlayerStates(Godot.Collections.Dictionary<long, bool> syncedPlayerStates)
    {
        _playersStates = syncedPlayerStates;
        GD.Print("States synced");
        foreach (var state in _playersStates)
        {
            GD.Print($"Player {state.Key}: Ready - {state.Value}");
        }
    }

	
}
