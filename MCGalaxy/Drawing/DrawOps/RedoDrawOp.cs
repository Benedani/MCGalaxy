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
using MCGalaxy.DB;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Undo;

namespace MCGalaxy.Drawing.Ops {
    
    public class RedoSelfDrawOp : DrawOp {
        public override string Name { get { return "RedoSelf"; } }
        public override bool AffectedByTransform { get { return false; } }
        
        /// <summary> Point in time that the /undo should go backwards up to. </summary>
        public DateTime Start = DateTime.MinValue;
        
        /// <summary> Point in time that the /undo should start updating blocks. </summary>
        public DateTime End = DateTime.MaxValue;
        
        public RedoSelfDrawOp() {
            Flags = BlockDBFlags.RedoSelf;
        }
        
        public override long BlocksAffected(Level lvl, Vec3S32[] marks) { return -1; }
        
        public override void Perform(Vec3S32[] marks, Brush brush, Action<DrawOpBlock> output) {
            UndoCache cache = Player.UndoBuffer;
            using (IDisposable locker = cache.ClearLock.AccquireReadLock()) {
                if (RedoBlocks(Player, output)) return;
            }
            
            bool found = false;
            UndoFormatArgs args = new UndoFormatArgs(Player, Start, End, output);
            UndoFormat.DoRedo(Player.name.ToLower(), ref found, args);
        }
        
        bool RedoBlocks(Player p, Action<DrawOpBlock> output) {
            UndoFormatArgs args = new UndoFormatArgs(p, Start, End, output);
            UndoFormat format = new UndoFormatOnline(p.UndoBuffer);
            UndoFormat.DoRedo(null, format, args);
            return args.Stop;
        }
    }
}
