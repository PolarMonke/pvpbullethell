using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SpawnManager : Node2D
{
	public static SpawnManager Instance { get; private set; }
    [Export] private PackedScene heroScene;
    [Export] private PackedScene bossScene;
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

    public void SpawnForHost()
    {
        _createPlayer(Multiplayer.GetUniqueId());
    }

    public void SpawnForConnected(long id)
    {
        GD.Print($"Peer connected: {id}");
        if (Multiplayer.IsServer())
        {
            foreach (int playerId in NetworkingManager.Instance.playerIds)
            {
                if (playerId != (int)id)
                {
                  RpcId((int)id, nameof(_createPlayer), playerId);
                  RpcId((int)id, nameof(NetworkingManager.Instance._SyncPlayerPosition), playerId, NetworkingManager.Instance.playerNodes[playerId].Position);
                }
            }
           
            _createPlayer((int)id);
        }
        else
        {
           _createPlayer(Multiplayer.GetUniqueId());
        }
    }

    private void _addPlayer(int id)
    {
        if (heroScene
 == null)
        {
            GD.PrintErr("Error: Player scene not assigned.");
            return;
        }
    
        if (NetworkingManager.Instance.playerNodes.ContainsKey(id)) return;

        GD.Print($"_addPlayer called, id = {id}, Server = {Multiplayer.IsServer()}");
        
        Node2D player = _playerClassSwitch ? bossScene.Instantiate() as Node2D : heroScene.Instantiate() as Node2D;
        _playerClassSwitch = !_playerClassSwitch;

        if (player is BasicCharacter playerScript)
        {
            playerScript.PlayerFiredBullet += BulletManager.Instance.HandleBulletSpawned;
        }
        else
        {
            GD.PrintErr("Player script not found");
        }

        if (player == null)
        {
            GD.PrintErr("Error: Player scene is not Node2D or a descendant");
            return;
        }
        player.Name = id.ToString();
        NetworkingManager.Instance.playerNodes.Add(id, player);
        AddChild(player);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void _createPlayer(int id)
    {
        GD.Print($"_createPlayer called, id = {id}, Server = {Multiplayer.IsServer()}");
        if(!NetworkingManager.Instance.playerIds.Contains(id))
        {
            NetworkingManager.Instance.playerIds.Add(id);
            _addPlayer(id);
        }
        else
        {
            _addPlayer(id);
        }
    }

    // public void SpawnExistingPlayersForNewcomer(long newPlayerId)
    // {
    //     if (!Multiplayer.IsServer()) return;
        
    //     foreach (var existingId in _players.Keys)
    //     {
    //         if (existingId != newPlayerId)
    //         {
    //             RpcId((int)newPlayerId, nameof(CreatePlayerForClient), existingId, _players[existingId].Position);
    //         }
    //     }
    // }

    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    // public void SpawnPlayer(long id)
    // {
    //     if (Multiplayer.IsServer())
    //     {
    //         CreatePlayer(id);
    //         Rpc(nameof(CreatePlayerForAll), id);
    //     }
    // }

    // private void CreatePlayer(long id)
    // {
    //     if (_players.ContainsKey(id)) return;

    //     var playerType = _playerClassSwitch ? bossScene : heroScene;
    //     var player = playerType.Instantiate<Node2D>();
    //     _playerClassSwitch = !_playerClassSwitch;

    //     player.Name = $"{id}";
    //     player.Position = new Vector2(200, 200);
    //     AddChild(player);
    //     _players.Add(id, player);
    // }

    // [Rpc]
    // private void CreatePlayerForAll(long id)
    // {
    //     if (Multiplayer.IsServer()) return;
    //     CreatePlayer(id);
    // }

    // [Rpc]
    // private void CreatePlayerForClient(long id, Vector2 position)
    // {
    //     if (!_players.ContainsKey(id))
    //     {
    //         CreatePlayer(id);
    //         _players[id].Position = position;
    //     }
    // }
}
