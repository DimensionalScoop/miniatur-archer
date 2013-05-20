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
    class ChatItem { public string Message; public TimeSpan SpawnTime;}

    class UI:DrawableGameComponent
    {
        static readonly TimeSpan chatDisplayDuration = TimeSpan.FromSeconds(5);

        List<ChatItem> chat = new List<ChatItem>();

        Card selectedCard;
        Vector2 startDragPosition;

        public Vector2 Camera;

        TimeSpan lastUpdate;
        Font font = new Font("font");
        SpriteBatch spriteBatch;
        Vector2 screen { get { return new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height); } }
        public static Map Map;
        public static InputProvider Input;
        public static Synchronizer Sync;
        public static KeyProvider Key { get { return Input.Key; } }
        public static MouseProvider Mouse { get { return Input.Mouse; } }


        bool writingChatLine;
        private string chatLine = "";



        public UI(Game game) : base(game) 
        {
            DrawOrder = 10;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Key.TextEnteredSync += Key_TextEnteredSync;

            base.LoadContent();
        }

        void Key_TextEnteredSync(char arg1, KeyFunction arg2)
        {
            if (writingChatLine)
                chatLine += arg1;
        }

        public void NewChatMessage(string msg, bool local = true)
        {
            chat.Add(new ChatItem() { Message = msg, SpawnTime = lastUpdate });
            if (!local) Sync.NewEvent(SyncEvents.Chat, msg);
        }

        public override void Update(GameTime gameTime)
        {
            lastUpdate = gameTime.TotalGameTime;

            if (writingChatLine && Key.KeysStroked.Contains(Keys.Enter))
            {
                NewChatMessage(chatLine, false);
                chatLine = "";
                writingChatLine = false;
            }
            else if (Key.KeysStroked.Contains(Keys.Enter)) writingChatLine = true;

            chat.RemoveAll(p => lastUpdate - p.SpawnTime > chatDisplayDuration);


            if (Mouse.StartDrags.Contains(MouseButtons.Left))
            {
                if (selectedCard == null)
                {
                    selectedCard = GetCardUnderMouse(out startDragPosition);
                }
            }
            if (selectedCard != null&&Mouse.Clicks.Contains(MouseButtons.Left))
            {
                if (selectedCard != GetCardUnderMouse(out startDragPosition))
                {
                    if (selectedCard.ActivateCard(Mouse.Position - Camera))
                        Sync.OwnFraction.Discard(selectedCard);
                }
                selectedCard = null;
            }

            if (Sync.OwnFraction.Hand.Count > 0 && Key.KeysStroked.Contains(Keys.D))
            {
                Sync.OwnFraction.DiscardAny();
                Sync.OwnFraction.DrawCards(1);
            }

            base.Update(gameTime);
        }
        Card GetCardUnderMouse(out Vector2 cardPos)
        {
            var grp = new Vector2(screen.X - 100, screen.Y - 100);
            var hand = grp - new Vector2(100, 0);
            for (int i = 0; i < Card.AllCards.Count; i++)
            {
                var count = Sync.OwnFraction.CountHandCards(Card.AllCards[i]);

                if (count > 0)
                {
                    if (new Rectangle((int)(hand.X - Card.TurnedCard.TextureOrigin.X), (int)(hand.Y - Card.TurnedCard.TextureOrigin.Y), (int)Card.TurnedCard.TextureOrigin.X * 2, (int)Card.TurnedCard.TextureOrigin.Y * 2)
                    .Contains(Mouse.IntPosition))
                    {
                        cardPos = hand;
                        return Card.AllCards[i];
                    }
                    hand -= new Vector2(Card.TurnedCard.TextureOrigin.X * 2.3f, 0);
                }
            }
            cardPos = Vector2.Zero;
            return null;
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            for (int i = 0; i < 10 && i < chat.Count; i++)
                spriteBatch.DrawText(chat[chat.Count - i - 1].Message, new Vector2(screen.X/2,screen.Y*2/3f)+new Vector2(0, font.SpriteFont.LineSpacing * i), true, font, Color.Black);


            var grp = new Vector2(screen.X - 100, screen.Y - 100);
            var size = (int)Card.TurnedCard.TextureOrigin.X * 2;

            //spriteBatch.DrawRectangle(grp-new Vector2(100,30), 100, 40, Sync.OwnFraction.Color);
            spriteBatch.DrawProgressBar(grp + new Vector2(0, 65), size, (int)MathHelper.Clamp((int)(size * (gameTime.TotalGameTime - Map.TurnBegin).TotalSeconds / Map.TurnDuration.TotalSeconds), 0, size), 4, Color.DarkGray, Sync.OwnFraction.Color, true);
            spriteBatch.DrawText(Map.Turn.ToString(), grp + new Vector2(0, 55),true, font, Color.White);

            for (int i = 0; i < Sync.OwnFraction.Deck.Count; i++)
            {
                var pos = new Vector2(screen.X - 100, screen.Y - 100 - i / 3f);
                if (i == Sync.OwnFraction.Deck.Count - 1) pos = pos.Round();
                Sync.OwnFraction.Deck[i].Draw(spriteBatch, pos, false, true, i);
            }

            var hand = grp - new Vector2(100, 0);
            for (int i = 0; i < Card.AllCards.Count; i++)
            {
                var count=Sync.OwnFraction.CountHandCards(Card.AllCards[i]);

                if (count > 0)
                {
                    for (int i2 = count - 1; i2 >= 0; i2--)
                        Card.AllCards[i].Draw(spriteBatch, hand.Round() + new Vector2(0, i2*2), false, false,i2);
 
                    spriteBatch.DrawText("x" + count, hand + Card.TurnedCard.TextureOrigin-new Vector2(10,10), true, font, Card.AllCards[i].Color.Inverse());
                    hand -= new Vector2(Card.TurnedCard.TextureOrigin.X * 2.3f, 0);
                }
            }

            if (selectedCard != null)
                spriteBatch.DrawLine(startDragPosition, Mouse.Position, selectedCard.Color);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}