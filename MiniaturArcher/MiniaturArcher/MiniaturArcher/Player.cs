using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using X45Game;
using X45Game.Drawing;
using X45Game.Effect;
using X45Game.Input;
using X45Game.Extensions;
using X45Game.Strategics;
using System.Diagnostics;

namespace MiniaturArcher
{
    class Player:X45Game.Strategics.Player
    {
        static Random random = new Random();

        const int deckSize = 150;

        public List<Card> Hand, Deck, Graveyard;
        public int CountHandCards(Card type)
        {
            return Hand.Count(p => p == type);
        }

        public Player(Players id, int[] deck=null):base(id)
        {
            Hand = new List<Card>();
            Graveyard = new List<Card>();

            if (deck == null)
            switch(id)
            {
                case Players.Red:
                    deck = new int[] {100,40,50 };
                    break;
                case Players.Blue:
                    deck = new int[] { 50, 100, 40 };
                    break;
                case Players.Green:
                    deck = new int[] { 50, 40, 100 };
                    break;
                default:
                    deck = new int[] { 1, 1, 1 };
                    break;
            }

            CreateDeck(deck);
            DrawCards(8);
        }

        void CreateDeck(int[] deck)
        {
            var availableCards = new Card[deck.Sum()];
            Deck = new List<Card>();

            var min = 0;
            for (int cardType = 0; cardType < deck.Length; cardType++)
            {
                for (int i = min; i < deck[cardType]+min; i++)
                    availableCards[i] = Card.AllCards[cardType];
                min += deck[cardType];
            }

            for (int i = 0; i < deckSize; i++)
                Deck.Add(random.NextElement(availableCards));
        }

        public void DrawCards(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (Deck.Count > 0)
                {
                    Hand.Add(Deck[0]);
                    Deck.RemoveAt(0);
                }
            }
        }

        public void Discard(Card type)
        {
            Debug.Assert(Hand.Contains(type));
            Hand.Remove(type);
        }

        public void DiscardAny()
        {
            Hand.RemoveAt(random.Next(Hand.Count));
        }
    }
}
