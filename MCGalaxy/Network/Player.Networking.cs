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
using System.Net.Sockets;
using System.Text;

namespace MCGalaxy {
    public sealed partial class Player : IDisposable {

        public NetworkStream Stream;

        static void Receive(IAsyncResult result) {
            //Server.s.Log(result.AsyncState.ToString());
            Player p = (Player)result.AsyncState;
            if (p.disconnected || p.socket == null) return;
            
            try {
                int length = p.socket.EndReceive(result);
                if (length == 0) { p.Disconnect(); return; }

                byte[] allData = new byte[p.leftBuffer.Length + length];
                Buffer.BlockCopy(p.leftBuffer, 0, allData, 0, p.leftBuffer.Length);
                Buffer.BlockCopy(p.tempbuffer, 0, allData, p.leftBuffer.Length, length);
                p.leftBuffer = p.ProcessReceived(allData);
                
                if (p.dontmindme && p.leftBuffer.Length == 0) {
                    Server.s.Log("Disconnected");
                    p.socket.Close();
                    p.disconnected = true;
                    return;
                }
                if ( !p.disconnected )
                    p.socket.BeginReceive(p.tempbuffer, 0, p.tempbuffer.Length, SocketFlags.None,
                                          new AsyncCallback(Receive), p);
            } catch ( SocketException ) {
                p.Disconnect();
            }  catch ( ObjectDisposedException ) {
                // Player is no longer connected, socket was closed
                // Mark this as disconnected and remove them from active connection list
                Player.SaveUndo(p);
                connections.Remove(p);
                p.RemoveFromPending();
                p.disconnected = true;
            } catch ( Exception e ) {
                Server.ErrorLog(e);
                p.Leave("Error!");
            }
        }
        
        public bool hasCpe, finishedCpeLogin = false;
        public string appName;
        public int extensionCount;
        public List<string> extensions = new List<string>();
        public int customBlockSupportLevel;
        
        void HandleExtInfo(byte[] packet) {
            appName = NetUtils.ReadString(packet, 1);
            extensionCount = packet[66];          
            CheckReadAllExtensions(); // in case client supports 0 CPE packets
        }

        void HandleExtEntry(byte[] packet) {
            AddExtension(NetUtils.ReadString(packet, 1), NetUtils.ReadI32(packet, 65));
            extensionCount--;
            CheckReadAllExtensions();
        }

        void HandlePlayerClicked(byte[] packet) {
            if (OnPlayerClick == null) return;

            var Button = (MouseButton)packet[1];
            var Action = (MouseAction)packet[2];
            ushort Yaw = NetUtils.ReadU16(packet, 3);
            ushort Pitch = NetUtils.ReadU16(packet, 5);
            byte EntityID = packet[7];
            ushort X = NetUtils.ReadU16(packet, 8);
            ushort Y = NetUtils.ReadU16(packet, 10);
            ushort Z = NetUtils.ReadU16(packet, 12);
            byte Face = packet[14];

            var face = TargetBlockFace.None;
            if (Face < (byte)face)
                face = (TargetBlockFace)Face;
            OnPlayerClick(this, Button, Action, Yaw, Pitch, EntityID, X, Y, Z, face);
            OnPlayerClickEvent.Call(this, Button, Action, Yaw, Pitch, EntityID, X, Y, Z, face);
        }

        void CheckReadAllExtensions() {
            if (extensionCount <= 0 && !finishedCpeLogin) {
                CompleteLoginProcess();
                finishedCpeLogin = true;
            }
        }


        public void SendRaw(int id) {
            byte[] buffer = new [] { (byte)id };
            Send(buffer);
        }
        
        public void SendRaw(int id, byte data) {
            byte[] buffer = new [] { (byte)id, data };
            Send(buffer);
        }
        
        [Obsolete("Include the opcode in the array to avoid an extra temp allocation.")]
        public void SendRaw(int id, byte[] send, bool sync = false) {
            byte[] buffer = new byte[send.Length + 1];
            buffer[0] = (byte)id;
            for ( int i = 0; i < send.Length; i++ )
                buffer[i + 1] = send[i];
            SendRaw(buffer, sync);
            buffer = null;
        }
        
