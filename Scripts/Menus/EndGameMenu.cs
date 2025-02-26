using Godot;
using System;

public partial class EndGameMenu : Control
{
	[Export] private PackedScene _characterMenuScene;
	[Export] private PackedScene _mainMenuScene;

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
