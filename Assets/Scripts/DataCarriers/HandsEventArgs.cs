using System;

/// <summary>
/// EventArgs to hold an array of hands
/// </summary>
public class HandsEventArgs : EventArgs {
    public Hand[] Hands { get; set; }
}