        [Obsolete("Use Send() instead.")]
        public void SendRaw(byte[] buffer, bool sync = false) { Send(buffer, sync); }
        
        public void Send(byte[] buffer, bool sync = false) {
            // Abort if socket has been closed
            if (disconnected || socket == null || !socket.Connected) return;
            
            try {
                if (sync)
                    socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                else
                    socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, delegate(IAsyncResult result) { }, null);
                buffer = null;
            } catch (SocketException e) {
                buffer = null;
                Disconnect();
                #if DEBUG
                Server.ErrorLog(e);
                #endif
            } catch (ObjectDisposedException) {
                // socket was already closed by another thread.
                buffer = null;
            }
        }
        
        /// <summary> Sends a message to the target player, unless the 
        /// target player is ignoring this player. </summary>
        /// <returns> Whether the target player is ignoring this player. </returns>
        public bool MessageTo(Player other, string message) {
            if (other.ignoreAll || other.listignored.CaselessContains(name)) return false;
            other.SendMessage(message);
            return true;
        }
        
        public void SendBlankMessage() {
            byte[] buffer = new byte[66];
            buffer[0] = Opcode.Message;
            
            for (int i = 2; i < buffer.Length; i++)
                buffer[i] = (byte)' ';
            Send(buffer);
        }
        
        public static void MessageLines(Player p, IEnumerable<string> lines) {
            foreach (string line in lines)
                SendMessage(p, line, true);
        }
        
        public static void Message(Player p, string message) {
            SendMessage(p, message, true);
        }
        
        public static void Message(Player p, string message, object a0) {
            SendMessage(p, String.Format(message, a0), true);
        }
        
        public static void Message(Player p, string message, object a0, object a1) {
            SendMessage(p, String.Format(message, a0, a1), true);
        }
        
        public static void Message(Player p, string message, object a0, object a1, object a2) {
            SendMessage(p, String.Format(message, a0, a1, a2), true);
        }
        
        public static void Message(Player p, string message, params object[] args) {
            SendMessage(p, String.Format(message, args), true);
        }

        public static void SendMessage(Player p, string message) {
            SendMessage(p, message, true);
        }
        
        public static void SendMessage(Player p, string message, bool colorParse) {
            if (p == null) {
                if (storeHelp)
                    storedHelp += message + "\r\n";
                else
                    Server.s.Log(message);
            } else if (p.ircNick != null) {
                if (p.ircNick == "#@public@#")
                    Server.IRC.Say(message, false, true);
                else if (p.ircNick == "#@private@#")
                    Server.IRC.Say(message, true, true);
                else
                    Server.IRC.Pm(p.ircNick, message);
            } else {
                p.SendMessage(0, Server.DefaultColor + message, colorParse);
            }
        }
        
        public void SendMessage(string message) {
            SendMessage(0, Server.DefaultColor + message, true);
        }
        
        public void SendMessage(string message, bool colorParse) {
            SendMessage(0, Server.DefaultColor + message, colorParse);
        }
        
        public void SendMessage(byte id, string message, bool colorParse = true) {
            message = Chat.Format(message, this, colorParse);
            int totalTries = 0;
            if (MessageRecieve != null)
                MessageRecieve(this, message);
            if (OnMessageRecieve != null)
                OnMessageRecieve(this, message);
            OnMessageRecieveEvent.Call(this, message);
            if (cancelmessage) { cancelmessage = false; return; }
            
            retryTag: try {
                foreach (string raw in LineWrapper.Wordwrap(message)) {
                    string line = raw;
                    if (!HasCpeExt(CpeExt.EmoteFix) && line.TrimEnd(' ')[line.TrimEnd(' ').Length - 1] < '!')
                        line += '\'';
                    
                    byte[] buffer = new byte[66];
                    buffer[0] = Opcode.Message;
                    buffer[1] = (byte)id;
                    NetUtils.Write(line, buffer, 2, HasCpeExt(CpeExt.FullCP437));
                    Send(buffer);
                }
            } catch ( Exception e ) {
                message = "&f" + message;
                totalTries++;
                if ( totalTries < 10 ) goto retryTag;
                else Server.ErrorLog(e);
            }
        }
        
