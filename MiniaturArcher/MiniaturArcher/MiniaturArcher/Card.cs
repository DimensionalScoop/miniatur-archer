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

namespace MiniaturArcher
{
    class Card
    {
        public static Synchronizer Sync;
        public static Map Map;

        public static List<Card> AllCards = new List<Card>();
        static readonly Sprite DefaultCard = new Sprite("c\\card");
        public static readonly Sprite TurnedCard = new Sprite("c\\turnedcard");

        static readonly Random random = new Random();
        static readonly Color[] deckDiff = new Color[50];

        static Card()
        {
            for (int i = 0; i < deckDiff.Length; i++)
            {
                byte gray = (byte)(random.Next(55) + 200);
                deckDiff[i] = new Color(gray, gray, gray);
            }
            deckDiff[0] = Color.White;

            AllCards.AddRange(new Card[]{
            new Card() { Color = Color.Red,Type= UnitTypes.Fire },
            new Card() { Color = Color.Blue,Type= UnitTypes.Water },
            new Card() { Color = Color.Green,Type= UnitTypes.Air}
            });
        }

        public UnitTypes Type;
        public Color Color;
        Sprite card = DefaultCard;


        public virtual bool ActivateCard(Vector2 position)
        {
            var tile = Map[position];

            if (tile != null)
                if (tile.Fraction == null || Sync.OwnFraction.IsAlley(tile.Fraction))
                    if (tile.CountUnits < 2)
                        //TODO: ACTIVITY
                    //    if (tile.Activity >= Tile.ActivityPerSummoning)
                        {
                            tile.Summon(new Unit(tile.Position, Type, Sync.OwnFraction));
                            return true;
                        }
            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, bool flipped, bool turned, int num = 0)
        {
            float angle = 0;
            if (flipped) angle = MathHelper.PiOver2;

            var color = Color.Lerp(this.Color, deckDiff[num % deckDiff.Length], 0.5f);
            Sprite sprite = card;
            if (turned) { sprite = TurnedCard; color = deckDiff[num % deckDiff.Length]; }

            spriteBatch.Draw(sprite, position, null, color, angle, card.TextureOrigin, 1, SpriteEffects.None, 0);
        }
    }
}