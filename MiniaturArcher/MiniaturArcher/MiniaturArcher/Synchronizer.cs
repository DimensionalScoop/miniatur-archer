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
using Lidgren.Network;
using X45Game.Network;
using System.IO;
using X45Game.Strategics;
using System.Threading;

namespace MiniaturArcher
{
    enum SyncEvents:byte { TurnEnd,TurnBegin, UnitMoved, UnitSummoned, Chat, GameStart, ConnectionId }

    class Synchronizer:GameComponent
    {
        public static Map Map;
        public static UI Ui;

        public int CountTotalPlayers { get { return net.CountConnections+1; } }
        public int CountOtherPlayers { get { return net.CountConnections; } }
        int countTurnEnds;

        NetworkManager net;
        public Player OwnFraction;
        string userName="";

        int IdCounter;
        long IdBias
        {
            get
            {
                return int.MaxValue * (byte)OwnFraction.Id;
            }
        }

        public Dictionary<NetConnection, Player> PlayersByConnection = new Dictionary<NetConnection, Player>();

        const int port = 6669;
        const int timeout = 5000;

        public Synchronizer(Game game)
            : base(game)
        {
            Console.WriteLine("------------------------------");
            Console.WriteLine("Miniatur(e)-Archer");
            Console.WriteLine("(c) by Max Pernklau");
            Console.WriteLine("------------------------------\n");

            InitializeConnection();
        }

