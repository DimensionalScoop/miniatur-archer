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
                    case UnitTypes.Air: return Color.Green;
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

        public Unit(Point2 position, UnitTypes type, Player fraction) : this(position, type, fraction, Sync.GetId()) { }

        public Unit(Point2 position, UnitTypes type, Player fraction, long id)
        {
            Type = type;
            Position = position;
            Fraction = fraction;
            Id = id;
            Hitpoints = MaxHitpoints;
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
                    if (target.Fraction==null || Fraction.IsEnemy(target.Fraction)) target.Activity = -target.Activity;
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
                    if (neighbor != null)
                    {
                        if (neighbor.Fraction == null)
                            neighbor.Fraction = Fraction;
                        else if (Fraction.IsEnemy(neighbor.Fraction))
                            continue;
                        Map[Position + new Point2(x, y)].Activity += activityPerTurnNeighborTile;
                    }
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

        public static readonly Sprite tile = new Sprite("s\\tileframe"),
            tileOverlay = new Sprite("s\\tileframe-overlay"),
            tileBg = new Sprite("s\\tilebg1"), tileBg2 = new Sprite("s\\tilebg2"),
            token = new Sprite("s\\token");
            //fire = new Sprite("s\\fire"), water = new Sprite("s\\water"), air = new Sprite("s\\air"),
            //firefire=new Sprite("s\\firefire"),waterwater=new Sprite("s\\waterwater"),airair=new Sprite("s\\airair"),
            //fireair = new Sprite("s\\fireair"), firewater = new Sprite("s\\firewater"), waterair = new Sprite("s\\waterair");

        const int defaultActivity = -10;
        const int activityReductionPerTurn = 1;
        const int maxActivity = 50;
        public const int ActivityPerSummoning = 10;
        
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
        public Color Color
        {
            get
            {
                switch (Type)
                {
                    case UnitTypes.Fire: return Color.OrangeRed;
                    case UnitTypes.Water: return Color.DarkBlue;
                    case UnitTypes.Air: return Color.Green;
                    case UnitTypes.FireAir: return Color.Yellow;
                    case UnitTypes.FireWater: return new Color(170, 0, 255, 255);
                    case UnitTypes.WaterAir: return new Color(0, 255, 180, 255);
                    default: throw new NotImplementedException();
                }
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

        public void TurnEnds()
        {
            Activity = (int)MathHelper.Clamp(Activity - activityReductionPerTurn, defaultActivity, maxActivity);
        }

        internal void Summon(Unit unit,bool local=false)
        {
            Debug.Assert(CountUnits < 2);
            unit.Spawn(this);
            Activity -= Tile.ActivityPerSummoning;
            if(!local)
            Map.Sync.NewEvent(SyncEvents.UnitSummoned, (byte)Type,unit.Id ,Position);
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
            if (Activity < 0) color = Color.Lerp(Color.White, Color.Gray, -Activity / (float)maxActivity);
            else color = Color.Lerp(Color.White, Color.Orange, Activity / (float)maxActivity);

            spriteBatch.Draw(tileBg2, Position * tile.TextureOrigin * 2 + camera, color);
            spriteBatch.Draw(tileBg, Position * tile.TextureOrigin * 2 + camera, Fraction == null ? Color.White : Fraction.Color);
            spriteBatch.Draw(tile, Position * tile.TextureOrigin * 2 + camera, Color.White);
            //spriteBatch.Draw(tile, Position * tile.TextureOrigin * 2 + camera, Fraction == null ? Color.White : Fraction.Color);

            if (CountUnits > 0)
            {
                if (CountUnits == 2)
                {
                    var color1 = Color.Lerp(Color,Unit[0].Color,0.2f);
                    var color2 = Color.Lerp(Color, Unit[1].Color, 0.2f);
                    var pos1 = Vector2.Zero;
                    var pos2 = Vector2.Zero;

                    if(Map.Ui.SelectedUnit==Unit[0]||Map.Ui.SelectedUnit==Unit[1])
                    {
                        color1 = Unit[0].Color;
                        color2 = Unit[1].Color;
                        if (Map.Ui.SelectedUnit == Unit[0])
                        {
                            pos1 = new Vector2(0, -4);
                        }
                        else
                        {
                            pos2 = new Vector2(0, -4);
                        }
                    }

                    spriteBatch.Draw(token, pos1 + Position * tile.TextureOrigin * 2 + camera - token.TextureOrigin + tile.TextureOrigin - new Vector2(7, 7), color1);
                    spriteBatch.Draw(token, pos2 + Position * tile.TextureOrigin * 2 + camera - token.TextureOrigin + tile.TextureOrigin + new Vector2(7, 7), color2);

                    spriteBatch.DrawProgressBar(pos1 + Position * tile.TextureOrigin * 2 + camera - token.TextureOrigin + tile.TextureOrigin + new Vector2(18, -1) - new Vector2(7, 7), 21, (int)(21 * Unit[0].Hitpoints / MiniaturArcher.Unit.MaxHitpoints), 3, Color.DarkGray, Color.Green);
                    spriteBatch.DrawProgressBar(pos2 + Position * tile.TextureOrigin * 2 + camera - token.TextureOrigin + tile.TextureOrigin + new Vector2(18, -1) + new Vector2(7, 7), 21, (int)(21 * Unit[1].Hitpoints / MiniaturArcher.Unit.MaxHitpoints), 3, Color.DarkGray, Color.Green);
                }
                else
                {
                    var pos = Vector2.Zero;
                    if (Map.Ui.SelectedUnit == GetUnitWithMostHitpoints) pos = new Vector2(0, -4);

                    spriteBatch.DrawProgressBar(pos + Position * tile.TextureOrigin * 2 + camera - token.TextureOrigin + tile.TextureOrigin + new Vector2(18, -1), 21, (int)(21 * GetUnitWithMostHitpoints.Hitpoints / MiniaturArcher.Unit.MaxHitpoints), 3, Color.DarkGray, Color.Green);
                    spriteBatch.Draw(token, pos + Position * tile.TextureOrigin * 2 + camera - token.TextureOrigin + tile.TextureOrigin, Color);
                }
            }

                //    if (i == 0)
                //        spriteBatch.DrawLine(Position * tile.TextureOrigin * 2 + camera + new Vector2(2, 3), (new Vector2(0, 15) * Unit[i].Hitpoints / (float)MiniaturArcher.Unit.MaxHitpoints + Position * tile.TextureOrigin * 2 + camera + new Vector2(2, 3)), Unit[i].Color);
                //    else
                //        spriteBatch.DrawLine(Position * tile.TextureOrigin * 2 + camera + new Vector2(3, 1), (new Vector2(15, 0) * Unit[i].Hitpoints / (float)MiniaturArcher.Unit.MaxHitpoints + Position * tile.TextureOrigin * 2 + camera + new Vector2(3, 1)), Unit[i].Color);
                //}
        }
        //Sprite Sprite
        //{
        //    get
        //    {
        //        switch (Type)
        //        {
        //            case UnitTypes.WaterAir: return waterair;
        //            case UnitTypes.FireWater: return firewater;
        //            case UnitTypes.FireAir: return fireair;
        //            case UnitTypes.Fire: if (CountUnits == 2) return firefire; return fire;
        //            case UnitTypes.Water: if (CountUnits == 2) return waterwater; return water;
        //            case UnitTypes.Air: if (CountUnits == 2) return airair; return air;
        //            default: throw new NotImplementedException();
        //        }
        //    }
        //}
    }
}
