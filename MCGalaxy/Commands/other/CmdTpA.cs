//Copyright 2015 MCGalaxy
using System;
using System.Threading;

namespace MCGalaxy.Commands {  
    public sealed class CmdTpA : Command {        
        public override string name { get { return "tpa"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Other; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("tpaccept", "accept"), new CommandAlias("tpdeny", "deny") }; }
        }
        
        public override void Use(Player p, string message) {
            if (message == "") { Help(p); return; }
            
            if (message.CaselessEq("accept")) {
                DoAccept(p);
            } else if (message.CaselessEq("deny")) {
                DoDeny(p);
            } else {
                DoTpa(p, message);
            }
        }
        
        void DoTpa(Player p, string message) {
            Player target = PlayerInfo.FindMatches(p, message);
            if (target == null) return;
            if (target == p) { Player.Message(p, "You cannot /tpa to yourself."); return; }
            if (target.listignored.Contains(p.name)) { ShowSentMessage(p, target); return; }
            
            if (target.name == p.currentTpa) { 
                Player.Message(p, "You still have a pending teleport request with this player."); return; 
            }
            if (p.level != target.level && target.level.IsMuseum) {
                Player.Message(p, "Player \"{0}\" is in a museum.", target.ColoredName); return;
            }
            if (target.Loading) {
                Player.Message(p, "Waiting for {0} %Sto spawn...", target.ColoredName);
                target.BlockUntilLoad(10);
            }
            
            ShowSentMessage(p, target);
            ShowRequestMessage(p, target);
            target.senderName = p.name;
            target.Request = true;
            p.currentTpa = target.name;
            
            Thread.Sleep(90000);
            if (target.Request) {
                Player.Message(p, "Your teleport request has timed out.");
                Player.Message(target, "Pending teleport request has timed out.");
                target.Request = false;
                target.senderName = "";
                p.currentTpa = "";
            }
        }
        
        static void ShowSentMessage(Player p, Player target) {
            Player.Message(p, "Your teleport request has been sent to " + target.ColoredName);
            Player.Message(p, "This request will timeout after &b90 %Sseconds.");
        }
        
        static void ShowRequestMessage(Player p, Player target) {
            p.MessageTo(target, p.ColoredName + " %Swould like to teleport to you.");
            p.MessageTo(target, "Type &2/tpaccept %Sor &4/tpdeny%S.");
            p.MessageTo(target, "This request will timeout after &b90 %Sseconds.");
        }
        
        void DoAccept(Player p) {
            if (!p.Request) { Player.Message(p, "You do not have any pending teleport requests."); return; }
            
            Player sender = PlayerInfo.FindExact(p.senderName);
            p.Request = false;
            p.senderName = "";
            if (sender == null) {
                Player.Message(p, "The player who requested to teleport to you isn't online anymore."); return;
            }
            
            Player.Message(p, "You have accepted {0}%S's teleportation request.", sender.ColoredName);
            Player.Message(sender, "{0} %Shas accepted your request. Teleporting now...", p.ColoredName);            
            sender.currentTpa = "";
            Thread.Sleep(1000);
            if (p.level != sender.level) {
                PlayerActions.ChangeMap(sender, p.level);
                Thread.Sleep(1000);
            }

            sender.SendPos(Entities.SelfID, p.pos[0], p.pos[1], p.pos[2], p.rot[0], 0);
        }
        
        void DoDeny(Player p) {
            if (!p.Request) { Player.Message(p, "You do not have any pending teleport requests."); return; }
            
            Player sender = PlayerInfo.FindExact(p.senderName);
            p.Request = false;
            p.senderName = "";
            if (sender == null) {
                Player.Message(p, "The player who requested to teleport to you isn't online anymore."); return;
            }
            
            Player.Message(p, "You have denied {0}%S's teleportation request.", sender.ColoredName);
            Player.Message(sender, "{0} %Shas denied your request.", p.ColoredName);
            sender.currentTpa = "";            
        } 

        public override void Help(Player p) {
            Player.Message(p, "%T/tpa [player] %H- Sends a teleport request to that player");
            Player.Message(p, "%T/tpa accept %H- Accepts a teleport request");
            Player.Message(p, "%T/tpa deny %H- Denies a teleport request");
        }
    }
}
