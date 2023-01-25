using System;

public class EndRoundEventArgs : EventArgs {
    public Deck Deck { get; private set; }

    public EndRoundEventArgs(Deck deck) {
        Deck = deck;
    }
}