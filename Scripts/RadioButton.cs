using Godot;
using System;

public partial class RadioButton : CheckBox
{
    [Export] public string RadioGroupID;

    public override void _Ready()
    {
        Toggled += OnToggled;
    }

    private void OnToggled(bool buttonPressed)
    {
        if (!IsMultiplayerAuthority())
		{
			return;
		}
        Rpc(nameof(RemoteInvertSelection), RadioGroupID, buttonPressed);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RemoteInvertSelection(string targetRadioGroupID, bool senderState)
    {
        if (RadioGroupID == targetRadioGroupID)
		{
			return;
		}
        ButtonPressed = !senderState;
    }
}