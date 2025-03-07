using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class EndGameMenu : Control
{
	[Export] private PackedScene _characterMenuScene;
	[Export] private PackedScene _mainMenuScene;

	public override void _Ready()
	{
		_characterMenuScene = GD.Load<PackedScene>("res://Scenes/characterPickMenu.tscn");
		_mainMenuScene = GD.Load<PackedScene>("res://Scenes/mainScene.tscn");
	}
	public void OnCharacterMenuButtonPressed()
	{
		GetTree().ChangeSceneToPacked(_characterMenuScene);
	}	
	public void OnMainMenuButtonPressed()
	{
		NetworkingManager.Instance.LeaveServer();
		GetTree().ChangeSceneToPacked(_mainMenuScene);
	}
}
