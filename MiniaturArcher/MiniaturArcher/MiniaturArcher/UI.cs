﻿using System;
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

        public Unit SelectedUnit;
        Unit beginUnitMoveUnit;

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


            MouseLeft();
            MouseOver();

            if (Sync.OwnFraction.Hand.Count > 0 && Key.KeysStroked.Contains(Keys.D))
            {
                Sync.OwnFraction.DiscardAny();
                Sync.OwnFraction.DrawCards(1);
            }

            base.Update(gameTime);
        }

        private void MouseOver()
        {
            var tile = Map[Mouse.Position-Camera];

            if (tile != null && tile.CountUnits > 0)
            {
                if (tile.CountUnits == 1) 
                    SelectedUnit = tile.GetUnitWithMostHitpoints;
                else if (Mouse.Position.Y %( Tile.tile.TextureOrigin.Y *2) > Tile.tile.TextureOrigin.Y)
                    SelectedUnit = tile.Unit[1];
                else
                    SelectedUnit = tile.Unit[0];
            }
            else
                SelectedUnit = null;
        }

        private void MouseLeft()
        {
            if (Mouse.StartDrags.Contains(MouseButtons.Left))
            {
                if (selectedCard == null)
                {
                    selectedCard = GetCardUnderMouse(out startDragPosition);
                }
                if (selectedCard == null && SelectedUnit != null && SelectedUnit.Fraction == Sync.OwnFraction)
                    beginUnitMoveUnit = SelectedUnit;
            }


            if (Mouse.Clicks.Contains(MouseButtons.Left))
            {
                if (selectedCard != null)
                {
                    if (selectedCard != GetCardUnderMouse(out startDragPosition))
                    {
                        if (selectedCard.ActivateCard(Mouse.Position - Camera))
                            Sync.OwnFraction.Discard(selectedCard);
                    }
                    selectedCard = null;
                }
                else if (beginUnitMoveUnit!=null)
                {


                    beginUnitMoveUnit = null;
                }
            }

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
            spriteBatch.DrawProgressBar(grp + new Vector2(0, 62+font.SpriteFont.LineSpacing/2), size, (int)MathHelper.Clamp((int)(size * (gameTime.TotalGameTime - Map.TurnBegin).TotalSeconds / Map.TurnDuration.TotalSeconds), 0, size), 4, Color.DarkGray, Sync.OwnFraction.Color, true);
            spriteBatch.DrawText(Map.Turn.ToString(), grp + new Vector2(0, 60),true, font, Color.White);

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
 
                    var counterColor=Color.Lerp(Card.AllCards[i].Color.Inverse(),Color.Black,0.4f);
                    spriteBatch.DrawText(count.ToString(), hand, true, font,counterColor );
                    hand -= new Vector2(Card.TurnedCard.TextureOrigin.X * 2.3f, 0);
                }
            }

            if (selectedCard != null)
                spriteBatch.DrawLine(startDragPosition, Mouse.Position, selectedCard.Color);

            if(Key.KeysPressed.Contains(Keys.Tab))
            {
                var pos=screen/2-new Vector2(0,screen.Y/3);
                var offset=new Vector2(0,font.SpriteFont.LineSpacing);

                spriteBatch.DrawBox(pos-new Vector2(200,20), 400,(int)( (3 + Sync.PlayersByConnection.Count) * offset.Y),Color.White,Color.Black);
                spriteBatch.DrawText("Connected Players:",pos,true,font,Color.Black);pos+=offset*1.5f;
                spriteBatch.DrawText(Sync.OwnFraction.Name+" (you)", pos, true, font, Sync.OwnFraction.Color); pos += offset;
                foreach (var players in Sync.PlayersByConnection)
                {
                    var ping = (int)(players.Key.AverageRoundtripTime*1000);
                    var timeOffset=(int)players.Key.RemoteTimeOffset*1000;
                    var status=players.Key.Status;
                    spriteBatch.DrawText(ping+" "+players.Value.Name+" ("+status+")", pos, true, font, players.Value.Color); pos += offset;
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}