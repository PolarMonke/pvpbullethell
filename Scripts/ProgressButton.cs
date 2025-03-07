using Godot;
using System;
using System.Collections;
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
        RunCoroutine(Spawn());
    }
    private IEnumerator Spawn()
    {
        yield return ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        _button = GetNode<Button>("Button");

        _bossCharacter = GetNode<BossCharacter>($"/root/MainScene/SpawnManager/{NetworkingManager.Instance.GetBoss()}");

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
                    if (_bossCharacter != null)
                    {
                        GD.Print("Shooting circle");
                        _bossCharacter.ShootCircle();
                    }
                    else
                    {
                        GD.Print("Boss is null");
                    }
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
    private async void RunCoroutine(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is Godot.SignalAwaiter signalAwaiter)
            {
                await signalAwaiter;
            }
        }
    }
}