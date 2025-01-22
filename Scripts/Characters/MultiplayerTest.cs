using Godot;
using System;

public partial class MultiplayerTest : Node2D
{
    private const int serverPort = 135;
    private ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
    [Export] PackedScene playerScene;

    private System.Collections.Generic.Dictionary<int, Node2D> playerNodes = new System.Collections.Generic.Dictionary<int, Node2D>();

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"Peer connected: {id}");
        if (Multiplayer.IsServer())
		{
            foreach (var playerNodePair in playerNodes)
			{
                if (playerNodePair.Key != (int)id) 
				{
                    RpcId((int)id, nameof(_SyncPlayerPosition), playerNodePair.Key, playerNodePair.Value.Position);
                }
            }
            RpcId(id, nameof(_createPlayerOnClient), (int)id);
            _addPlayer((int)id);
        }
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Peer disconnected: {id}");
        if (playerNodes.ContainsKey((int)id))
		{
			playerNodes[(int)id].QueueFree();
			playerNodes.Remove((int)id);
		}
    }
    
    public void _OnHostButtonPressed()
    {
        GD.Print($"Host button pressed");
        //if (Multiplayer.IsServer()) return;
        peer.CreateServer(serverPort);
        Multiplayer.MultiplayerPeer = peer;
        _addPlayer(Multiplayer.GetUniqueId());
    }

    public void _OnJoinButtonPressed()
    {
        GD.Print($"Join button pressed");
        //if (Multiplayer.IsServer()) return;
        peer.CreateClient("localhost", serverPort);
        Multiplayer.MultiplayerPeer = peer;
    }

    private void _addPlayer(int id)
    {
        if (playerScene == null)
        {
            GD.PrintErr("Error: Player scene not assigned.");
            return;
        }
         
         if (playerNodes.ContainsKey(id)) return;
        
        GD.Print($"_addPlayer called, id = {id}, Server = {Multiplayer.IsServer()}");
        
        Node2D player = playerScene.Instantiate() as Node2D;

        if (player == null)
		{
			GD.PrintErr("Error: Player scene is not Node2D or a descendant");
			return;
        }

        player.Name = id.ToString();
        playerNodes.Add(id, player);
        AddChild(player);

        if (Multiplayer.IsServer() && Multiplayer.GetUniqueId() != id)
        {
            GD.Print($"Server calling RPC for player ID = {id}");
            RpcId(id, nameof(_createPlayerOnClient), id);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void _createPlayerOnClient(int id)
    {
        GD.Print($"_createPlayerOnClient called, id = {id}, Server = {Multiplayer.IsServer()}");
        _addPlayer(id);
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
}