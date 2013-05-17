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
    enum SyncEvents { TurnEnd,TurnBegin, UnitMoved, UnitCreated }

    class Synchronizer:GameComponent
    {
        public static Map Map;

        int countPlayer;
        int countTurnEnds;


        internal void NewEvent(SyncEvents syncEvent, params object[] param)
        {
            switch (syncEvent)
            {
                case SyncEvents.TurnEnd:
                    countTurnEnds++;
                    break;

                case SyncEvents.UnitMoved:
                    long m0=(long)param[0];
                    Point2 m1 = (Point2)param[1];
                    break;

                default: throw new NotImplementedException();
            }
        }

        internal long GetId()
        {
            return 0;
        }

        void ReceiveEvent(SyncEvents syncEvent,object[] param)
        {
            switch (syncEvent)
            {
                case SyncEvents.TurnEnd:
                    countTurnEnds++;
                    if (countTurnEnds == countPlayer)
                    {
                        countTurnEnds = 0;
                        Map.TurnBegins();
                    }
                    break;

                case SyncEvents.UnitMoved:
                    long id=(long)param[0];
                    Point2 target=(Point2)param[1];

                    var unit=Map.Units.Find(p => p.Id == id);
                    unit.Move(Map[target]);
                    break;

                default: throw new NotImplementedException();
            }
        }
    }
}
