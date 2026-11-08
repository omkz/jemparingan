// A tracked gesture must start from neutral after any interruption. In particular,
// reacquiring a held string and subsequently opening it is not a fresh shot.
public sealed class JemparinganQuestGestureGate
{
    private bool _awaitNeutral = true;

    public void Interrupt() { _awaitNeutral = true; }

    public bool Accept(bool valid, bool bowPinched, bool stringPinched)
    {
        if (!valid || (!bowPinched && stringPinched))
        {
            Interrupt();
            return false;
        }
        if (_awaitNeutral)
        {
            if (bowPinched || stringPinched) return false;
            _awaitNeutral = false;
        }
        return true;
    }
}
