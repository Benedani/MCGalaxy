﻿/*
Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
Dual-licensed under the Educational Community License, Version 2.0 and
the GNU General Public License, Version 3 (the "Licenses"); you may
not use this file except in compliance with the Licenses. You may
obtain a copy of the Licenses at
http://www.opensource.org/licenses/ecl2.php
http://www.gnu.org/licenses/gpl-3.0.html
Unless required by applicable law or agreed to in writing,
software distributed under the Licenses are distributed on an "AS IS"
BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
or implied. See the Licenses for the specific language governing
permissions and limitations under the Licenses.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MCGalaxy.DB;
using MCGalaxy.Games;
using MCGalaxy.SQL;
using MCGalaxy.Undo;

namespace MCGalaxy {
    public sealed partial class Player : IDisposable {

        public void IncrementBlockStats(byte block, bool drawn) {
            loginBlocks++;
            overallBlocks++;
            
            if (drawn) TotalDrawn++;
            else if (block == 0) TotalDeleted++;
            else TotalPlaced++;
        }
        
        public byte GetActualHeldBlock(out byte extBlock) {
            byte block = RawHeldBlock;
            extBlock = 0;
            if (modeType != 0) return modeType;    
            
            if (block < Block.CpeCount) return bindings[block];
            extBlock = block;
            return Block.custom_block;            
        }
        
        public static string CheckPlayerStatus(Player p) {
            if ( p.hidden ) return "hidden";
            if ( p.IsAfk ) return "afk";
            return "active";
        }
        
        public void SetPrefix() {
            prefix = Game.Referee ? "&2[Ref] " : "";
            if (group.prefix != "") prefix += "&f" + group.prefix + color;
            Team team = Game.Team;
            prefix += team != null ? "<" + team.Color + team.Name + color + "> " : "";
            
            IGame game = level == null ? null : level.CurrentGame();
            if (game != null) game.AdjustPrefix(this, ref prefix);
            
            bool isOwner = Server.server_owner.CaselessEq(name);
            string viptitle =
                isMod ? string.Format("{0}[&aInfo{0}] ", color) :                
                isDev ? string.Format("{0}[&9Dev{0}] ", color) :
                isOwner ? string.Format("{0}[&cOwner{0}] ", color) : "";
            prefix = prefix + viptitle;
            prefix = (title == "") ? prefix : prefix + color + "[" + titlecolor + title + color + "] ";
        }
        
        public bool CheckIfInsideBlock() {
            ushort x = (ushort)(pos[0] / 32), y = (ushort)(pos[1] / 32), z = (ushort)(pos[2] / 32);
            byte head = level.GetTile(x, y, z);
            byte feet = level.GetTile(x, (ushort)(y - 1), z);

            return !(Block.Walkthrough(Block.Convert(head)) || head == Block.Invalid)
                && !(Block.Walkthrough(Block.Convert(feet)) || feet == Block.Invalid);
        }
        static int sessionCounter;

        //This is so that plugin devs can declare a player without needing a socket..
        //They would still have to do p.Dispose()..
        public Player(string playername) { 
            name = playername;
            truename = playername;
            DisplayName = playername;
            SessionID = Interlocked.Increment(ref sessionCounter) & SessionIDMask;
            spamChecker = new SpamChecker(this);
        }

        public Player(Socket s) {
            spamChecker = new SpamChecker(this);
            try {
                socket = s;
                ip = socket.RemoteEndPoint.ToString().Split(':')[0];
                SessionID = Interlocked.Increment(ref sessionCounter) & SessionIDMask;
                Server.s.Log(ip + " connected to the server.");

                for (byte i = 0; i < Block.CpeCount; i++) bindings[i] = i;

                socket.BeginReceive(tempbuffer, 0, tempbuffer.Length, SocketFlags.None, new AsyncCallback(Receive), this);
                InitTimers();
                connections.Add(this);
            }
            catch ( Exception e ) { Leave("Login failed!"); Server.ErrorLog(e); }
        }

        public void save() {
            if (MySQLSave != null) MySQLSave(this, "");
            OnMySQLSaveEvent.Call(this, "");
            if (cancelmysql) { cancelmysql = false; return; }

            long blocks = PlayerData.BlocksPacked(TotalPlaced, overallBlocks);
            long cuboided = PlayerData.CuboidPacked(TotalDeleted, TotalDrawn);
            Database.Backend.UpdateRows("Players", "IP=@0, LastLogin=@1, totalLogin=@2, totalDeaths=@3, Money=@4, " +
                                        "totalBlocks=@5, totalCuboided=@6, totalKicked=@7, TimeSpent=@8", "WHERE Name=@9", 
                                        ip, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                        totalLogins, overallDeath, money, blocks,
                                        cuboided, totalKicked, time.ToDBTime(), name);
            
            Server.zombie.SaveZombieStats(this);
            SaveUndo(this);
        }

        #region == GLOBAL MESSAGES ==
        
        public static void GlobalBlockchange(Level level, int b, byte block, byte extBlock) {
            ushort x, y, z;
            level.IntToPos(b, out x, out y, out z);
            GlobalBlockchange(level, x, y, z, block, extBlock);
        }
        
        public static void GlobalBlockchange(Level level, ushort x, ushort y, ushort z, 
                                             byte block, byte extBlock) {
            Player[] players = PlayerInfo.Online.Items; 
            foreach (Player p in players) { 
                if (p.level == level) p.SendBlockchange(x, y, z, block, extBlock);
            }
        }

        [Obsolete("Use SendChatFrom() instead.")]
        public static void GlobalChat(Player from, string message) { SendChatFrom(from, message, true); }
        [Obsolete("Use SendChatFrom() instead.")]
        public static void GlobalChat(Player from, string message, bool showname) { SendChatFrom(from, message, showname); }
        
        public static void SendChatFrom(Player from, string message) { SendChatFrom(from, message, true); }
        public static void SendChatFrom(Player from, string message, bool showname) {
            if (from == null) return;            
            if (Last50Chat.Count == 50) Last50Chat.RemoveAt(0);
            var chatmessage = new ChatMessage();
            chatmessage.text = message;
            chatmessage.username = from.color + from.name;
            chatmessage.time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            Last50Chat.Add(chatmessage);
            
            string msg_NT = message, msg_NN = message, msg_NNNT = message;
            if (showname) {
                string msg = ": &f" + message;
                string pre = from.color + from.prefix;     
                message = pre + from.DisplayName + msg; // Titles + Nickname
                msg_NN = pre + from.truename + msg; // Titles + Account name
                
                pre = from.group.prefix == "" ? "" : "&f" + from.group.prefix;
                msg_NT = pre + from.color + from.DisplayName + msg; // Nickname
                msg_NNNT = pre + from.color + from.truename + msg; // Account name
            }
            
            Player[] players = PlayerInfo.Online.Items; 
            foreach (Player p in players) {
                if (p.Chatroom != null || p.listignored.Contains(from.name)) continue;
                if (!p.level.worldChat || (p.ignoreAll && p != from)) continue;
                
                if (p.ignoreNicks && p.ignoreTitles) Player.Message(p, msg_NNNT);
                else if (p.ignoreNicks) Player.Message(p, msg_NN);
                else if (p.ignoreTitles) Player.Message(p, msg_NT);
                else Player.Message(p, message);
            }
        }

        public static List<ChatMessage> Last50Chat = new List<ChatMessage>();
        [Obsolete("Use Chat.MessageAll() instead")]
        public static void GlobalMessage(string message) { Chat.MessageAll(message); }
        [Obsolete("Use Chat.MessageAll() instead")]
        public static void GlobalMessage(string message, bool global) { Chat.MessageAll(message); }
        
        public static void GlobalIRCMessage(string message) {
            message = Colors.EscapeColors(message);
            Player[] players = PlayerInfo.Online.Items; 
            foreach (Player p in players) {
                if (p.ignoreAll || p.ignoreIRC) continue;
                
                if (p.level.worldChat && p.Chatroom == null)
                    p.SendMessage(message);
            }
        }
        
        public static void GlobalMessage(Player from, string message) {
            if (from == null) Chat.MessageAll(message);
            else SendChatFrom(from, message, false);
        }
        
        
        public static void GlobalSpawn(Player p, bool self, string possession = "") {
           Entities.GlobalSpawn(p, self, possession);
        }
        
        public static void GlobalSpawn(Player p, ushort x, ushort y, ushort z, 
                                       byte rotx, byte roty, bool self, string possession = "") {
            Entities.GlobalSpawn(p, x, y, z, rotx, roty, self, possession);
        }
        
        internal void SpawnEntity(Player p, byte id, ushort x, ushort y, ushort z, 
                                       byte rotx, byte roty, string possession = "") {
            Entities.Spawn(this, p, id, x, y, z, rotx, roty, possession);
        }
        
        internal void DespawnEntity(byte id) { Entities.Despawn(this, id); }
        
        public static void GlobalDespawn(Player p, bool self) { Entities.GlobalDespawn(p, self); }

        public bool MarkPossessed(string marker = "") {
            if (marker != "") {
                Player controller = PlayerInfo.FindExact(marker);
                if (controller == null) return false;
                marker = " (" + controller.color + controller.name + color + ")";
            }
            
            Entities.GlobalDespawn(this, true);
            Entities.GlobalSpawn(this, true, marker);
            return true;
        }

        #endregion
        #region == DISCONNECTING ==
        
        /// <summary> Disconnects the players from the server, 
        /// with their default logout message shown in chat. </summary>
        public void Disconnect() { LeaveServer(PlayerDB.GetLogoutMessage(this), "disconnected", false); }
        
        /// <summary> Kicks the player from the server,
        /// with the given messages shown in chat and in the disconnect packet. </summary>
        public void Kick(string chatMsg, string discMsg, bool sync = false) {
            LeaveServer(chatMsg, discMsg, true, sync); 
        }

        /// <summary> Kicks the player from the server,
        /// with the given message shown in both chat and in the disconnect packet. </summary>
        public void Kick(string discMsg) { Kick(discMsg, false); }
        public void Kick(string discMsg, bool sync = false) {
            string chatMsg = discMsg;
            if (chatMsg != "") chatMsg = "(" + chatMsg + ")"; // old format
            LeaveServer(chatMsg, discMsg, true, sync);
        }
        
        /// <summary> Disconnects the players from the server,
        /// with the given message shown in both chat and in the disconnect packet. </summary>
        public void Leave(string chatMsg, string discMsg, bool sync = false) { 
            LeaveServer(chatMsg, discMsg, false, sync); 
        }

        /// <summary> Disconnects the players from the server,
        /// with the given messages shown in chat and in the disconnect packet. </summary>        
        public void Leave(string discMsg) { Leave(discMsg, false); }       
        public void Leave(string discMsg, bool sync = false) {
            LeaveServer(discMsg, discMsg, false, sync);
        }
        
        [Obsolete("Use Leave() or Kick() instead")]
        public void leftGame(string discMsg = "") {
            string chatMsg = discMsg;
            if (chatMsg != "") chatMsg = "(" + chatMsg + ")"; // old format
            LeaveServer(chatMsg, discMsg, true); 
        }
        

        bool leftServer = false;
        void LeaveServer(string chatMsg, string discMsg, bool isKick, bool sync = false) {
            if (leftServer) return;
            leftServer = true;
            if (chatMsg != null) chatMsg = Colors.EscapeColors(chatMsg);
            discMsg = Colors.EscapeColors(discMsg);
            
            OnPlayerDisconnectEvent.Call(this, discMsg);
            //Umm...fixed?
            if (name == "") {
                if (socket != null) CloseSocket();
                connections.Remove(this);
                SaveUndo(this);
                disconnected = true;
                return;
            }
            
            Server.reviewlist.Remove(name);
            try { 
                if (disconnected) {
                    CloseSocket();
                    connections.Remove(this);
                    return;
                }
                // FlyBuffer.Clear();
                DisposeTimers();
                LastAction = DateTime.UtcNow;
                IsAfk = false;
                isFlying = false;
                aiming = false;
                
                bool cp437 = HasCpeExt(CpeExt.FullCP437);
                string kickPacketMsg = ChatTokens.Apply(discMsg, this);
                Send(Packet.Kick(discMsg, cp437), sync);
                disconnected = true;
                if (isKick) totalKicked++;
                
                if (!loggedIn) {
                    connections.Remove(this);
                    RemoveFromPending();
                    
                    string user = String.IsNullOrEmpty(name) ? ip : name + " (" + ip + ")";
                    Server.s.Log(user + " disconnected. (" + discMsg + ")");
                    return;
                }

                Server.zombie.PlayerLeftServer(this);
                if ( Game.team != null ) Game.team.RemoveMember(this);
                Server.Countdown.PlayerLeftServer(this);
                TntWarsGame tntwarsgame = TntWarsGame.GetTntWarsGame(this);
                if ( tntwarsgame != null ) {
                    tntwarsgame.Players.Remove(tntwarsgame.FindPlayer(this));
                    tntwarsgame.SendAllPlayersMessage("TNT Wars: " + ColoredName + " %Shas left TNT Wars!");
                }

                Entities.DespawnEntities(this, false);
                ShowDisconnectInChat(chatMsg, isKick);

                try { save(); }
                catch ( Exception e ) { Server.ErrorLog(e); }

                PlayerInfo.Online.Remove(this);
                Server.s.PlayerListUpdate();
                if (PlayerDisconnect != null)
                    PlayerDisconnect(this, discMsg);
                if (Server.AutoLoad && level.unload && !level.IsMuseum && IsAloneOnCurrentLevel())
                    level.Unload(true);
                Dispose();
            } catch ( Exception e ) { 
                Server.ErrorLog(e); 
            } finally {
                CloseSocket();
            }
        }
        
        void ShowDisconnectInChat(string chatMsg, bool isKick) {
            if (chatMsg == null) return;
            
            if (!isKick) {
                string leavem = "&c- " + FullName + " %S" + chatMsg;
                const LevelPermission perm = LevelPermission.Guest;
                if (group.Permission > perm || (Server.guestLeaveNotify && group.Permission <= perm)) {
                    Player[] players = PlayerInfo.Online.Items;
                    foreach (Player pl in players) {
                        if (Entities.CanSee(pl, this)) Player.Message(pl, leavem);
                    }
                }
                Server.s.Log(name + " disconnected (" + chatMsg + "%S).");
            } else {
                SendChatFrom(this, "&c- " + FullName + " %Skicked %S" + chatMsg, false);
                Server.s.Log(name + " kicked (" + chatMsg + "%S).");
            }
        }
        
        public static void SaveUndo(Player p) {
            try {
                UndoFormat.SaveUndo(p);
            } catch (Exception e) { 
                Server.s.Log("Error saving undo data for " + p.name + "!"); Server.ErrorLog(e); 
            }
        }

        public void Dispose() {
            connections.Remove(this);
            RemoveFromPending();
            Extras.Clear();
            if (CopyBuffer != null)
                CopyBuffer.Clear();
            DrawOps.Clear();
            UndoBuffer.Clear();
            spamChecker.Clear();
            spyChatRooms.Clear();
        }

        public bool IsAloneOnCurrentLevel() {
            lock (PlayerInfo.Online.locker) {
                Player[] players = PlayerInfo.Online.Items;
                foreach (Player p in players) {
                    if (p != this && p.level == level) return false;
                }
                return true;
            }
        }

        #endregion
        #region == OTHER ==
        
        [Obsolete("Use PlayerInfo.Online.Items")]
        public static List<Player> players;
        
        [Obsolete("Use PlayerInfo.Find(name)")]
        public static Player Find(string name) { return PlayerInfo.Find(name); }
        
        [Obsolete("Use PlayerInfo.FindExact(name)")]
        public static Player FindExact(string name) { return PlayerInfo.FindExact(name); }
        
        [Obsolete("Use PlayerInfo.FindNick(name)")]
        public static Player FindNick(string name) { return PlayerInfo.FindNick(null, name); }
        
        unsafe static byte NextFreeId() {
            byte* used = stackalloc byte[256];
            for (int i = 0; i < 256; i++)
                used[i] = 0;

            // Lock to ensure that no two players can end up with the same playerid
            lock (PlayerInfo.Online.locker) {
                Player[] players = PlayerInfo.Online.Items;
                for (int i = 0; i < players.Length; i++) {
                    byte id = players[i].id;
                    used[id] = 1;
                }
            }
            
            for (byte i = 0; i < 255; i++ ) {
                if (used[i] == 0) return i;
            }
            return 1;
        }

        public static bool ValidName(string name) {
            const string valid = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890._+";
            foreach (char c in name) {
                if (valid.IndexOf(c) == -1) return false;
            }
            return true;
        }

        public static int GetBannedCount() {
            Group group = Group.findPerm(LevelPermission.Banned);
            return group == null ? 0 : group.playerList.Count;
        }
        #endregion

        public void BlockUntilLoad(int sleep) {
            while (Loading) 
                Thread.Sleep(sleep);
        }
        public void RevertBlock(ushort x, ushort y, ushort z) {
            byte b = level.GetTile(x, y, z);
            SendBlockchange(x, y, z, b);
        }

        public static bool IPInPrivateRange(string ip) {
            //range of 172.16.0.0 - 172.31.255.255
            if (ip.StartsWith("172.") && (int.Parse(ip.Split('.')[1]) >= 16 && int.Parse(ip.Split('.')[1]) <= 31))
                return true;
            return IPAddress.IsLoopback(IPAddress.Parse(ip)) || ip.StartsWith("192.168.") || ip.StartsWith("10.");
            //return IsLocalIpAddress(ip);
        }

        public static bool IsLocalIpAddress(string host) {
            try { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach ( IPAddress hostIP in hostIPs ) {
                    // is localhost
                    if ( IPAddress.IsLoopback(hostIP) ) return true;
                    // is local address
                    foreach ( IPAddress localIP in localIPs ) {
                        if ( hostIP.Equals(localIP) ) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public bool EnoughMoney(int amount) {
            return money >= amount;
        }
        
        public void OnMoneyChanged() {
            if (Server.zombie.Running) Server.zombie.PlayerMoneyChanged(this);
            if (Server.lava.active) Server.lava.PlayerMoneyChanged(this);
        }

        public void TntAtATime() {
            CurrentAmountOfTnt++;
            int delay = 0;

            switch (TntWarsGame.GetTntWarsGame(this).GameDifficulty) {
                case TntWarsGame.TntWarsDifficulty.Easy:
                    delay = 3250; break;
                case TntWarsGame.TntWarsDifficulty.Normal:
                    delay = 2250; break;
                case TntWarsGame.TntWarsDifficulty.Hard:
                case TntWarsGame.TntWarsDifficulty.Extreme:
                    delay = 1250; break;
            }
            Server.MainScheduler.QueueOnce(AllowMoreTntTask, null, 
                                           TimeSpan.FromMilliseconds(delay));
        }
        
        void AllowMoreTntTask(SchedulerTask task) {
            CurrentAmountOfTnt--;
        }
        
        internal static bool CheckVote(string message, Player p, string a, string b, ref int totalVotes) {
            if (!p.voted && (message == a || message == b)) {
                totalVotes++;
                p.SendMessage(Colors.red + "Thanks for voting!");
                p.voted = true;
                return true;
            }
            return false;
        }

        internal void RemoveInvalidUndos() {
            UndoDrawOpEntry[] items = DrawOps.Items;
            for (int i = 0; i < items.Length; i++) {
                if (items[i].End < UndoBuffer.LastClear)
                    DrawOps.Remove(items[i]);
            }
        }
        
        internal static void AddNote(string target, Player who, string type) {
             if (!Server.LogNotes) return;
             string src = who == null ? "(console)" : who.name;
             
             string time = DateTime.UtcNow.ToString("dd/MM/yyyy");
             Server.Notes.Append(target + " " + type + " " + src + " " + time);
        }
        
        internal static void AddNote(string target, Player who, string type, string reason) {
             if (!Server.LogNotes) return;
             string src = who == null ? "(console)" : who.name;
             
             string time = DateTime.UtcNow.ToString("dd/MM/yyyy");
             reason = reason.Replace(" ", "%20");
             Server.Notes.Append(target + " " + type + " " + src + " " + time + " " + reason);
        }
        
        readonly object selLock = new object();
        Vec3S32[] selMarks;
        object selState;
        SelectionHandler selCallback;
        int selIndex;

        public void MakeSelection(int marks, object state, SelectionHandler callback) {
            lock (selLock) {
                selMarks = new Vec3S32[marks];
                selState = state;
                selCallback = callback;
                selIndex = 0;
                Blockchange = SelectionBlockChange;
            }
        }
        
        void SelectionBlockChange(Player p, ushort x, ushort y, ushort z, byte block, byte extBlock) {
            lock (selLock) {
                Blockchange = SelectionBlockChange;
                RevertBlock(x, y, z);
                
                selMarks[selIndex] = new Vec3S32(x, y, z);
                selIndex++;
                if (selIndex != selMarks.Length) return;
                
                Blockchange = null;
                block = block < Block.CpeCount ? p.bindings[block] : block;
                bool canRepeat = selCallback(this, selMarks, selState, block, extBlock);
                if (canRepeat && staticCommands)
                    MakeSelection(selIndex, selState, selCallback);
            }
        }
        
        
        public void CheckForMessageSpam() {
            spamChecker.CheckChatSpam();
        }        
    }
}
