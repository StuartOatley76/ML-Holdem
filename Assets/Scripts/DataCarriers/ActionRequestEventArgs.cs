using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionRequestEventArgs : EventArgs
{
    public Bet Bet { get; private set; }
    public Board Board { get; private set; }
    public Pocket Pocket { get; set; }
    public int Stack { get; set; }

    public RoundStage Stage { get; private set; }
    public int NumberOfPlayersAtTable { get; private set; }
    public List<Bet> Bets { get; private set; }
    
    public int PotAmount { get; private set; }

    public int BigBlindAmount { get; private set; }
    public ActionRequestEventArgs(Bet bet, Board board, List<Bet> bets, 
        int potAmount, int bigBlindAmount, RoundStage stage) {
        Bet = bet;
        Board = board;
        Bets = bets;
        PotAmount = potAmount;
        BigBlindAmount = bigBlindAmount;
        Stage = stage;
    }
}

public class ScoredActionRequestEventArgs : ActionRequestEventArgs {

    public int Score { get; set; }
    public ScoredActionRequestEventArgs(Bet bet, Board board, List<Bet> bets, 
        int potAmount, int bigBlindAmount, RoundStage stage) : 
        base(bet, board, bets, potAmount, bigBlindAmount, stage) {
    }

    public ScoredActionRequestEventArgs(ActionRequestEventArgs e) : 
        base(e.Bet, e.Board, e.Bets, e.PotAmount, e.BigBlindAmount, e.Stage) {

    }
   

}

public class OutsActionEventArgs : ScoredActionRequestEventArgs {

    public int Outs { get; set; }
    public OutsActionEventArgs(Bet bet, Board board, List<Bet> bets,
        int potAmount, int bigBlindAmount, RoundStage stage) :
        base(bet, board, bets, potAmount, bigBlindAmount, stage) {
    }

    public OutsActionEventArgs(ActionRequestEventArgs e) :
        base(e.Bet, e.Board, e.Bets, e.PotAmount, e.BigBlindAmount, e.Stage) {
    }

    public OutsActionEventArgs(ScoredActionRequestEventArgs e) :
        base(e.Bet, e.Board, e.Bets, e.PotAmount, e.BigBlindAmount, e.Stage) {
        Score = e.Score;
    }

}

public interface IWinningHandEventArgs {
    public Pocket WinningPocket { get; set; }
    public int WinningScore { get; set; }
}

public class WinningHandEventArgs : ActionRequestEventArgs, IWinningHandEventArgs {

    public Pocket WinningPocket { get; set; }
    public int WinningScore { get; set; }
    public WinningHandEventArgs(Bet bet, Board board, List<Bet> bets, int potAmount, int bigBlindAmount, RoundStage stage)
        : base(bet, board, bets, potAmount, bigBlindAmount, stage) {
    }

    public WinningHandEventArgs(ActionRequestEventArgs e) :
        base(e.Bet, e.Board, e.Bets, e.PotAmount, e.BigBlindAmount, e.Stage) {

    }
}

public class ScoredWinningHandEventArgs : ScoredActionRequestEventArgs, IWinningHandEventArgs {
    public Pocket WinningPocket { get; set; }
    public int WinningScore { get; set; }
    public ScoredWinningHandEventArgs(Bet bet, Board board, List<Bet> bets, int potAmount, int bigBlindAmount, RoundStage stage) : base(bet, board, bets, potAmount, bigBlindAmount, stage) {
    }

    public ScoredWinningHandEventArgs(ScoredActionRequestEventArgs e) : base(e) {
        Score = e.Score;
    }
}

public class OutsWinningHandEventArgs : OutsActionEventArgs, IWinningHandEventArgs {

    public Pocket WinningPocket { get; set; }
    public int WinningScore { get; set; }
    public OutsWinningHandEventArgs(OutsActionEventArgs e) : base(e) {
        Outs = e.Outs;
    }
}