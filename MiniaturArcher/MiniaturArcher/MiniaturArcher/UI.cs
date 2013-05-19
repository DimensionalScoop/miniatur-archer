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
    class UI:DrawableGameComponent
    {
        public List<string> Chat = new List<string>();

        Font font = new Font("font");
        SpriteBatch spriteBatch;
        public static Map Map;
        public static InputProvider Input;
        public static Synchronizer Sync;
        public static KeyProvider Key { get { return Input.Key; } }
        public static MouseProvider Mouse { get { return Input.Mouse; } }

        bool writingChatLine;
        private string chatLine = "";



        public UI(Game game) : base(game) { }

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

        public override void Update(GameTime gameTime)
        {
            if (writingChatLine && Key.KeysStroked.Contains(Keys.Enter))
            {
                Sync.NewEvent(SyncEvents.Chat, chatLine);
                chatLine = "";
                writingChatLine = false;
            }
            else if (Key.KeysStroked.Contains(Keys.Enter)) writingChatLine = true;
            
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            for (int i = 0; i < 10 && i < Chat.Count; i++)
                spriteBatch.DrawText(Chat[Chat.Count - i - 1], new Vector2(20, 300 - font.SpriteFont.LineSpacing * i), false, font, Color.Black);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
