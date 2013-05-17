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
    enum UnitTypes 
    { 
        None=0,
        Fire=1<<0, 
        Water=1<<1, 
        Air=1<<2,
        FireWater=Fire|Water,
        FireAir=Fire|Air, 
        WaterAir=Water|Air,
    }

    class Unit
    {
        public static Map Map;
        public static Synchronizer Sync;

        public const int MaxHitpoints = 10;
        const int attackDamageEffective = 3;
        const int attackDamageModest = 2;
        const int attackDamageNormal = 1;
        const int attackDamageIneffective = 0;

        const int activityPerTurnNeighborTile = 1;
        const int activityPerTurnThisTile = 2;


        public Tile Tile
        {
            get
            {
                return Map[Position];
            }
        }
        public UnitTypes Type;
        public UnitTypes Synergy
        {
            get
            {
                return Tile.Type;
            }
        }
        public Player Fraction;
        public readonly long Id;

        public Color Color
        {
            get
            {
                switch (Type)
                {
                    case UnitTypes.Fire: return Color.OrangeRed;
                    case UnitTypes.Water: return Color.DarkBlue;
                    case UnitTypes.Air: return Color.GreenYellow;
                    case UnitTypes.FireAir: return Color.Yellow;
                    case UnitTypes.FireWater: return new Color(170, 0, 255, 255);
                    case UnitTypes.WaterAir: return new Color(0, 255, 180, 255);
                    default: throw new NotImplementedException();
                }
            }
        }

        public int Hitpoints;
        public bool CanAct;
        public Point2 Position;


        public Unit()
        {
            Id = Sync.GetId();
        }

        //public Unit(Synchronizer.Unit template):this(

        private Unit(Point2 position, UnitTypes type, Player fraction, long id)
        {
            Type = type;
            Position = position;
            Fraction = fraction;
            Id = id;

            Debug.Assert(Tile.CountUnits < 2);
            Spawn(Tile);
        }

        public void Attack()
        {
            if (!CanAct) return;

            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    var target = Map[Position + new Point2(x, y)];

                    if (target != null && target.CountUnits>0 && Fraction.IsEnemy(target.Fraction))
                    {
                        int damage = CalcDamage(Synergy, target.Type);
                        target.GetUnitWithMostHitpoints.Hitpoints -= damage;
                        CanAct = false;
                    }
                }
        }

        public bool Move(Tile target)
        {
            if (
            Math.Abs((Position - target.Position).X) <= 1 &&
            Math.Abs((Position - target.Position).Y) <= 1 &&
            CanAct)
            {
                if (Spawn(target))
                {
                    Tile.Release(this);
                    CanAct = false;
                    Sync.NewEvent(SyncEvents.UnitMoved,Id, target.Position);
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public bool Spawn(Tile target)
        {
            if (target.CountUnits < 2)
            {
                if (target.CountUnits == 0)
                {
                    if (Fraction.IsEnemy(target.Fraction)) target.Activity = -target.Activity;
                    target.Fraction = Fraction;
                }
                else if (Fraction.IsAlley(target.GetUnitWithMostHitpoints.Fraction)) { }
                else return false;

                if (target.Unit[0] == null) target.Unit[0] = this;
                else target.Unit[1] = this;
                Position = target.Position;
                return true;
            }
            else return false;
        }

        public void Idle()
        {
            Tile.Activity += activityPerTurnThisTile;

            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    var neighbor = Map[Position + new Point2(x, y)];
                    if (neighbor != null && Fraction.IsAlley(neighbor.Fraction))
                        Map[Position + new Point2(x, y)].Activity += activityPerTurnNeighborTile;
                }
        }

        static int CalcDamage(UnitTypes source, UnitTypes target)
        {
            switch (source)
            {
                case UnitTypes.Fire:
                    switch (target)
                    {
                        case UnitTypes.Fire:
                        case UnitTypes.Water:
                            return attackDamageNormal;

                        case UnitTypes.Air:
                            return attackDamageEffective;

                        case UnitTypes.FireWater:
                        case UnitTypes.FireAir:
                            return attackDamageEffective;

                        case UnitTypes.WaterAir:
                            return attackDamageNormal;

                        default: throw new NotImplementedException();
                    }

                case UnitTypes.Air:
                    switch (target)
                    {
                        case UnitTypes.Air:
                        case UnitTypes.Fire:
                            return attackDamageNormal;

                        case UnitTypes.Water:
                            return attackDamageEffective;

                        case UnitTypes.WaterAir:
                        case UnitTypes.FireAir:
                            return attackDamageEffective;

                        case UnitTypes.FireWater:
                            return attackDamageNormal;

                        default: throw new NotImplementedException();
                    }

                case UnitTypes.Water:
                    switch (target)
                    {
                        case UnitTypes.Air:
                        case UnitTypes.Water:
                            return attackDamageNormal;

                        case UnitTypes.Fire:
                            return attackDamageEffective;

                        case UnitTypes.FireWater:
                        case UnitTypes.WaterAir:
                            return attackDamageEffective;

                        case UnitTypes.FireAir:
                            return attackDamageNormal;

                        default: throw new NotImplementedException();
                    }

                case UnitTypes.FireAir:
                    switch (target)
                    {
                        case UnitTypes.Fire:
                        case UnitTypes.Air:
                            return attackDamageNormal;

                        case UnitTypes.Water:
                            return attackDamageEffective;

                        case UnitTypes.FireWater:
                        case UnitTypes.FireAir:
                            return attackDamageNormal;

                        case UnitTypes.WaterAir:
                            return attackDamageEffective;

                        default: throw new NotImplementedException();
                    }

                case UnitTypes.FireWater:
                    switch (target)
                    {
                        case UnitTypes.Fire:
                        case UnitTypes.Water:
                            return attackDamageNormal;

                        case UnitTypes.Air:
                            return attackDamageEffective;

                        case UnitTypes.FireWater:
                        case UnitTypes.WaterAir:
                            return attackDamageNormal;

                        case UnitTypes.FireAir:
                            return attackDamageEffective;

                        default: throw new NotImplementedException();
                    }

                case UnitTypes.WaterAir:
                    switch (target)
                    {
                        case UnitTypes.Water:
                        case UnitTypes.Air:
                            return attackDamageNormal;

                        case UnitTypes.Fire:
                            return attackDamageEffective;

                        case UnitTypes.WaterAir:
                        case UnitTypes.FireAir:
                            return attackDamageNormal;

                        case UnitTypes.FireWater:
                            return attackDamageEffective;

                        default: throw new NotImplementedException();
                    }

                default: throw new NotImplementedException();
            }
        }
    }

    class Tile
    {
        public static Map Map;

        static Sprite tile = new Sprite("s\\tile"),
            fire = new Sprite("s\\fire"), water = new Sprite("s\\water"), air = new Sprite("s\\air"),
            firefire=new Sprite("s\\firefire"),waterwater=new Sprite("s\\waterwater"),airair=new Sprite("s\\airair"),
            fireair = new Sprite("s\\fireair"), firewater = new Sprite("s\\firewater"), waterair = new Sprite("s\\waterair");

        const int defaultActivity = -10;
        const int activityReductionPerTurn = 1;
        const int maxActivity = 50;
        
        public Unit[] Unit;
        
        public int Activity;
        public readonly Point2 Position;
        public Player Fraction;
        public UnitTypes Type
        {
            get
            {
                if (Unit[0] == null && Unit[1] == null) return UnitTypes.None;
                if (Unit[0] == null) return Unit[1].Type;
                if (Unit[1] == null) return Unit[0].Type;
                return Unit[0].Type | Unit[1].Type;
            }
        }
        public int CountUnits
        {
            get
            {
                return (Unit[0] != null ? 1 : 0) + (Unit[1] != null ? 1 : 0);
            }
        }
        public Unit GetUnitWithMostHitpoints
        {
            get
            {
                if (Unit[0] == null) return Unit[1];
                if (Unit[1] == null) return Unit[0];
                if (Unit[0].Hitpoints > Unit[1].Hitpoints) return Unit[0];
                return Unit[1];
            }
        }
        

        public Tile(Point2 pos)
        {
            Position = pos;
            Unit = new Unit[2];
            Fraction = null;
            Activity = defaultActivity;
        }

        internal void Release(Unit unit)
        {
            //XXX: May lead to glitches if unit[0]==unit[1]
            if (Unit[0] == unit) Unit[0] = null;
            else if (Unit[1] == unit) Unit[1] = null;
            else throw new ArgumentException();
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 camera)
        {
            Color color = Color.LightGray;
            if (Activity < 0) color = Color.Lerp(Color.White, Color.Gray, Activity / (float)maxActivity);
            else color = Color.Lerp(Color.White, Fraction.Color, Activity / (float)maxActivity);

            spriteBatch.Draw(tile, Position * tile.TextureOrigin * 2 + camera, color);

            for (int i = 0; i < 2; i++)
            {
                if (Unit == null) continue;
                spriteBatch.Draw(Sprite, Position * tile.TextureOrigin * 2 + camera, Fraction.Color);
                if (i == 0)
                    spriteBatch.DrawLine(Position * tile.TextureOrigin * 2 + camera + new Vector2(1, 3), (new Vector2(0, 15) * Unit[i].Hitpoints / (float)MiniaturArcher.Unit.MaxHitpoints + Position * tile.TextureOrigin * 2 + camera + new Vector2(2, 4)), Unit[i].Color);
                else
                    spriteBatch.DrawLine(Position * tile.TextureOrigin * 2 + camera + new Vector2(3, 1), (new Vector2(15, 0) * Unit[i].Hitpoints / (float)MiniaturArcher.Unit.MaxHitpoints + Position * tile.TextureOrigin * 2 + camera + new Vector2(3, 1)), Unit[i].Color);
            }
        }

        Sprite Sprite
        {
            get
            {
                switch (Type)
                {
                    case UnitTypes.WaterAir: return waterair;
                    case UnitTypes.FireWater: return firewater;
                    case UnitTypes.FireAir: return fireair;
                    case UnitTypes.Fire: if (CountUnits == 2) return firefire; return fire;
                    case UnitTypes.Water: if (CountUnits == 2) return waterwater; return water;
                    case UnitTypes.Air: if (CountUnits == 2) return airair; return air;
                    default: throw new NotImplementedException();
                }
            }
        }
    }
}
