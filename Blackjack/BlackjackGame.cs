using System;
using System.Collections.Generic;
using System.Linq;

namespace Dinghy.Blackjack
{
    public enum GameResult { Ongoing, PlayerWin, DealerWin, BlackJack, DoubleAce, Forfeit, Tie }

    public class BlackjackGame
    {
        private static readonly string[] Suits = { "♠️", "♥️", "♦️", "♣️" };
        private static readonly string[] Ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        private static readonly Dictionary<string, int> CardValues = new Dictionary<string, int>
        {
            { "A", 11 }, { "2", 2 }, { "3", 3 }, { "4", 4 }, { "5", 5 },
            { "6", 6 }, { "7", 7 }, { "8", 8 }, { "9", 9 }, { "10", 10 },
            { "J", 10 }, { "Q", 10 }, { "K", 10 }
        };

        private List<string> Deck;
        public List<string> PlayerHand { get; private set; }
        public List<string> DealerHand { get; private set; }
        public bool IsFinished { get; private set; }
        public GameResult Result { get; private set; }

        public BlackjackGame()
        {
            Deck = GenerateDeck();
            ShuffleDeck();

            PlayerHand = new List<string>();
            DealerHand = new List<string>();
            IsFinished = false;
            Result = GameResult.Ongoing;
        }

        public void ShuffleAndDeal()
        {
            PlayerHand.Clear();
            DealerHand.Clear();

            PlayerHand.Add(DrawCard());
            DealerHand.Add(DrawCard());
            PlayerHand.Add(DrawCard());
            DealerHand.Add(DrawCard());

            Result = EvaluateHands();
        }

        private List<string> GenerateDeck()
        {
            List<string> deck = new List<string>();
            foreach (string suit in Suits)
            {
                foreach (string rank in Ranks)
                {
                    deck.Add(rank + suit);
                }
            }
            return deck;
        }

        private void ShuffleDeck()
        {
            Random rng = new Random();
            for (int i = Deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                string temp = Deck[i];
                Deck[i] = Deck[j];
                Deck[j] = temp;
            }
        }

        private string DrawCard()
        {
            if (Deck.Count == 0)
                throw new InvalidOperationException("No cards left in the deck.");

            string card = Deck[0];
            Deck.RemoveAt(0);
            return card;
        }

        public int CalculateHandValue(List<string> hand)
        {
            int total = 0;
            int aces = 0;

            foreach (string card in hand)
            {
                string rank = new string(card.Where(char.IsLetterOrDigit).ToArray());
                int value;

                if (!CardValues.TryGetValue(rank, out value))
                    throw new ArgumentException("Invalid card rank: " + rank);

                total += value;
                if (rank == "A") aces++;
            }

            while (total > 21 && aces > 0)
            {
                total -= 10;
                aces--;
            }

            return total;
        }

        public GameResult EvaluateHands()
        {
            int playerValue = CalculateHandValue(PlayerHand);

            if (PlayerHand.Count == 2 && PlayerHand.All(c => c.StartsWith("A")))
                return GameResult.DoubleAce;

            if (playerValue == 21 && PlayerHand.Count == 2)
                return GameResult.BlackJack;

            if (playerValue > 21)
                return GameResult.DealerWin;

            return GameResult.Ongoing;
        }

        public void PlayerHit()
        {
            PlayerHand.Add(DrawCard());
            Result = EvaluateHands();
        }

        public void PlayerForfeit()
        {
            IsFinished = true;
            Result = GameResult.Forfeit;
        }

        public void PlayerStand()
        {
            int playerValue = CalculateHandValue(PlayerHand);

            while (CalculateHandValue(DealerHand) < 17)
                DealerHand.Add(DrawCard());

            int dealerValue = CalculateHandValue(DealerHand);

            bool playerBust = playerValue > 21;
            bool dealerBust = dealerValue > 21;

            if (playerBust && dealerBust)
            {
                Result = GameResult.Tie; // Both bust — treat it as tie
            }
            else if (playerBust)
            {
                Result = GameResult.DealerWin;
            }
            else if (dealerBust)
            {
                Result = GameResult.PlayerWin;
            }
            else if (playerValue > dealerValue)
            {
                Result = GameResult.PlayerWin;
            }
            else if (dealerValue > playerValue)
            {
                Result = GameResult.DealerWin;
            }
            else
            {
                Result = GameResult.Tie; // Exact same hand value
            }

            IsFinished = true;
        }
    }
}
