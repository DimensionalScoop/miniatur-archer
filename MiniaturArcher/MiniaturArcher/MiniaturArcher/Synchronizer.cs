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
using ProtoBuf;
using System.IO;
using X45Game.Strategics;
using System.Threading;

namespace MiniaturArcher
{
    enum SyncEvents:byte { TurnEnd,TurnBegin, UnitMoved, UnitCreated, Chat, GameStart }

    class Synchronizer:GameComponent
    {
        public static Map Map;
        public static UI Ui;

        int countPlayer { get { return net.CountConnections; } }
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

        const int port = 29735;
        const int timeout = 5000;

        public Synchronizer(Game game)
            : base(game)
        {
            Console.WriteLine("------------------------------");
            Console.WriteLine("Miniatur(e)-Hunter");
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

            countPlayer = 1;
            password=Console.ReadLine();

            while (userName == "")
            {
                Console.Write("Username: ");
                userName = Console.ReadLine();
            }
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

            OwnFraction = new Player(Player.playersEnumCount[0]);
            for (int i = 0; i < countPlayer; i++)
                NewEvent(SyncEvents.GameStart, (byte)Player.playersEnumCount[i + 1],i);
        }

        //internal void NewEvent(SyncEvents syncEvent, params object[] param) { NewEvent(syncEvent, -1, param); }
        internal void NewEvent<T1, T2, T3>(SyncEvents syncEvent, T1 param1, T2 param2, T3 param3, int? sentTo = null)
            //where T1 : struct
            //where T2 : struct
            //where T3 : struct
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

            msg.OutgoingMessage.Write(RawSerializer.RawSerialize(param1));
            msg.OutgoingMessage.Write(RawSerializer.RawSerialize(param2));
            msg.OutgoingMessage.Write(RawSerializer.RawSerialize(param3));

            net.Send(msg);
        }
        internal void NewEvent<T1, T2>(SyncEvents syncEvent, T1 param1, T2 param2, int? sentTo = null)
            //where T1 : struct
            //where T2 : struct
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

            msg.OutgoingMessage.Write(RawSerializer.RawSerialize(param1));
            msg.OutgoingMessage.Write(RawSerializer.RawSerialize(param2));

            net.Send(msg);
        }
        internal void NewEvent<T1>(SyncEvents syncEvent, T1 param1, int? sentTo = null)
            //where T1 : struct
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

            msg.OutgoingMessage.Write(RawSerializer.RawSerialize(param1));

            net.Send(msg);
        }
        internal void NewEvent(SyncEvents syncEvent, int? sentTo = null)
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

            net.Send(msg);
        }

        internal long GetId()
        {
            return IdBias + IdCounter++;
        }

        public override void Update(GameTime gameTime)
        {
            UpdateNetwork();
            
            base.Update(gameTime);
        }

        private void UpdateNetwork()
        {
            NetIncomingMessage msg;
            while ((msg = net.Receive()) != null)
            {
                msg.ReadByte();//get rid of the MessageType.UserData byte
                SyncEvents type = (SyncEvents)msg.ReadByte();
                ReceiveEvent(type, msg);
            }
        }

        void ReceiveEvent(SyncEvents syncEvent, NetIncomingMessage msg)
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
                    long id = -1; msg.ReadAllFields(id);
                    Point2 target = Point2.Zero; msg.ReadAllFields(target);

                    var unit = Map.Units.Find(p => p.Id == id);
                    unit.Move(Map[target]);
                    break;

                case SyncEvents.Chat:
                    string chat = ""; msg.ReadAllFields(chat);
                    Ui.Chat.Add(chat);
                    break;

                case SyncEvents.GameStart:
                    Debug.Assert(OwnFraction == null);
                    byte fraction = 0; 
                    OwnFraction = new Player((Players)RawSerializer.RawDeserialize<byte>(new byte[]{msg.ReadByte()}));
                    break;

                default: throw new NotImplementedException();
            }
        }
    }
}
