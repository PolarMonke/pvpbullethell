using Godot;
using System;
public partial class ProgressButton : TextureProgressBar
{
    public enum Skills
    {
        Circle,
        Bomb,
        SineWave,
        Cross,
        SpiralCross
    }

    [Export] public Skills Skill;
    [Export] public float Cooldown;

    private BossCharacter _bossCharacter;
    private Button _button;
    private float _currentCooldown = 0.0f;

    public override void _Ready()
    {
        _button = GetNode<Button>("Button");
        _bossCharacter = GetNode<BossCharacter>($"/root/MainScene/SpawnManager/{GetMultiplayerAuthority()}");

        _button.Connect("pressed", new Callable(this, nameof(OnButtonPressed)));

        MaxValue = Cooldown;
        Value = Cooldown;
        _currentCooldown = Cooldown;
    }

    public override void _Process(double delta)
    {
        if (_currentCooldown < Cooldown)
        {
            _currentCooldown += (float)delta;

            Value = _currentCooldown;

            if (_currentCooldown >= Cooldown)
            {
                _button.Disabled = false;
                Value = Cooldown;
            }
        }
    }

    private void OnButtonPressed()
    {
        if (_currentCooldown >= Cooldown && _bossCharacter != null)
        {
            switch (Skill)
            {
                case Skills.Circle:
                    _bossCharacter.ShootCircle();
                    break;
                case Skills.Bomb:
                    _bossCharacter.ShootExplosiveBullets();
                    break;
                case Skills.SineWave:
                    _bossCharacter.ShootSineWave();
                    break;
                case Skills.Cross:
                    _bossCharacter.ShootCross();
                    break;
                case Skills.SpiralCross:
                    _bossCharacter.ShootSpiralCross();
                    break;
            }

            _currentCooldown = 0;
            _button.Disabled = true;
        }
    }
}