//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Temp : MonoBehaviour
//{
//    public abstract class Player : MonoBehaviour {

//        private PlayerController controller;
//        private HandFinder handFinder;
//        private HandScorer handScorer;

//        private Pocket pocket;
//        private List<Hand> hands;

//        public int Stack { get; private set; }
//        public bool IsAllIn { get; private set; } = false;
//        private int amountToPay;

//        private EventHandler RequestAnalysis;
//        private EventHandler RequestHands;
//        public EventHandler<Bet> ActionMade;
//        private GameObject playerPosition;
//        private Transform[] cardPositions = new Transform[2];

//        private static string holeCard1Tag = "HoleCard1";
//        private static string holeCard2Tag = "HoleCard2";

//        public int Outs { get; private set; } = 0;
//        private delegate void RecieveAnalysisListener(object obj, HandsEventArgs h);
//        private RecieveAnalysisListener analysisListener;
//        private delegate void RecieveHandsListener(object o, HandsEventArgs h);
//        private RecieveHandsListener handsListener;
//        public int Score {
//            get {
//                if (hands != null && hands.Count != 0) {
//                    return hands[0].Score;
//                }
//                return 0;
//            }
//        }
//        // Start is called before the first frame update
//        private void Start() {
//            pocket = new Pocket();
//            handScorer = GetComponent<HandScorer>();
//            controller = GetComponent<PlayerController>();
//            handFinder = GetComponent<HandFinder>();
//            handsListener = new RecieveHandsListener(ReceiveHands);
//            RequestHands += (EventHandler)handFinder.SetListener(handsListener);
//            if (handScorer) {
//                analysisListener = new RecieveAnalysisListener(ReceiveAnalysedHands);
//                RequestAnalysis += (EventHandler)handScorer.SetListener(analysisListener);
//            }
//        }

//        private void ReceiveHands(object o, HandsEventArgs h) {
//            throw new NotImplementedException();
//        }

//        public void RequestAction(Board board) {
//            if (board.CountCards() == 0) {
//                //Request preflop action from controller (event)
//                return;
//            }



//        }
//        private void RequestHandAnalysis() {
//            RequestAnalysis?.Invoke(this, new HandsEventArgs { Hands = hands.ToArray() });
//        }

//        public void NewHand(object obj, NewHandEventArgs e) {
//            foreach (Transform transform in cardPositions) {
//                Transform card = transform.GetChild(0);
//                card.SetParent(null);
//                card.gameObject.SetActive(false);
//            }
//            Outs = 0;

//            HandleBlinds(e);

//        }

//        private void HandleBlinds(NewHandEventArgs e) {
//            if (e.anteAmount > 0) {
//                e.AddBlind(PayBlinds(e.anteAmount));
//            }
//            if (e.BigBlindPlayer == this) {
//                e.AddBlind(PayBlinds(e.SmallBlindAmount * 2));
//            }
//            if (e.SmallBlindPlayer == this) {
//                e.AddBlind(PayBlinds(e.SmallBlindAmount));
//            }
//        }

//        public void PlaceAtTable(GameObject position) {
//            playerPosition = position;
//            cardPositions[0] = Helpers.FindChildWithTag(position, holeCard1Tag).transform;
//            cardPositions[1] = Helpers.FindChildWithTag(position, holeCard2Tag).transform;
//        }



//        private Bet PayBlinds(int blind) {
//            if (Stack <= 0) {
//                IsAllIn = true;
//                return new Bet(this, blind, PokerAction.CheckOrCall);
//            }
//            if (Stack > blind) {
//                Stack -= blind;
//                return new Bet(this, blind, PokerAction.CheckOrCall);
//            }
//            blind = Stack;
//            Stack = 0;
//            IsAllIn = true;
//            return new Bet(this, blind, PokerAction.CheckOrCall);

//        }

//        public void GivePocketCard(Card card, GameObject cardGO) {
//            int cardPos = pocket.AddCard(card);
//            if (cardPos == -1) {
//                return;
//            }
//            cardGO.transform.SetParent(cardPositions[cardPos], false);
//            cardGO.SetActive(true);

//        }



//        public void OutOfTourney() {

//        }

//        private void ReceiveAnalysedHands(object obj, HandsEventArgs h) {
//            hands = new List<Hand>(h.Hands);
//            hands = hands.OrderByDescending(hand => hand.Score).ToList();
//        }

//        internal void SetPosition(GameObject position) {
//            throw new NotImplementedException();
//        }

//        internal void ClearPosition() {
//            throw new NotImplementedException();
//        }
//    }

//}