        public void SendCpeMessage(CpeMessageType id, string message, bool colorParse = true) {
            if (id != CpeMessageType.Normal && !HasCpeExt(CpeExt.MessageTypes)) {
                if (id == CpeMessageType.Announcement) id = CpeMessageType.Normal;
                else return;
            }
            message = Chat.Format(message, this, colorParse);
            SendRawMessage(id, message);
        }
        
        /// <summary> Sends a raw message without performing any token resolving, emoticon parsing, or color parsing. </summary>
        public void SendRawMessage(CpeMessageType id, string message) {
            byte[] buffer = new byte[66];
            buffer[0] = Opcode.Message;
            buffer[1] = (byte)id;
            NetUtils.Write(message, buffer, 2, HasCpeExt(CpeExt.FullCP437));
            Send(buffer);
        }
        
        public void SendMotd() { SendMapMotd(); }
        public void SendUserMOTD() { SendMapMotd(); }

        void SendMapMotd() {
            byte[] packet = Packet.Motd(this, level.GetMotd(this));
            if (OnSendMOTD != null) OnSendMOTD(this, packet);
            Send(packet);
            
            if (!HasCpeExt(CpeExt.HackControl)) return;
            Send(Hacks.MakeHackControl(this));
            if (Game.Referee)
                Send(Packet.HackControl(true, true, true, true, true, -1));
        }
        
        public void SendMap(Level oldLevel) { SendRawMap(oldLevel, level); }
        
        readonly object joinLock = new object();
        public bool SendRawMap(Level oldLevel, Level level) {
            lock (joinLock)
                return SendRawMapCore(oldLevel, level);
        }
        
