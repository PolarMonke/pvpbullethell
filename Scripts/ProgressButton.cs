using Godot;
using System;
public partial class ProgressButton : TextureProgressBar
{
    public enum Skills
    {
        Circle,
        Bomb,
        SineWave,
        Spiral,
        SpiralCross
    }

    [Export] public Skills Skill;
    [Export] public float Cooldown;

    private BossCharacter _bossCharacter;
    private Button _button;
    private float _remainingCooldown = 0.0f;

    public override void _Ready()
    {
        _button = GetNode<Button>("Button");
        _bossCharacter = GetNode<BossCharacter>($"/root/MainScene/SpawnManager/{GetMultiplayerAuthority()}");

        _button.Connect("pressed", new Callable(this, nameof(OnButtonPressed)));

        MaxValue = Cooldown;
        Value = Cooldown;
    }

    public override void _Process(double delta)
    {
        if (_remainingCooldown > 0)
        {
            _remainingCooldown -= (float)delta;

            Value = _remainingCooldown;

            if (_remainingCooldown <= 0)
            {
                _button.Disabled = false;
                Value = Cooldown;
            }
        }
    }

    private void OnButtonPressed()
    {
        if (_remainingCooldown <= 0 && _bossCharacter != null)
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
                case Skills.Spiral:
                    _bossCharacter.ShootSpiral();
                    break;
                case Skills.SpiralCross:
                    _bossCharacter.ShootSpiralCross();
                    break;
            }

            _remainingCooldown = Cooldown;
            _button.Disabled = true;
        }
    }
}