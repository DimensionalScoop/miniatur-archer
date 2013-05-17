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
    class Map:DrawableGameComponent
    {
        public static Synchronizer Sync;

        const int mapSize=30;
        const int mapBias = mapSize / 2;

        public static readonly TimeSpan TurnDuration=TimeSpan.FromSeconds(15);
        public static readonly TimeSpan TurnBreakDuration=TimeSpan.FromSeconds(1);

        Tile[,] tiles = new Tile[mapSize, mapSize];

        public List<Unit> Units = new List<Unit>();

        Vector2 camera;
        SpriteBatch spriteBatch;

        public int Turn;
        public TimeSpan TurnBegin;

        public Tile this[Point2 i]
        {
            get
            {
                if (i.X + mapBias >= mapSize - mapBias || i.X + mapBias < 0 || i.Y + mapBias >= mapSize - mapBias || i.Y + mapBias < 0)
                    return null;
                return tiles[i.X + mapBias, i.Y + mapBias];
            }
        }


        public Map(Game game):base(game)
        {
            Tile.Map=this;
            Unit.Map=this;

            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                    tiles[x, y] = new Tile(new Point2(x, y));
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            if (gameTime.TotalGameTime - TurnBegin >= TurnDuration)
                TurnEnds();
            
            base.Update(gameTime);
        }

        private void TurnEnds()
        {
            Sync.NewEvent(SyncEvents.TurnEnd);
            
            //TODO: stuff that happens when the turn ends
        }

        public void TurnBegins()
        {

        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                    tiles[x, y].Draw(spriteBatch, camera);

            spriteBatch.DrawProgressBar(new Vector2(20, 20), 45, (int)(45 * (gameTime.TotalGameTime - TurnBegin).TotalSeconds / TurnDuration.TotalSeconds), 4, Color.Gray, Color.GhostWhite);

            spriteBatch.End();
            
            base.Draw(gameTime);
        }
    }
}
