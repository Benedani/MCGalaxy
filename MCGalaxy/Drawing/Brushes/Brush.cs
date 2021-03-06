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
using MCGalaxy.Drawing.Ops;

namespace MCGalaxy.Drawing.Brushes {
    public abstract class Brush {
        
        /// <summary> Human friendly name of this brush. </summary>
        public abstract string Name { get; }
        
        /// <summary> Performs calcuations (if necessary) for the given drawop. </summary>
        public virtual void Configure(DrawOp op, Player p) { }
        
        /// <summary> Returns the next block that should be placed in the world, 
        /// based on the draw operation's current state. </summary>
        public abstract byte NextBlock(DrawOp op);

        /// <summary> Returns the next custom block that should be placed in the world, 
        /// based on the draw operation's current state. </summary>        
        public abstract byte NextExtBlock(DrawOp op);
    }
}
