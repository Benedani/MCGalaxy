/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the    Educational Community License, Version 2.0 and
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

namespace MCGalaxy.Commands {
    
    public sealed class CmdGlobalBlock : Command {
        
        public override string name { get { return "globalblock"; } }
        public override string shortcut { get { return "gb"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public CmdGlobalBlock() { }
        static char[] trimChars = {' '};

        public override void Use(Player p, string message) {
            string[] parts = message.Split(trimChars, 4);
            for (int i = 0; i < Math.Min(parts.Length, 3); i++)
                parts[i] = parts[i].ToLower();
            
            if (message == "") {
                if (GetBD(p) != null)
                    SendStepHelp(p, GetStep(p));
                else
                    Help(p);
                return;
            }
            
            switch (parts[0]) {
                case "add":
                case "create":
                    AddHandler(p, parts); break;
                case "delete":
                case "remove":
                    RemoveHandler(p, parts); break;
                case "list":
                case "ids":
                    ListHandler(p, parts); break;
                case "abort":
                    Player.SendMessage(p, "Aborted the custom block creation process.");
                    SetBD(p, null); break;
                case "edit":
                    EditHandler(p, parts); break;                   
                default:
                    if (GetBD(p) != null)
                        DefineBlockStep(p, message);
                    else
                        Help(p);
                    break;
            }
        }
        
        int targetId;
        void AddHandler(Player p, string[] parts) {
            if (parts.Length >= 2 ) {
                string id = parts[1];
                if (!CheckBlockId(p, id, out targetId)) return;
                BlockDefinition def = BlockDefinition.GlobalDefinitions[targetId];
                
                if (def != null) {
                    Player.SendMessage(p, "There is already a custom block with the id " + id +
                                       ", you must either use a different id or use \"/gb remove " + id + "\"");
                    return;
                }
            } else {
                targetId = GetFreeId();
                if (targetId == Block.Zero) {
                    Player.SendMessage(p, "There are no custom block ids left, " +
                                       "you must /gb remove a custom block first.");
                    return;
                }
            }
            
            SetBD(p, new BlockDefinition());
            GetBD(p).BlockID = (byte)targetId;
            Player.SendMessage(p, "Type '/gb abort' at anytime to abort the creation process.");
            Player.SendMessage(p, "Type '/gb revert' to go back a step in the creation process.");
            Player.SendMessage(p, "Use '/gb <arg>' to enter arguments for the creation process.");
            Player.SendMessage(p, "%f----------------------------------------------------------");
            SetStep(p, 2);
            SendStepHelp(p, GetStep(p));
        }
        
        byte GetFreeId() {
            for (int i = 70; i < 255; i++) {
                if (BlockDefinition.GlobalDefinitions[i] == null)
                    return (byte)i;
            }
            return Block.Zero;
        }
        
        void ListHandler(Player p, string[] parts) {
            int offset = 0, index = 0, count = 0;
            if (parts.Length > 1) int.TryParse(parts[1], out offset);
            BlockDefinition[] defs = BlockDefinition.GlobalDefinitions;
            
            for( int i = 1; i < 256; i++ ) {
                BlockDefinition def = defs[i];
                if (def == null) continue;
                
                if (index >= offset) {
                    count++;
                    const string format = "Custom block %T{0} %Shas name %T{1}";
                    Player.SendMessage(p, String.Format(format, def.BlockID, def.Name));
                    
                    if (count >= 8) {
                        const string helpFormat = "To see the next set of custom blocks, type %T/gb list {0}";
                        Player.SendMessage(p, String.Format(helpFormat, offset + 8));
                        return;
                    }
                }
                index++;
            }
        }
        
        void RemoveHandler(Player p, string[] parts) {
            if (parts.Length <= 1) { Help(p); return; }
            int blockID;
            if (!CheckBlockId(p, parts[1], out blockID)) return;
            
            BlockDefinition def = BlockDefinition.GlobalDefinitions[blockID];
            if (def == null) {
                Player.SendMessage(p, "There is no globally defined custom block with that block id.");
                Player.SendMessage(p, "Use \"%T/gb list\" %Sto see a list of global custom blocks.");
                return;
            }
            
            BlockDefinition.RemoveGlobal(def);
            BlockDefinition.SaveGlobal("blocks.json");
        }
        
        void DefineBlockStep(Player p, string value) {
            string opt = value.ToLower();
            int step = GetStep(p);
            if (opt == "revert" && step > 2) {
                step--;
                SendStepHelp(p, step); 
                SetStep(p, step); return;
            }
            BlockDefinition bd = GetBD(p);
            
            if (step == 2) {
                bd.Name = value;
                step++;
            } else if (step == 3) {
                if (value == "0" || value == "1" || value == "2") {
                    bd.CollideType = byte.Parse(value);
                    step++;
                }
            } else if (step == 4) {
                if (float.TryParse(value, out bd.Speed) && bd.Speed >= 0.25f && bd.Speed <= 3.96f)
                    step++;
            } else if (step == 5) {
                if (byte.TryParse(value, out bd.TopTex))
                    step++;
            } else if (step == 6) {
                if (byte.TryParse(value, out bd.SideTex))
                    step++;
            } else if (step == 7) {
                if (byte.TryParse(value, out bd.BottomTex))
                    step++;
            } else if (step == 8) {
                if (value == "0" || value == "1") {
                    bd.BlocksLight = value == "0";
                    step++;
                }
            } else if (step == 9) {
                bool result = byte.TryParse(value, out bd.WalkSound);
                if (result && bd.WalkSound <= 11)
                    step++;
            } else if (step == 10) {
                if (value == "0" || value == "1") {
                    bd.FullBright = value != "0";
                    step++;
                }
            } else if (step == 11) {
                bool result = byte.TryParse(value, out bd.BlockDraw);
                if (result && bd.BlockDraw >= 0 && bd.BlockDraw <= 3)
                    step++;
            } else if (step == 12) {
                if (value == "0" || value == "1") {
                    bd.Shape = value == "0" ? (byte)0 : (byte)16;
                    step = bd.Shape == 0 ? 15 : 13;
                }
            } else if (step == 13) {
                if (ParseCoords(value, out bd.MinX, out bd.MinY, out bd.MinZ))
                    step++;
            } else if (step == 14) {
                if (ParseCoords(value, out bd.MaxX, out bd.MaxY, out bd.MaxZ))
                    step++;
                bd.Shape = bd.MaxY;
            } else if (step == 15) {
                if (byte.TryParse(value, out bd.FogDensity))
                    step = bd.FogDensity == 0 ? 19 : 16;
            } else if (step == 16) {
                if (byte.TryParse(value, out bd.FogR))
                    step++;
            } else if (step == 17) {
                if (byte.TryParse(value, out bd.FogG))
                    step++;
            } else if (step == 18) {
                if (byte.TryParse(value, out bd.FogB))
                    step++;
            } else if (step == 19) {
                if (Block.Byte(value) == Block.Zero) {
                    SendStepHelp(p, step); return;
                }
                bd.FallBack = Block.Byte(value);
                
                // in case the list is modified before we finish the command.
                if (BlockDefinition.GlobalDefinitions[bd.BlockID] != null) {
                    bd.BlockID = GetFreeId();
                    if (bd.BlockID == Block.Zero) {
                        Player.SendMessage(p, "There are no custom block ids left, " +
                                           "you must /gb remove a custom block first.");
                        return;
                    }
                }
                
                Player.SendMessage(p, "Created a new custom block " + bd.Name + "(" + bd.BlockID + ")");
                BlockDefinition.AddGlobal(bd);
                SetBD(p, null);
                SetStep(p, 0);
                return;
            }
            SendStepHelp(p, step);
            SetStep(p, step);
        }
        
        bool EditByte(Player p, string arg, string propName, ref byte target) {
            return EditByte(p, arg, propName, ref target, -1, 0, 0, 255);
        }
        
        bool EditByte(Player p, string value, string propName, ref byte target,
                      int step, int offset, byte min, byte max) {
            int temp = 0;
            if (!int.TryParse(value, out temp) || temp < min || temp > max) {
                Player.SendMessage(p, propName + " must be an integer between " + min + " and " + max + ".");
                if (step != -1) SendEditHelp(p, step, offset);
                return false;
            }
            target = (byte)temp;
            return true;
        }
        
        void EditHandler(Player p, string[] parts) {
            if (parts.Length <= 3) {
                if(parts.Length == 1)
                    Player.SendMessage(p, "Valid properties: name, collide, speed, toptex, sidetex, " +
                                       "bottomtex, blockslight, sound, fullbright, shape, blockdraw, min, max, " +
                                       "fogdensity, fogred, foggreen, fogblue, fallback");
                else
                    Help(p);
                return;
            }
            int blockId;
            if (!CheckBlockId(p, parts[1], out blockId));
            
            BlockDefinition def = BlockDefinition.GlobalDefinitions[blockId];
            if (def == null) {
                Player.SendMessage(p, "There is no globally defined custom block with that block id.");
                Player.SendMessage(p, "Use \"%T/gb list\" %Sto see a list of global custom blocks.");
                return;
            }
            string value = parts[3];
            float fTemp;
            byte tempX, tempY, tempZ;
            
            switch (parts[2].ToLower()) {
                case "name":
                    def.Name = value; break;
                case "collide":
                    if( !(value == "0" || value == "1" || value == "2")) {
                        SendEditHelp(p, 3, 0); return;
                    }
                    def.CollideType = byte.Parse(value); break;
                case "speed":
                    if (!float.TryParse(value, out fTemp) || fTemp < 0.25f || fTemp > 3.96f) {
                        SendEditHelp(p, 4, 0); return;
                    }
                    def.Speed = fTemp; break;
                case "top":
                case "toptex":
                    if (!EditByte(p, value, "Top texture", ref def.TopTex)) return;
                    break;
                case "side":
                case "sidetex":
                    if (!EditByte(p, value, "Side texture", ref def.SideTex)) return;
                    break;
                case "bottom":
                case "bottomtex":
                    if (!EditByte(p, value, "Bottom texture", ref def.BottomTex)) return;
                    break;
                case "light":
                case "blockslight":
                    if( !(value == "0" || value == "1")) {
                        SendEditHelp(p, 8, 0); return;
                    }
                    def.BlocksLight = value == "0"; break;
                case "sound":
                case "walksound":
                    if (!EditByte(p, value, "Walk sound", ref def.WalkSound, 9, 1, 0, 11)) return;
                    break;
                case "bright":
                case "fullbright":
                    if( !(value == "0" || value == "1")) {
                        SendEditHelp(p, 10, 0); return;
                    }
                    def.FullBright = value != "0"; break;
                case "shape":
                    if( !(value == "0" || value == "1")) {
                        SendEditHelp(p, 12, 0); return;
                    }
                    def.Shape = value == "0" ? (byte)0 : def.MaxY; break;
                case "draw":
                case "blockdraw":
                    if (!EditByte(p, value, "Block draw", ref def.BlockDraw, 11, 1, 0, 3)) return;
                    break;
                case "min":
                case "mincoords":
                    if (!ParseCoords(value, out tempX, out tempY, out tempZ)) {
                        SendEditHelp(p, 13, 0); return;
                    }
                    def.MinX = tempX; def.MinY = tempY; def.MinZ = tempZ;
                    break;
                case "max":
                case "maxcoords":
                    if (!ParseCoords(value, out tempX, out tempY, out tempZ)) {
                        SendEditHelp(p, 14, 0); return;
                    }
                    def.MaxX = tempX; def.MaxY = tempY; def.MaxZ = tempZ; def.Shape = def.MaxY;
                    break;
                case "density":
                case "fogdensity":
                    if (!EditByte(p, value, "Fog density", ref def.FogDensity)) return;
                    break;
                case "red":
                case "fogred":
                    if (!EditByte(p, value, "Fog red", ref def.FogR)) return;
                    break;
                case "green":
                case "foggreen":
                    if (!EditByte(p, value, "Fog green", ref def.FogG)) return;
                    break;
                case "blue":
                case "fogblue":
                    if (!EditByte(p, value, "Fog blue", ref def.FogB)) return;
                    break;
                case "fallback":
                case "fallbackid":
                case "fallbackblock":
                    tempX = Block.Byte(value);
                    if (tempX == Block.Zero) {
                        Player.SendMessage(p, "'" + value + "' is not a valid standard tile."); return;
                    }
                    def.FallBack = tempX; break;
            }
            
            BlockDefinition.AddGlobal(def);
            foreach (Player pl in Player.players) {
                if (!pl.HasCpeExt(CpeExt.BlockDefinitions)) continue;
                if (pl.level == null || !pl.level.HasCustomBlocks) continue;
                Command.all.Find("reveal").Use(p, pl.name);
            }
        }
        
        static bool ParseCoords(string parts, out byte x, out byte y, out byte z) {
            x = 0; y = 0; z = 0;
            string[] coords = parts.Split(' ');
            if (coords.Length != 3) return false;
            
            if (!byte.TryParse(coords[0], out x) || !byte.TryParse(coords[1], out y) ||
                !byte.TryParse(coords[2], out z)) return false;
            if (x > 16 || y > 16 || z > 16) return false;
            return true;
        }
        
        static string[][] stepsHelp = new string[][] {
            null, // step 0
            null, // step 1
            new[] { "Type the name of this block." },
            new[] { "Type '0' if this block is walk-through.", "Type '1' if this block is swim-through.",
                "Type '2' if this block is solid.",
            },
            new[] { "Type a number between '0.25' (0.25% speed) and '3.96' (3.96% speed).",
                "This speed is used inside or swimming in the block, or when you are walking on it.",
            },
            new[] { "Type a number between '0' and '255' to identify which texture tile to use for the top of the block.",
                "Textures tile numbers are left to right in terrain.png (The file the textures are located).",
            },
            new[] { "Type a number between '0' and '255' to identify which texture tile to use for the sides of the block.",
                "Textures tile numbers are left to right in terrain.png (The file the textures are located).",
            },
            new[] { "Type a number between '0' and '255' to identify which texture tile to use for the bottom of the block.",
                "Textures tile numbers are left to right in terrain.png (The file the textures are located).",
            },
            new[] { "Type '0' if this block blocks light, otherwise '1' if it doesn't" },
            new[] { "Type a number between 0 and 9 to choose the sound heard when walking on it and breaking.",
                "0 = None, 1 = Wood, 2 = Gravel, 3 = Grass, 4 = Stone",
                "5 = Metal, 6 = Glass, 7 = Cloth, 8 = Sand, 9 = Snow",
            },
            new[] { "Type '0' if the block should be darkened when in shadow, '1' if not(e.g lava)." },
            new[] { "Define the block's draw method.", "0 = Opaque, 1 = Transparent (Like glass)",
                "2 = Transparent (Like leaves), 3 = Translucent (Like ice)",
            },
            new[] { "Type '0' if the block is treated as a sprite(e.g roses), '1' if not." },
            new[] { "Enter the three minimum coordinates of the cube in pixels (separated by spaces). There are 16 pixels per block." },
            new[] { "Enter the three maximum coordinates of the cube in pixels (separated by spaces). There are 16 pixels per block." },
            new[] { "Define the block's fog density (The density of it inside, i.e water, lava",
                "0 = No fog at all; 1-255 = Less to greater density",
            },
            new[] { "Define the fog's red value of its RGB (0-255)", },
            new[] { "Define the fog's green value of its RGB (0-255)", },
            new[] { "Define the fog's blue value of its RGB (0-255)", },
            new[] { "Define a fallback for this block (Clients that can't see this block).",
                "You can use the block name or block ID",
            },
        };
        
        static void SendStepHelp(Player p, int step) {
            string[] help = stepsHelp[step];
            for (int i = 0; i < help.Length; i++)
                Player.SendMessage(p, help[i]);
            Player.SendMessage(p, "%f--------------------------");
        }
        
        static void SendEditHelp(Player p, int step, int offset) {
            string[] help = stepsHelp[step];
            for (int i = offset; i < help.Length; i++)
                Player.SendMessage(p, help[i].Replace("Type", "Use"));
        }
        
        static bool CheckBlockId(Player p, string arg, out int blockId) {
            if (!int.TryParse(arg, out blockId)) {
                Player.SendMessage(p, "Provided block id is not a number."); return false;
            }
            if (blockId <= 0 || blockId >= 255) {
                Player.SendMessage(p, "Block id must be between 1-254"); return false;
            }
            return true;
        }
        
        static BlockDefinition consoleBD;
        static int consoleStep = 0;
        
        static BlockDefinition GetBD(Player p) { return p == null ? consoleBD : p.gbBlock; }
        
        static void SetBD(Player p, BlockDefinition bd) {
            if (p == null) consoleBD = bd;
            else p.gbBlock = bd;
        }
            
        static int GetStep(Player p) { return p == null ? consoleStep : p.gbStep; }
        
        static void SetStep(Player p, int step) {
            if (p == null) consoleStep = step;
            else p.gbStep = step;
        }
        
        public override void Help(Player p) {
            Player.SendMessage(p, "%T/globalblock <add/remove/list/edit>");
            Player.SendMessage(p, "%H   /gb add [id] - begins the creation a new custom block.");
            Player.SendMessage(p, "%H   /gb remove id - removes the custom block with the given id.");
            Player.SendMessage(p, "%H   /gb list [offset] - lists all custom blocks.");
            Player.SendMessage(p, "%H   /gb edit id property value - edits the given property of the custom block with the given id.");
            Player.SendMessage(p, "%HTo see the list of editable properties, type /gb edit.");
        }
    }
}