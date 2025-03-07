using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class SpawnManager : Node2D
{
	public static SpawnManager Instance { get; private set; }
    [Export] private PackedScene heroScene;
    [Export] private PackedScene bossScene;
    [Export] private PackedScene bossGUIScene;
    private bool _playerClassSwitch;
    public Dictionary<long, Node2D> playerNodes = new Dictionary<long, Node2D>();
    

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

        if (Multiplayer.IsServer())
        {
            foreach (var playerId in NetworkingManager.Instance.playerIds)
            {
                bool isBoss = NetworkingManager.Instance.PlayerClasses[playerId];
                Rpc(nameof(SpawnPlayer), playerId, isBoss);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void SpawnPlayer(long id, bool isBoss)
    {
        if (playerNodes.ContainsKey(id)) return;

        Node2D player = isBoss ? bossScene.Instantiate<Node2D>() : heroScene.Instantiate<Node2D>(); 
        player.Name = id.ToString();
        player.Position = isBoss ? new Vector2(200, 0) : new Vector2(-200, 0);
        
        AddChild(player);
        playerNodes[id] = player;
        
        GD.Print($"Player {id} spawned as {(isBoss ? "Boss" : "Hero")}");
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
            foreach (long playerId in NetworkingManager.Instance.playerIds)
            {
                if (playerId != id)
                {
                    RpcId(id, nameof(_createPlayer), playerId);
                    RpcId(id, nameof(_SyncPlayerPosition), playerId, playerNodes[playerId].Position);
                }
            }
           
            _createPlayer(id);
        }
        else
        {
           _createPlayer(Multiplayer.GetUniqueId());
        }
    }

    private void _addPlayer(long id)
    {
        //if (playerNodes.ContainsKey(id)) { return; }

        GD.Print($"_addPlayer called, id = {id}, Server = {Multiplayer.IsServer()}");

        Node2D player = _playerClassSwitch ? bossScene.Instantiate() as Node2D : heroScene.Instantiate() as Node2D;
        player.Position = _playerClassSwitch ? new Vector2(200, 0) : new Vector2(-200, 0);
        _playerClassSwitch = !_playerClassSwitch;

        if (player is BasicCharacter playerScript)
        {
            playerScript.PlayerFiredBullet += BulletManager.Instance.HandleBulletSpawned;
        }

        player.Name = id.ToString();
        playerNodes.Add(id, player);
        AddChild(player);

        GD.Print($"Player node created with name: {player.Name}, path: {player.GetPath()}");
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void _createPlayer(long id)
    {
        GD.Print($"_createPlayer called, id = {id}, Server = {Multiplayer.IsServer()}");
        if (!NetworkingManager.Instance.playerIds.Contains(id))
        {
            NetworkingManager.Instance.playerIds.Add(id);
        }
        _addPlayer(id);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void _SyncPlayerPosition(long playerId, Vector2 position)
    {
        if (playerNodes.ContainsKey(playerId))
        {
            GD.Print($"SetPlayerPosition called for player {Name}, position = {position}");
            playerNodes[playerId].Position = position;
        }
    }

}
