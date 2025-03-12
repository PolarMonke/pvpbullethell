using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class NetworkingManager : Node
{
    public static NetworkingManager Instance { get; private set; }

    [Export] private int _port = 135;
    [Export] private PackedScene heroScene;
    [Export] private PackedScene bossScene;

    public bool PlayerClass = false; //false - hero, true - boss
    private ENetMultiplayerPeer _peer = new ENetMultiplayerPeer();
    public List<long> playerIds = new List<long>();
    public Godot.Collections.Dictionary<long, bool> PlayerClasses = new Godot.Collections.Dictionary<long, bool>();
    [Signal]
    public delegate void PlayerClassesUpdatedEventHandler(Godot.Collections.Dictionary<long, bool> classes);

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

    }
    
    public long GetBoss()
    {
        foreach (var kvp in PlayerClasses)
        {
            if (kvp.Value == true)
            {
                return kvp.Key;
            }
        }
        return 1;
    }

    public void CreateServer()
    {
        GD.Print("Starting server");
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        _peer.CreateServer(_port);
        Multiplayer.MultiplayerPeer = _peer;
        _addPlayerID(Multiplayer.GetUniqueId());
        GD.Print("Server Started");
    }

    public void JoinServer(string address)
    {
        GD.Print("Attempting to connect to " + address);

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        _peer.CreateClient(address, _port);
        Multiplayer.MultiplayerPeer = _peer;
        GD.Print("Client connecting to " + address);
    }

    public void LeaveServer()
    {
        GD.Print("Leaving server...");

        if (Multiplayer.IsServer())
        {
            GD.Print("Server closed");
        }
        else
        {
            GD.Print("Client disconnected from server");
        }

        _peer.Close();
        GD.Print("Peer connection closed");

        playerIds.Clear();
        PlayerClasses.Clear();
        GD.Print("Player lists cleared");

        if (SpawnManager.Instance != null)
        {
            foreach (var playerNode in SpawnManager.Instance.playerNodes.Values)
            {
                playerNode.QueueFree();
            }
            SpawnManager.Instance.playerNodes.Clear();
            GD.Print("Player nodes cleared");
        }
        Multiplayer.MultiplayerPeer = null;
        GD.Print("Multiplayer peer reset");

        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
        GD.Print("Multiplayer signals disconnected");

        PauseMultiplayerDependentNodes();
    }

    private void PauseMultiplayerDependentNodes()
    {
        var multiplayerNodes = GetTree().GetNodesInGroup("MultiplayerDependent");
        foreach (var node in multiplayerNodes)
        {
            if (node is Node2D node2D)
            {
                node2D.SetProcess(false);
                node2D.SetPhysicsProcess(false);
            }
        }
        GD.Print("Paused multiplayer-dependent nodes");
    }
    private void OnPeerConnected(long id)
    {
        GD.Print($"Peer connected: {id}");
        if (Multiplayer.IsServer())
        {
            _addPlayerID(id);
            RpcId(id, nameof(SyncPlayerIDs), playerIds.ToArray());
        }
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Peer disconnected: {id}");
        if (SpawnManager.Instance.playerNodes.ContainsKey(id))
        {
            SpawnManager.Instance.playerNodes[id].QueueFree();
            SpawnManager.Instance.playerNodes.Remove(id);
            playerIds.Remove(id);
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void _addPlayerID(long id)
    {
        if (!playerIds.Contains(id))
        {
            playerIds.Add(id);
            PlayerClasses.Add(id, false);
            Rpc(nameof(SyncPlayerIDs), playerIds.ToArray());
            Rpc(nameof(SyncPlayerClasses), PlayerClasses);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SyncPlayerIDs(long[] ids)
    {
        playerIds = new List<long>(ids);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SyncPlayerClasses(Godot.Collections.Dictionary<long, bool> classes)
    {
        PlayerClasses = classes;
        EmitSignal(nameof(PlayerClassesUpdated), PlayerClasses); 
    }

}