using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SpawnManager : Node2D
{
	public static SpawnManager Instance { get; private set; }
    [Export] private PackedScene heroScene;
    [Export] private PackedScene bossScene;
    
    private Dictionary<long, Node2D> _players = new();
    private bool _playerClassSwitch;

	public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
        }
        else
        {
            Instance = this;
        }
        _playerClassSwitch = NetworkingManager.Instance.PlayerClass;
    }

    public void SpawnExistingPlayersForNewcomer(long newPlayerId)
    {
        if (!Multiplayer.IsServer()) return;
        
        foreach (var existingId in _players.Keys)
        {
            if (existingId != newPlayerId)
            {
                RpcId((int)newPlayerId, nameof(CreatePlayerForClient), existingId, _players[existingId].Position);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SpawnPlayer(long id)
    {
        if (Multiplayer.IsServer())
        {
            CreatePlayer(id);
            Rpc(nameof(CreatePlayerForAll), id);
        }
    }

    private void CreatePlayer(long id)
    {
        if (_players.ContainsKey(id)) return;

        var playerType = _playerClassSwitch ? bossScene : heroScene;
        var player = playerType.Instantiate<Node2D>();
        _playerClassSwitch = !_playerClassSwitch;

        player.Name = $"{id}";
        player.Position = new Vector2(200, 200);
        AddChild(player);
        _players.Add(id, player);
    }

    [Rpc]
    private void CreatePlayerForAll(long id)
    {
        if (Multiplayer.IsServer()) return;
        CreatePlayer(id);
    }

    [Rpc]
    private void CreatePlayerForClient(long id, Vector2 position)
    {
        if (!_players.ContainsKey(id))
        {
            CreatePlayer(id);
            _players[id].Position = position;
        }
    }
}
