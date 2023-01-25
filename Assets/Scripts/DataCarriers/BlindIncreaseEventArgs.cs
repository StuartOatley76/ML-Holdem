using System;

/// <summary>
/// Event args for blind increase
/// </summary>
public class BlindIncreaseEventArgs : EventArgs {
    public int SmallBlind { get; private set; }

    public BlindIncreaseEventArgs(int smallBlind) {
        SmallBlind = smallBlind;
    }
}