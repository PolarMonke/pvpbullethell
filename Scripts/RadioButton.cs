using Godot;
using System;

public partial class RadioButton : CheckBox
{
    public override void _Ready()
    {
        Toggled += OnToggled;
    }

    private void OnToggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            foreach (Node child in GetParent().GetChildren())
            {
                if (child is RadioButton otherRadioButton && otherRadioButton != this)
                {
                    otherRadioButton.ButtonPressed = false;
                }
            }
            Rpc(nameof(RemoteUpdateSelection), Multiplayer.GetUniqueId(), Name);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RemoteUpdateSelection(long senderID, string selectedButtonName)
    {
        if (Multiplayer.GetUniqueId() != senderID)
        {
            foreach (Node child in GetParent().GetChildren())
            {
                if (child is RadioButton otherRadioButton)
                {
                    otherRadioButton.ButtonPressed = !(otherRadioButton.Name == selectedButtonName);
                }
            }
        }
    }
}