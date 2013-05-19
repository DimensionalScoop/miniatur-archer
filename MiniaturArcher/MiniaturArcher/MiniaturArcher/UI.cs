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
            
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            for (int i = 0; i < 10 && i < chat.Count; i++)
                spriteBatch.DrawText(chat[chat.Count - i - 1].Message, new Vector2(screen.X/2,screen.Y*2/3f)+new Vector2(0, font.SpriteFont.LineSpacing * i), true, font, Color.Black);

            spriteBatch.DrawRectangle(new Vector2(20, 20), 100, 40, Sync.OwnFraction.Color);
            spriteBatch.DrawProgressBar(new Vector2(30, 30), 45, (int)MathHelper.Clamp((int)(45 * (gameTime.TotalGameTime - Map.TurnBegin).TotalSeconds / Map.TurnDuration.TotalSeconds), 0, 45), 4, Color.LightGray, Color.DarkGray, false);
            spriteBatch.DrawText(Map.Turn.ToString(), new Vector2(30 + 50, 30 - 5), false, font, Color.Black);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
