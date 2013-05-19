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
        UI ui;
        Synchronizer sync;
        Map map;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            _input = new InputProvider(this);

            ui = new UI(this);
            Components.Add(ui);

            map = new Map(this);
            Components.Add(map);

            sync = new Synchronizer(this);
            Components.Add(sync);

            Map.Sync = sync;
            Tile.Map = map;
            UI.Map = map;
            UI.Input = _input;
            Synchronizer.Ui = ui;
            Synchronizer.Map = map;

            _graphics.PreferredBackBufferWidth = 1360;
            _graphics.PreferredBackBufferHeight = 730;
            IsMouseVisible = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(30);
        }

        protected override void UnloadContent()
        {
            sync.Dispose();
            
            base.UnloadContent();
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
