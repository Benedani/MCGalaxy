﻿/*
    Copyright 2015 MCGalaxy
        
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
using MCGalaxy.Blocks.Physics;
using MCGalaxy.DB;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Undo;

namespace MCGalaxy.Drawing.Ops {

    public class UndoSelfDrawOp : UndoOnlineDrawOp {
        
        public UndoSelfDrawOp() {
            Flags = BlockDBFlags.UndoSelf;
        }
        
        public override string Name { get { return "UndoSelf"; } }
    }
    
    public class UndoOnlineDrawOp : DrawOp {
        public override string Name { get { return "UndoOnline"; } }
        public override bool AffectedByTransform { get { return false; } }
        
        /// <summary> Point in time that the /undo should go backwards up to. </summary>
        public DateTime Start = DateTime.MinValue;
        
        /// <summary> Point in time that the /undo should start updating blocks. </summary>
        public DateTime End = DateTime.MaxValue;
        
        internal Player who;
        
        public UndoOnlineDrawOp() {
            Flags = BlockDBFlags.UndoOther;
        }
        
        public override long BlocksAffected(Level lvl, Vec3S32[] marks) { return -1; }
        
        public override void Perform(Vec3S32[] marks, Brush brush, Action<DrawOpBlock> output) {
            UndoCache cache = who.UndoBuffer;
            using (IDisposable locker = cache.ClearLock.AccquireReadLock()) {
                if (UndoBlocks(Player, who, output)) return;
            }
            bool found = false;
            string target = who.name.ToLower();
            
            UndoFormatArgs args = new UndoFormatArgs(Player, Start, End, output);
            if (Min.X != ushort.MaxValue) {
                UndoFormat.DoUndoArea(target, Min, Max, ref found, args);
            } else {
                UndoFormat.DoUndo(target, ref found, args);
            }
        }
        
        bool UndoBlocks(Player p, Player who, Action<DrawOpBlock> output) {
            UndoFormatArgs args = new UndoFormatArgs(p, Start, End, output);
            UndoFormat format = new UndoFormatOnline(who.UndoBuffer);
            
            if (Min.X != ushort.MaxValue) {
                UndoFormat.DoUndoArea(null, Min, Max, format, args);
            } else {
                UndoFormat.DoUndo(null, format, args);
            }
            return args.Stop;
        }
    }

    public class UndoOfflineDrawOp : DrawOp {
        public override string Name { get { return "UndoOffline"; } }
        public override bool AffectedByTransform { get { return false; } }
        
        /// <summary> Point in time that the /undo should go backwards up to. </summary>
        public DateTime Start = DateTime.MinValue;
        
        internal string whoName;
        internal bool found = false;
        
        public UndoOfflineDrawOp() {
            Flags = BlockDBFlags.UndoOther;
        }
        
        public override long BlocksAffected(Level lvl, Vec3S32[] marks) { return -1; }
        
        public override void Perform(Vec3S32[] marks, Brush brush, Action<DrawOpBlock> output) {
            string target = whoName.ToLower();
            UndoFormatArgs args = new UndoFormatArgs(Player, Start, DateTime.MaxValue, output);
            
            if (Min.X != ushort.MaxValue)
                UndoFormat.DoUndoArea(target, Min, Max, ref found, args);
            else
                UndoFormat.DoUndo(target, ref found, args);
        }
    }

    public class UndoPhysicsDrawOp : DrawOp {
        public override string Name { get { return "UndoPhysics"; } }
        public override bool AffectedByTransform { get { return false; } }
        
        internal DateTime Start;
        
        public override long BlocksAffected(Level lvl, Vec3S32[] marks) { return -1; }
        
        public override void Perform(Vec3S32[] marks, Brush brush, Action<DrawOpBlock> output) {
            if (Level.UndoBuffer.Count != Server.physUndo) {
                int count = Level.currentUndo;
                for (int i = count; i >= 0; i--) {
                    try {
                        if (!CheckBlockPhysics(Player, Level, i)) break;
                    } catch { }
                }
            } else {
                int count = Level.currentUndo;
                for (int i = count; i >= 0; i--) {
                    try {
                        if (!CheckBlockPhysics(Player, Level, i)) break;
                    } catch { }
                }
                for (int i = Level.UndoBuffer.Count - 1; i > count; i--) {
                    try {
                        if (!CheckBlockPhysics(Player, Level, i)) break;
                    } catch { }
                }
            }
        }
        
        bool CheckBlockPhysics(Player p, Level lvl, int i) {
            Level.UndoPos undo = lvl.UndoBuffer[i];
            byte b = lvl.GetTile(undo.index);
            DateTime time = Server.StartTime.AddTicks((undo.flags >> 2) * TimeSpan.TicksPerSecond);
            if (time < Start) return false;
            
            byte newType = (undo.flags & 2) != 0 ? Block.custom_block : undo.newRawType;
            if (b == newType || Block.Convert(b) == Block.water || Block.Convert(b) == Block.lava) {
                ushort x, y, z;
                lvl.IntToPos(undo.index, out x, out y, out z);
                int undoIndex = lvl.currentUndo;
                lvl.currentUndo = i;
                lvl.currentUndo = undoIndex;
                byte oldType = (undo.flags & 1) != 0 ? Block.custom_block : undo.oldRawType;
                byte oldExtType = (undo.flags & 1) != 0 ? undo.oldRawType : (byte)0;
                lvl.Blockchange(x, y, z, oldType, true, default(PhysicsArgs), oldExtType, false);
            }
            return true;
        }
    }
}
