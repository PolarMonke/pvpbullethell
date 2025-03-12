using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class AudioManager : Node2D
{
	[Export] private AudioStreamPlayer _audioPlayer;
	List<AudioStreamMP3> _audioSources = new List<AudioStreamMP3>();
	public override void _Ready()
	{
		_audioPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
		_audioSources.Add(GD.Load<AudioStreamMP3>("res://Sounds/soundtrack1.mp3"));
		_audioSources.Add(GD.Load<AudioStreamMP3>("res://Sounds/soundtrack2.mp3"));
		_audioSources.Add(GD.Load<AudioStreamMP3>("res://Sounds/soundtrack3.mp3"));
		if (GetMultiplayerAuthority() == 1)
		{
			int index = new Random().Next(0,2);
			Rpc(nameof(PlaySoundtrack), index);
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void PlaySoundtrack(int index)
	{
		_audioPlayer.Stream = _audioSources[index];
		_audioPlayer.Play();
	}

}
