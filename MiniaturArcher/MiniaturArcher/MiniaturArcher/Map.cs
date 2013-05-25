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
using System.Diagnostics;

namespace MiniaturArcher
{
    class Map:DrawableGameComponent
    {
        public static Synchronizer Sync;
        public static UI Ui;

        const int mapSize=20;

        public static readonly TimeSpan TurnDuration=TimeSpan.FromSeconds(5);
        public static readonly TimeSpan TurnBreakDuration=TimeSpan.FromSeconds(1);

        Tile[,] tiles = new Tile[mapSize, mapSize];

        //public List<Unit> Units = new List<Unit>();

        Vector2 camera;
        SpriteBatch spriteBatch;
        TimeSpan lastUpdate;

        public int Turn;
        public TimeSpan TurnBegin;
        public bool TurnEnded;

        public Tile this[Point2 i]
        {
            get
            {
                if (i.X >= mapSize || i.X < 0 || i.Y >= mapSize || i.Y < 0)
                    return null;
                else
                    return tiles[i.X, i.Y];
            }
        }
        public Tile this[Vector2 i]
        {
            get
            {
                return this[new Point2((int)i.X / Tile.tile.Texture.Width, (int)i.Y / Tile.tile.Texture.Height)];
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
            lastUpdate = gameTime.TotalGameTime;

            if (!TurnEnded && gameTime.TotalGameTime - TurnBegin >= TurnDuration)
                TurnEnds();
            
            base.Update(gameTime);
        }

        private void TurnEnds()
        {
            TurnEnded = true;
            Sync.NewEvent(SyncEvents.TurnEnd);
            
            //TODO: stuff that happens when the turn ends
            List<Unit> units=new List<Unit>();

            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                {
                    var tile = tiles[x, y];
                    if (tile.CountUnits > 0)
                        units.AddRange(tile.Unit);
                }
            units.RemoveAll(p => p == null);

            units.ForEach(p => p.Idle());

            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                    tiles[x, y].TurnEnds();
        }

        public void TurnBegins()
        {
            Debug.Assert(TurnEnded);
            TurnEnded = false;
            Turn++;
            TurnBegin = lastUpdate;
            if(Turn%2==0)
            Sync.OwnFraction.DrawCards(1);

        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                    tiles[x, y].Draw(spriteBatch, camera);

            
            spriteBatch.End();
            
            base.Draw(gameTime);
        }
    }
}
