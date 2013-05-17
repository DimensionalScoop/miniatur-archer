using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using X45Game;
using X45Game.Drawing;
using X45Game.Effect;
using X45Game.Input;

namespace MiniaturArcher
{
    public class Game1 : Game
    {
        GraphicsDeviceManager _graphics;
        SpriteBatch _spriteBatch;

        InputProvider _input;


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            _input = new InputProvider(this);

            _graphics.PreferredBackBufferWidth = 1360;
            _graphics.PreferredBackBufferHeight = 730;
            IsMouseVisible = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(30);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Sprite.Initialize(Content, GraphicsDevice);
            Font.Initialize(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (_input.Key.KeysStroked.Contains(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(30, 30, 30));

            _spriteBatch.Begin();

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