        protected override void Dispose(bool disposing)
        {
            net.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeConnection()
        {  
            string password = "";
#if !DEBUG
            Console.Write("Set password for network game: ");

            password=Console.ReadLine();

            while (userName == "")
            {
                Console.Write("Username: ");
                userName = Console.ReadLine();
            }
#else
            userName = "DEBUG";
#endif
            net = new NetworkManager("Miniatur-Archer", port, password);

            ConsoleKey answer = ConsoleKey.Spacebar;
            while (answer != ConsoleKey.S && answer != ConsoleKey.J)
            {
                Console.WriteLine("Create a server (s) or join (j) an existing server?");
                answer = Console.ReadKey().Key;
            }

            if (answer == ConsoleKey.S) WaitForClients();
            else JoinServer();
        }

        private void JoinServer()
        {
            while (!net.IsConnected)
            {
                Console.Write("Server IP (type 'a' for auto discovery): ");
                string ip = Console.ReadLine();

                if (ip == "")
                    continue;
                else if (ip.ToLower() == "a")
                {
                    Console.WriteLine("Running discovery and trying to connect...");
                    net.EnablePeerDiscovery();
                }
                else
                {
                    Console.WriteLine("Trying to connect...");
                    net.Connect(ip);
                }

                Thread.Sleep(timeout);
                if (net.IsConnected) Console.WriteLine("Connected!");
                else Console.WriteLine("Could not find server");
                net.DisablePeerDiscovery();
            }

            Console.WriteLine("Waiting for server to start game...");

            while (OwnFraction == null) //The game starts when the server sends the fraction information
                UpdateNetwork();
        }

        private void WaitForClients()
        {
            Console.WriteLine("Created server");
            Console.WriteLine("Waiting for clients to connect");
            Console.WriteLine("Press 'M' to start game");

            while (!(Console.KeyAvailable&&Console.ReadKey().Key== ConsoleKey.M))
            {
                Thread.Sleep(100);
                Console.Write("\rConnected clients: " + net.CountConnections + "        ");
            }

            OwnFraction = new Player(Player.playersEnumCount[0],userName);
            for (int i = 0; i < CountOtherPlayers; i++)
                NewEvent(SyncEvents.GameStart, (byte)Player.playersEnumCount[i + 1],i);
            NewEvent(SyncEvents.ConnectionId, (byte)OwnFraction.Id, userName);
        }

        internal void NewEvent<T1, T2, T3, T4>(SyncEvents syncEvent, T1 param1, T2 param2, T3 param3,T4 param4, int? sentTo = null)
        {
            var msg = PrepareMessage(syncEvent, sentTo);

            msg.OutgoingMessage.WriteT(param1);
            msg.OutgoingMessage.WriteT(param2);
            msg.OutgoingMessage.WriteT(param3);
            msg.OutgoingMessage.WriteT(param4);

            net.Send(msg);
        }
        internal void NewEvent<T1, T2, T3>(SyncEvents syncEvent, T1 param1, T2 param2, T3 param3, int? sentTo = null)
        {
            var msg = PrepareMessage(syncEvent, sentTo);

            msg.OutgoingMessage.WriteT(param1);
            msg.OutgoingMessage.WriteT(param2);
            msg.OutgoingMessage.WriteT(param3);

            net.Send(msg);
        }
        internal void NewEvent<T1, T2>(SyncEvents syncEvent, T1 param1, T2 param2, int? sentTo = null)
        {
            var msg = PrepareMessage(syncEvent, sentTo);

            msg.OutgoingMessage.WriteT(param1);
            msg.OutgoingMessage.WriteT(param2);

            net.Send(msg);
        }
        internal void NewEvent<T1>(SyncEvents syncEvent, T1 param1, int? sentTo = null)
        {
            var msg = PrepareMessage(syncEvent, sentTo);

            msg.OutgoingMessage.WriteT(param1);

            net.Send(msg);
        }
        internal void NewEvent(SyncEvents syncEvent, int? sentTo = null)
        {
            var msg = PrepareMessage(syncEvent, sentTo);

            net.Send(msg);
        }
        NetOutgoingMessageWithDeliveryMethod PrepareMessage(SyncEvents syncEvent, int? sentTo = null)
        {
            switch (syncEvent)
            {
                case SyncEvents.TurnEnd:
                    countTurnEnds++;
                    break;
            }

            var msg = net.CreateMessage(MessageTypes.UserData);
            msg.TargetHost = sentTo;
            msg.OutgoingMessage.Write((byte)syncEvent);
            return msg;
        }


        internal long GetId()
        {
            return IdBias + IdCounter++;
        }

        public override void Update(GameTime gameTime)
        {
            UpdateNetwork();

            UpdateTurns(gameTime);
            
            base.Update(gameTime);
        }

        private void UpdateTurns(GameTime gameTime)
        {
            if (countTurnEnds == CountTotalPlayers)
            {
                countTurnEnds = 0;
                Map.TurnBegins();
            }
        }

        private void UpdateNetwork()
        {
            NetIncomingMessage msg;
            while ((msg = net.Receive()) != null)
            {
                msg.ReadByte();//get rid of the MessageType.UserData byte
                SyncEvents type = (SyncEvents)msg.ReadByte();
                ReceiveEvent(type, msg);
                Console.WriteLine("Received message: " + type.ToString());
            }
        }

        void ReceiveEvent(SyncEvents syncEvent, NetIncomingMessage msg)
        {
            switch (syncEvent)
            {
                case SyncEvents.TurnEnd:
                    countTurnEnds++;
                    return;

                case SyncEvents.UnitSummoned:
                    EventUnitSummoned(msg);
                    return;

                case SyncEvents.UnitMoved:
                    //long id = -1; msg.ReadAllFields(id);
                    //Point2 target = Point2.Zero; msg.ReadAllFields(target);

                    //var unit = Map.Units.Find(p => p.Id == id);
                    //unit.Move(Map[target]);
                    return;

                case SyncEvents.Chat:
                    Ui.NewChatMessage(msg.ReadString());
                    return;

                case SyncEvents.GameStart:
                    EventGameStart(msg);
                    return;

                case SyncEvents.ConnectionId:
                    EventConnectionId(msg);
                    return;

                default: throw new NotImplementedException();
            }
        }

        private void EventConnectionId(NetIncomingMessage msg)
        {
            Players id = (Players)msg.ReadByte();
            string name = msg.ReadString();//RawSerializer.RawDeserialize<string>(msg.Data, msg.PositionInBytes);
            Debug.Assert(!PlayersByConnection.ContainsKey(msg.SenderConnection));
            PlayersByConnection.Add(msg.SenderConnection, new Player(id, name));
        }

        private void EventGameStart(NetIncomingMessage msg)
        {
            Debug.Assert(OwnFraction == null);
            OwnFraction = new Player((Players)msg.ReadByte(),userName);
            NewEvent(SyncEvents.ConnectionId, (byte)OwnFraction.Id, userName);
        }

        private void EventUnitSummoned(NetIncomingMessage msg)
        {
            UnitTypes type = (UnitTypes)msg.ReadByte();
            Player fraction = Map.Sync.PlayersByConnection[msg.SenderConnection];
            long id = msg.ReadT<long>();
            Point2 pos = msg.ReadT<Point2>();
            Map[pos].Summon(new Unit(pos, type, fraction, id),true);
        }
    }
}
