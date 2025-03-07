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

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
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
        _peer.CreateServer(_port);
        Multiplayer.MultiplayerPeer = _peer;
        _addPlayerID(Multiplayer.GetUniqueId());
        GD.Print("Server Started");
    }

    public void JoinServer(string address)
    {
        GD.Print("Attempting to connect to " + address);
        _peer.CreateClient(address, _port);
        Multiplayer.MultiplayerPeer = _peer;
        GD.Print("Client connecting to " + address);
    }

    public void LeaveServer()
    {
        if (Multiplayer.IsServer())
        {
            _peer.Close();
            GD.Print("Server closed");
        }
        else
        {
            _peer.Close();
            GD.Print("Client disconnected from server");
        }
        playerIds.Clear();
        PlayerClasses.Clear();
        SpawnManager.Instance.playerNodes.Clear();

        Multiplayer.MultiplayerPeer = null;
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