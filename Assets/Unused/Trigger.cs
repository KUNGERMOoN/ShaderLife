public class Trigger
{
    public bool Triggered { get; private set; }

    public void Execute()
    {
        Triggered = true;
    }

    public bool Receive()
    {
        if (Triggered)
        {
            Triggered = false;
            return true;
        }
        else
        {
            return false;
        }
    }
}