        bool SendRawMapCore(Level oldLevel, Level level) {
            if (level.blocks == null) return false;
            bool success = true;
            useCheckpointSpawn = false;
            lastCheckpointIndex = -1;
            
            LevelAccess access = level.BuildAccess.Check(this);
            AllowBuild = access == LevelAccess.Whitelisted || access == LevelAccess.Allowed;
            
            try {               
                if (hasBlockDefs) {
                    if (oldLevel != null && oldLevel != level)
                        RemoveOldLevelCustomBlocks(oldLevel);
                    BlockDefinition.SendLevelCustomBlocks(this);
                }
                
                SendRaw(Opcode.LevelInitialise);
                using (LevelChunkStream s = new LevelChunkStream(this))
                    LevelChunkStream.CompressMap(this, s);
                
                byte[] buffer = new byte[7];
                buffer[0] = Opcode.LevelFinalise;
                NetUtils.WriteI16((short)level.Width, buffer, 1);
                NetUtils.WriteI16((short)level.Height, buffer, 3);
                NetUtils.WriteI16((short)level.Length, buffer, 5);
                Send(buffer);
                AFKCooldown = DateTime.UtcNow.AddSeconds(2);
                Loading = false;
                
                if (HasCpeExt(CpeExt.EnvWeatherType))
                	Send(Packet.EnvWeatherType((byte)level.Weather));
                if (HasCpeExt(CpeExt.EnvColors))
                    SendCurrentEnvColors();
                SendCurrentMapAppearance();
                if (HasCpeExt(CpeExt.BlockPermissions))
                    SendCurrentBlockPermissions();
                
                if (OnSendMap != null)
                    OnSendMap(this, buffer);
                if (!level.guns && aiming) {
                    aiming = false;
                    ClearBlockchange();
                }
            } catch (Exception ex) {
                success = false;
                PlayerActions.ChangeMap(this, Server.mainLevel);
                SendMessage("There was an error sending the map data, you have been sent to the main level.");
                Server.ErrorLog(ex);
            } finally {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return success;
        }
        
        void RemoveOldLevelCustomBlocks(Level oldLevel) {
            BlockDefinition[] defs = oldLevel.CustomBlockDefs;
            for (int i = 1; i < 256; i++) {
                BlockDefinition def = defs[i];
                if (def == null || def == BlockDefinition.GlobalDefs[i]) continue;
                SendRaw(Opcode.CpeRemoveBlockDefinition, (byte)i);
            }
        }
        
        /// <summary> Sends a packet indicating an entity was spawned in the current map
        /// at the given absolute position + coordinates </summary>
        public void SendSpawn(byte id, string name, ushort x, ushort y, ushort z, byte rotx, byte roty) {       	
            // NOTE: Fix for standard clients
            if (id == Entities.SelfID) y -= 22;
            
            byte[] buffer = new byte[74];
            buffer[0] = Opcode.AddEntity;
            buffer[1] = id;
            NetUtils.WriteAscii(name.TrimEnd('+'), buffer, 2);
            NetUtils.WriteU16(x, buffer, 66);
            NetUtils.WriteU16(y, buffer, 68);
            NetUtils.WriteU16(z, buffer, 70);
            buffer[72] = rotx;
            buffer[73] = roty;
            Send(buffer);

            if (hasChangeModel) UpdateModels();
        }
        
        /// <summary> Sends a packet indicating an absolute position + orientation change for an enity. </summary>
        public void SendPos(byte id, ushort x, ushort y, ushort z, byte rotx, byte roty) {
            pos = new ushort[3] { x, y, z };
            rot = new byte[2] { rotx, roty };

            // NOTE: Fix for standard clients
            if (id == Entities.SelfID) y -= 22;
            
            byte[] buffer = new byte[10];
            buffer[0] = Opcode.EntityTeleport;
            buffer[1] = id;
            NetUtils.WriteU16(x, buffer, 2);
            NetUtils.WriteU16(y, buffer, 4);
            NetUtils.WriteU16(z, buffer, 6);
            buffer[8] = rotx;
            buffer[9] = roty;
            Send(buffer);
        }
        
         /// <summary> Sends a packet indicating an absolute position + orientation change for the player. </summary>
        /// <remarks>This method treats Y as head Y, and adjusts for client increasing Y by 22/32 blocks. </remarks>
        public void SendOwnFeetPos(ushort x, ushort y, ushort z, byte rotx, byte roty) {
            SendPos(Entities.SelfID, x, (ushort)(y + 51), z, rotx, roty);
        }

        /// <summary> Sends a packet indicating an entity was removed from the current map. </summary>
        public void SendDespawn(byte id) {
            SendRaw(Opcode.RemoveEntity, id);
        }
        
        public void SendBlockchange(ushort x, ushort y, ushort z, byte block) {
            //if (x < 0 || y < 0 || z < 0) return;
            if (x >= level.Width || y >= level.Height || z >= level.Length) return;

            byte[] buffer = new byte[8];
            buffer[0] = Opcode.SetBlock;
            NetUtils.WriteU16(x, buffer, 1);
            NetUtils.WriteU16(y, buffer, 3);
            NetUtils.WriteU16(z, buffer, 5);
            
            if (block == Block.custom_block) {
                buffer[7] = hasBlockDefs ? level.GetExtTile(x, y, z) 
                    : level.GetFallbackExtTile(x, y, z);
                if (!hasCustomBlocks) buffer[7] = Block.ConvertCPE(buffer[7]);
            } else if (hasCustomBlocks) {
                buffer[7] = Block.Convert(block);
            } else {
                buffer[7] = Block.ConvertCPE(Block.Convert(block));
            }
            Send(buffer);
        }
        
        // Duplicated as this packet needs to have maximum optimisation.
        public void SendBlockchange(ushort x, ushort y, ushort z, byte block, byte extBlock) {
            //if (x < 0 || y < 0 || z < 0) return;
            if (x >= level.Width || y >= level.Height || z >= level.Length) return;

            byte[] buffer = new byte[8];
            buffer[0] = Opcode.SetBlock;
            NetUtils.WriteU16(x, buffer, 1);
            NetUtils.WriteU16(y, buffer, 3);
            NetUtils.WriteU16(z, buffer, 5);
            
            if (block == Block.custom_block) {
                buffer[7] = hasBlockDefs ? extBlock : level.GetFallback(extBlock);
                if (!hasCustomBlocks) buffer[7] = Block.ConvertCPE(buffer[7]);
            } else if (hasCustomBlocks) {
                buffer[7] = Block.Convert(block);
            } else {
                buffer[7] = Block.Convert(Block.ConvertCPE(block));
            }
            Send(buffer);
        }

        public void SendExtAddEntity(byte id, string name, string displayname = "") {
            byte[] buffer = new byte[130];
            buffer[0] = Opcode.CpeExtAddEntity;
            buffer[1] = id;
            NetUtils.WriteAscii(name, buffer, 2);
            if (displayname == "") displayname = name;
            NetUtils.WriteAscii(displayname, buffer, 66);
            Send(buffer);
        }
        
        public void SendExtAddEntity2(byte id, string skinName, string displayName, ushort x, ushort y, ushort z, byte rotx, byte roty) {
            // NOTE: Fix for standard clients
            if (id == Entities.SelfID) y -= 22;
            
            byte[] buffer = new byte[138];
            buffer[0] = Opcode.CpeExtAddEntity2;
            buffer[1] = id;
            NetUtils.WriteAscii(displayName.TrimEnd('+'), buffer, 2);
            NetUtils.WriteAscii(skinName.TrimEnd('+'), buffer, 66);
            NetUtils.WriteU16(x, buffer, 130);
            NetUtils.WriteU16(y, buffer, 132);
            NetUtils.WriteU16(z, buffer, 134);
            buffer[136] = rotx;
            buffer[137] = roty;
            Send(buffer);

            if (hasChangeModel) UpdateModels();
        }
        
        public void SendExtAddPlayerName(byte id, string listName, string displayName, string grp, byte grpRank) {
            byte[] buffer = new byte[196];
            buffer[0] = Opcode.CpeExtAddPlayerName;
            NetUtils.WriteI16(id, buffer, 1);
            NetUtils.WriteAscii(listName, buffer, 3);
            NetUtils.WriteAscii(displayName, buffer, 67);
            NetUtils.WriteAscii(grp, buffer, 131);
            buffer[195] = grpRank;
            Send(buffer);
        }
        
        public void SendExtRemovePlayerName(byte id) {
            byte[] buffer = new byte[3];
            buffer[0] = Opcode.CpeExtRemovePlayerName;
            NetUtils.WriteI16(id, buffer, 1);
            Send(buffer);
        }
        
        public void SendChangeModel( byte id, string model ) {
            // Fallback block models for clients that don't support block definitions
            byte block;
            bool fallback = byte.TryParse(model, out block) && block >= Block.CpeCount;
            block = level == null ? block : level.GetFallback(block);
            if (fallback && !hasBlockDefs && block != Block.air)
                model = block.ToString();
                
            byte[] buffer = new byte[66];
            buffer[0] = Opcode.CpeChangeModel;
            buffer[1] = id;
            NetUtils.WriteAscii(model, buffer, 2);
            Send(buffer);
        }

        internal void CloseSocket() {
            // Try to close the socket.
            // Sometimes its already closed so these lines will cause an error
            // We just trap them and hide them from view :P
            try {
                // Close the damn socket connection!
                socket.Shutdown(SocketShutdown.Both);
                #if DEBUG
                Server.s.Log("Socket was shutdown for " + name ?? ip);
                #endif
            }
            catch ( Exception e ) {
                #if DEBUG
                Exception ex = new Exception("Failed to shutdown socket for " + name ?? ip, e);
                Server.ErrorLog(ex);
                #endif
            }

            try {
                socket.Close();
                #if DEBUG
                Server.s.Log("Socket was closed for " + name ?? ip);
                #endif
            }
            catch ( Exception e ) {
                #if DEBUG
                Exception ex = new Exception("Failed to close socket for " + name ?? ip, e);
                Server.ErrorLog(ex);
                #endif
            }
            RemoveFromPending();
        }
    }
}
