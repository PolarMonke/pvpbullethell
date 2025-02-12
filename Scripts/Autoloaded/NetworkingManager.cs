using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class NetworkingManager : Node
{
    public static NetworkingManager Instance { get; private set; }

    [Export] private int _port = 135;
    [Export] private PackedScene heroScene;
    [Export] private PackedScene bossScene;

    public bool PlayerClass = false; //false - hero, true - boss for now
    private ENetMultiplayerPeer _peer = new ENetMultiplayerPeer();

    private Dictionary<int, Node2D> playerNodes = new Dictionary<int, Node2D>();
    private List<int> playerIds = new List<int>();

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

    public void CreateServer()
    {
        GD.Print("Starting server");
        _peer.CreateServer(_port);
        Multiplayer.MultiplayerPeer = _peer;
        //_createPlayer(Multiplayer.GetUniqueId());
        GD.Print("Server Started");
    }

    public void JoinServer(string address)
    {
        GD.Print("Attempting to connect to " + address);
        _peer.CreateClient(address, _port);
        Multiplayer.MultiplayerPeer = _peer;
        GD.Print("Client connecting to " + address);
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"Peer connected: {id}");
        if (Multiplayer.IsServer())
        {
            foreach (int playerId in playerIds)
            {
                if (playerId != (int)id)
                {
                  RpcId((int)id, nameof(_createPlayer), playerId);
                  RpcId((int)id, nameof(_SyncPlayerPosition), playerId, playerNodes[playerId].Position);
                }
            }
           
            //_createPlayer((int)id);
        }
        else
        {
           //_createPlayer(Multiplayer.GetUniqueId());
        }
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Peer disconnected: {id}");
        if (playerNodes.ContainsKey((int)id))
        {
            playerNodes[(int)id].QueueFree();
            playerNodes.Remove((int)id);
            playerIds.Remove((int)id);
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
    
        if (playerNodes.ContainsKey(id)) return;

        GD.Print($"_addPlayer called, id = {id}, Server = {Multiplayer.IsServer()}");
        
        Node2D player = PlayerClass ? bossScene.Instantiate() as Node2D : heroScene.Instantiate() as Node2D;
        PlayerClass = !PlayerClass; //very hardcoded but okay for now

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
        playerNodes.Add(id, player);
        AddChild(player);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void _createPlayer(int id)
    {
       GD.Print($"_createPlayer called, id = {id}, Server = {Multiplayer.IsServer()}");
       if(!playerIds.Contains(id))
        {
            playerIds.Add(id);
            _addPlayer(id);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void _SyncPlayerPosition(int playerId, Vector2 position)
    {
        if (playerNodes.ContainsKey(playerId))
        {
            playerNodes[playerId].Position = position;
            GD.Print($"Syncing player pos {playerId}, pos = {position}");
        }
    }

    public long[] GetConnectedPeers()
    {
        return Multiplayer.GetPeers().Select(peer => (long)peer).ToArray();
    }

}