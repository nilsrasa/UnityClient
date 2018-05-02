//Single command UI element

public class CommandElement : GazeButton
{
    public string Command { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Command = _buttonText.text;
    }

    protected override void Activate()
    {
        base.Activate();
        CommandListController.Instance.OnCommandActivated(Command);
    }

    public void Initialize(string commandText)
    {
        Command = commandText;
        _buttonText.text = commandText;
    }
}