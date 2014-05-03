using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace TeleportRequest
{
    [ApiVersion(1, 15)]
    public class TeleportRequest : TerrariaPlugin
    {
        public override string Name { get { return "TeleportRequest"; } }
        public override string Description { get { return "Allow requesting to teleport instead of immediate teleport"; } }
        public override string Author { get { return "AquaBlitz11"; } }
        public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        private Timer Timer;
        private Request[] Request = new Request[256];
        public Players[] Player = new Players[256];

        public TeleportRequest(Main game) : base(game)
        {
            
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
        }

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                Timer.Dispose();
            }
            base.Dispose(Disposing);
        }

        public void OnInitialize(EventArgs args)
        {
            Timer = new Timer();
            Timer.Interval = 1000;
            Timer.Elapsed += OnElapsed;
            //Timer.Start();

            Commands.ChatCommands.Add(new Command(TShockAPI.Permissions.tp, CMDTp, "tpa")
            {
                AllowServer = false,
                HelpText = "Sends a request to teleport to someone."
            });
            Commands.ChatCommands.Add(new Command(TShockAPI.Permissions.tphere, CMDTphere, "tpahere")
            {
                AllowServer = false,
                HelpText = "Sends a request for someone to teleport to you."
            });
            Commands.ChatCommands.Add(new Command(Permissions.tpcon, CMDAccept, "tpaccept")
            {
                AllowServer = false,
                HelpText = "Accepts a teleport request."
            });
            Commands.ChatCommands.Add(new Command(Permissions.tpcon, CMDDeny, "tpdeny")
            {
                AllowServer = false,
                HelpText = "Denies a teleport request."
            });
            Commands.ChatCommands.Add(new Command(Permissions.tpset, CMDSet, "tpset")
            {
                AllowServer = false,
                HelpText = "Set how teleport request behaves."
            });
        }

        public void OnJoin(JoinEventArgs args)
        {
            Player[args.Who] = new Players();
            Request[args.Who] = new Request();
        }
        public void OnLeave(LeaveEventArgs args)
        {
            Player[args.Who] = null;
            Request[args.Who] = null;
        }
        public void OnChat(ServerChatEventArgs e)
        {
            string[] text = e.Text.Split(' ');
            if (text[0] == "/tp")
            {
                string parameters = e.Text.Substring(3);
                Player[e.Who].AcknowledgeA = true;
                Commands.HandleCommand(TShock.Players[e.Who], "/tpa" + parameters);
                e.Handled = true;
            }
            else if (text[0] == "/tphere")
            {
                string parameters = e.Text.Substring(7);
                Player[e.Who].AcknowledgeA = true;
                Commands.HandleCommand(TShock.Players[e.Who], "/tpahere" + parameters);
                e.Handled = true;
            }
            else if (text[0] == "/tpallow")
            {
                if (!Player[e.Who].AcknowledgeSet)
                {
                    TShock.Players[e.Who].SendInfoMessage("Info : You can use /tpset to set how incoming request behaves.");
                }
                Player[e.Who].AcknowledgeSet = true;
                string parameters = e.Text.Substring(8);
                Commands.HandleCommand(TShock.Players[e.Who], "/tpset" + parameters);
                e.Handled = true;
            }
        }

        void OnElapsed(object sender, ElapsedEventArgs e)
        {
            bool TurnTimerOff = true;
            for (int i = 0; i < Request.Length; i++)
            {
                Request tpr = Request[i];
                if (tpr != null)
                {
                    if (tpr.Timeout != 0)
                    {
                        TurnTimerOff = false;
                    }
                }
            }
            if (TurnTimerOff)
            {
                Timer.Stop();
                return;
            }

            for (int i = 0; i < Request.Length; i++)
            {
                Request tpr = Request[i];
                if (tpr != null)
                {
                    if (tpr.Timeout > 0)
                    {
                        TSPlayer SenderID = TShock.Players[i];
                        TSPlayer ReceiverID = TShock.Players[tpr.ReceiverID];
                        if (tpr.Timeout == 15 || tpr.Timeout == 12 || tpr.Timeout == 9 || tpr.Timeout == 6 || tpr.Timeout == 3)
                        {
                            string msg = "{0} is requesting to teleport to you. (/tpaccept or /tpdeny)";
                            if (tpr.Direction)
                                msg = "You are requested to teleport to {0}. (/tpaccept or /tpdeny)";
                            ReceiverID.SendInfoMessage(msg, SenderID.Name);
                        }
                        tpr.Timeout--;
                        if (tpr.Timeout == 0)
                        {
                            tpr.Direction = false;
                            tpr.ReceiverID = 0;
                            tpr.Timeout = 0;
                            SenderID.SendErrorMessage("Your teleport request timed out.");
                            ReceiverID.SendErrorMessage("{0}'s teleport request timed out.", SenderID.Name);
                        }
                    }
                }
            }
        }

        void CMDTp(CommandArgs e)
        {
            if (e.Message.StartsWith("tpa") && !Player[e.Player.Index].AcknowledgeA)
            {
                e.Player.SendInfoMessage("Info : You can use /tp to request a teleport.");
                Player[e.Player.Index].AcknowledgeA = true;
            }

            if (e.Parameters.Count == 0)
            {
                e.Player.SendErrorMessage("Invalid syntax! Proper syntax: /tp <player>");
                return;
            }

            string PlayerName = String.Join(" ", e.Parameters.ToArray());
            List<TSPlayer> Players = TShock.Utils.FindPlayer(PlayerName);
            if (Players.Count == 0)
                e.Player.SendErrorMessage("Invalid Player!");
            else if (Players.Count > 1)
                e.Player.SendErrorMessage("More than one player matched!");
            else
            {
                var PlyIndex = Players[0].Index;
                if (e.Player.Index == PlyIndex)
                {
                    e.Player.SendErrorMessage("You cannot teleport to yourself.");
                    Timer.Start();
                    return;
                }
                if (!e.Player.Group.HasPermission(Permissions.tpsuper))
                {
                    if (Players[0].Group.HasPermission(Permissions.tpcon))
                    {
                        if (Player[PlyIndex].TPType == 0)
                        {
                            e.Player.SendErrorMessage("{0} has prevented your teleport request.", Players[0].Name);
                            Players[0].SendInfoMessage("{0} attempted to teleport to you.", e.Player.Name);
                        }
                        else if (Player[PlyIndex].TPType == 1)
                        {
                            for (int i = 0; i < Request.Length; i++)
                            {
                                Request tpr = Request[i];
                                if (tpr != null)
                                {
                                    if (tpr.Timeout > 0 && tpr.ReceiverID == PlyIndex)
                                    {
                                        e.Player.SendErrorMessage("{0} already has a teleport request. Try again later.", Players[0].Name);
                                        Players[0].SendInfoMessage("{0} attempted to teleport to you.", e.Player.Name);
                                        return;
                                    }
                                }
                            }
                            Request[e.Player.Index].Direction = false;
                            Request[e.Player.Index].ReceiverID = (byte)PlyIndex;
                            Request[e.Player.Index].Timeout = 15;
                            Timer.Start();
                            e.Player.SendSuccessMessage("Sent a teleport request to {0}.", Players[0].Name);
                        }
                        else
                        {
                            e.Player.SendSuccessMessage("Teleported to {0}.", Players[0].Name);
                            Players[0].SendInfoMessage("{0} teleported to you.", e.Player.Name);
                            Teleport(e.Player, Players[0]);
                        }
                    }
                    else
                    {
                        e.Player.SendSuccessMessage("Teleported to {0}.", Players[0].Name);
                        Players[0].SendInfoMessage("{0} teleported to you.", e.Player.Name);
                        Teleport(e.Player, Players[0]);
                    }
                }
                else
                {
                    e.Player.SendSuccessMessage("Teleported to {0}.", Players[0].Name);
                    Players[0].SendInfoMessage("{0} teleported to you.", e.Player.Name);
                    Teleport(e.Player, Players[0]);
                }
            }
        }

        void CMDTphere(CommandArgs e)
        {
            if (e.Message.StartsWith("tpahere") && !Player[e.Player.Index].AcknowledgeA)
            {
                e.Player.SendInfoMessage("Info : You can use /tphere to request a teleport to you.");
                Player[e.Player.Index].AcknowledgeA = true;
            }

            if (e.Parameters.Count == 0)
            {
                e.Player.SendErrorMessage("Invalid syntax! Proper syntax: /tphere <player>");
                return;
            }

            string PlayerName = String.Join(" ", e.Parameters.ToArray());
            List<TSPlayer> Players = TShock.Utils.FindPlayer(PlayerName);
            if (Players.Count == 0)
                e.Player.SendErrorMessage("Invalid Player!");
            else if (Players.Count > 1)
                e.Player.SendErrorMessage("More than one player matched!");
            else
            {
                var PlyIndex = Players[0].Index;
                if (e.Player.Index == PlyIndex)
                {
                    e.Player.SendErrorMessage("You cannot teleport yourself here. You're already here.");
                    Timer.Start();
                    return;
                }
                if (!e.Player.Group.HasPermission(Permissions.tpsuper))
                {
                    if (Players[0].Group.HasPermission(Permissions.tpcon))
                    {
                        if (Player[PlyIndex].TPHereType == 0)
                        {
                            e.Player.SendErrorMessage("{0} has prevented your teleport request.", Players[0].Name);
                            Players[0].SendInfoMessage("{0} attempted to teleport you.", e.Player.Name);
                        }
                        else if (Player[PlyIndex].TPHereType == 1)
                        {
                            for (int i = 0; i < Request.Length; i++)
                            {
                                Request tpr = Request[i];
                                if (tpr != null)
                                {
                                    if (tpr.Timeout > 0 && tpr.ReceiverID == PlyIndex)
                                    {
                                        e.Player.SendErrorMessage("{0} already has a teleport request. Try again later.", Players[0].Name);
                                        Players[0].SendInfoMessage("{0} attempted to teleport you.", e.Player.Name);
                                        return;
                                    }
                                }
                            }
                            Request[e.Player.Index].Direction = true;
                            Request[e.Player.Index].ReceiverID = (byte)PlyIndex;
                            Request[e.Player.Index].Timeout = 15;
                            Timer.Start();
                            e.Player.SendSuccessMessage("Sent a teleport here request to {0}.", Players[0].Name);
                        }
                        else
                        {
                            Players[0].SendInfoMessage("You were teleported to {0}.", e.Player.Name);
                            e.Player.SendSuccessMessage("You teleported {0} here.", Players[0].Name);
                            Teleport(Players[0], e.Player);
                        }
                    }
                    else
                    {
                        e.Player.SendErrorMessage("You cannot teleported {0} here.", Players[0].Name);
                    }
                }
                else
                {
                    Players[0].SendInfoMessage("You were teleported to {0}.", e.Player.Name);
                    e.Player.SendSuccessMessage("You teleported {0} here.", Players[0].Name);
                    Teleport(Players[0], e.Player);
                }
            }
        }

        void CMDAccept(CommandArgs e)
        {
            for (int i = 0; i < Request.Length; i++)
            {
                Request tpr = Request[i];
                if (tpr != null)
                {
                    if (tpr.Timeout > 0 && tpr.ReceiverID == e.Player.Index)
                    {
                        TSPlayer plr2 = tpr.Direction ? TShock.Players[i] : e.Player;
                        TSPlayer plr1 = tpr.Direction ? e.Player : TShock.Players[i];
                        if (!tpr.Direction)
                        {
                            plr1.SendSuccessMessage("Teleported to {0}.", plr2.Name);
                            plr2.SendInfoMessage("{0} teleported to you.", plr1.Name);
                        }
                        else
                        {
                            plr1.SendInfoMessage("You were teleported to {0}.", plr2.Name);
                            plr2.SendSuccessMessage("You teleported {0} here.", plr1.Name);
                        }
                        Teleport(plr1, plr2);
                        tpr.Timeout = 0;
                        return;
                    }
                }
            }
            e.Player.SendErrorMessage("You have no pending teleport requests.");
        }

        void CMDDeny(CommandArgs e)
        {
            for (int i = 0; i < Request.Length; i++)
            {
                Request tpr = Request[i];
                if (tpr != null)
                {
                    if (tpr.Timeout > 0 && tpr.ReceiverID == e.Player.Index)
                    {
                        e.Player.SendSuccessMessage("Denied {0}'s teleport request.", TShock.Players[i].Name);
                        TShock.Players[i].SendErrorMessage("{0} denied your teleport request.", e.Player.Name);
                        tpr.Timeout = 0;
                        return;
                    }
                }
            }
            e.Player.SendErrorMessage("You have no pending teleport requests.");
        }
        void CMDSet(CommandArgs e)
        {
            Player[e.Player.Index].AcknowledgeSet = true;
            if (e.Parameters.Count < 2)
            {
                e.Player.SendErrorMessage("Invalid Syntax! Syntax : /tpset <tp/tphere> <off/on/always>");
            }
            else
            {
                if (e.Parameters[0] == "tp")
                {
                    if (e.Parameters[1] == "off")
                    {
                        Player[e.Player.Index].TPType = 0;
                        e.Player.SendSuccessMessage("Incoming teleport request will be blocked.");
                    }
                    else if (e.Parameters[1] == "on")
                    {
                        Player[e.Player.Index].TPType = 1;
                        e.Player.SendSuccessMessage("Incoming teleport request is now allowed.");
                    }
                    else if (e.Parameters[1] == "always")
                    {
                        Player[e.Player.Index].TPType = 2;
                        e.Player.SendSuccessMessage("People can now teleport to you without requesting.");
                    }
                    else
                    {
                        e.Player.SendErrorMessage("Invalid Syntax! Syntax : /tpset <tp/tphere> <off/on/always>");
                    }
                }
                else if (e.Parameters[0] == "tphere")
                {
                    if (e.Parameters[1] == "off")
                    {
                        Player[e.Player.Index].TPHereType = 0;
                        e.Player.SendSuccessMessage("Incoming teleport here request will be blocked.");
                    }
                    else if (e.Parameters[1] == "on")
                    {
                        Player[e.Player.Index].TPHereType = 1;
                        e.Player.SendSuccessMessage("Incoming teleport here request is now allowed.");
                    }
                    else if (e.Parameters[1] == "always")
                    {
                        Player[e.Player.Index].TPHereType = 2;
                        e.Player.SendSuccessMessage("People can now teleport you to them without requesting.");
                    }
                    else
                    {
                        e.Player.SendErrorMessage("Invalid Syntax! Syntax : /tpset <tp/tphere> <off/on/always>");
                    }
                }
                else
                {
                    e.Player.SendErrorMessage("Invalid Syntax! Syntax : /tpset <tp/tphere> <off/on/always>");
                }
            }
        }

        void Teleport(TSPlayer plr1, TSPlayer plr2)
        {
            plr1.Teleport(plr2.X, plr2.Y);
        }
    }
}
