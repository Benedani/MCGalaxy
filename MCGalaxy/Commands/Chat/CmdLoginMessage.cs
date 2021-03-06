/*
    Copyright 2011 MCForge
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.osedu.org/licenses/ECL-2.0
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System.IO;

namespace MCGalaxy.Commands {
    public sealed class CmdLoginMessage : EntityPropertyCmd {
        public override string name { get { return "loginmessage"; } }
        public override string shortcut { get { return "loginmsg"; } }
        public override string type { get { return CommandTypes.Chat; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "+ can change the login message of others") }; }
        }
        public override void Use(Player p, string message) { UsePlayer(p, message, "login message"); }
        
        protected override void SetPlayerData(Player p, Player who, string[] args) {
            if (args.Length == 1) {
                string path = PlayerDB.LoginPath(who.name);
                if (File.Exists(path)) File.Delete(path);
                Player.Message(p, "The login message of {0} %Shas been removed.",
                               who.ColoredName);
            } else {
                PlayerDB.SetLoginMessage(who.name, args[1]);
                Player.Message(p, "The login message of {0} %Shas been changed to: {1}",
                               who.ColoredName, args[1]);
            }
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/loginmessage [player] [message]");
            Player.Message(p, "%HSets the login message shown for that player.");
        }
    }
}
