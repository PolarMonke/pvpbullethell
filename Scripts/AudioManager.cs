using Godot;
using System;
using System.Collections.Generic;

public partial class AudioManager : Node2D
{
	[Export] private AudioStreamPlayer _audioPlayer;
	List<AudioStreamMP3> _audioSources = new List<AudioStreamMP3>();
	public override void _Ready()
	{
		_audioSources.Add(GD.Load<AudioStreamMP3>("res://Sounds/soundtrack1.mp3"));
		_audioSources.Add(GD.Load<AudioStreamMP3>("res://Sounds/soundtrack2.mp3"));
		_audioSources.Add(GD.Load<AudioStreamMP3>("res://Sounds/soundtrack3.mp3"));
		_audioPlayer.Stream = _audioSources[new Random().Next(0,2)];
		_audioPlayer.Play();
	}

}
