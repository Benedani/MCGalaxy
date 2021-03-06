/*
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

namespace MCGalaxy {
    
    /// <summary> Manages the awards the server has, and which players have which awards. </summary>
    public static class Awards {
        
        public struct PlayerAward { public string Name; public List<string> Awards; }
        
        public class Award { public string Name, Description; }
        
        /// <summary> List of all awards the server has. </summary>
        public static List<Award> AwardsList = new List<Award>();

        /// <summary> List of all players who have awards. </summary>
        public static List<PlayerAward> PlayerAwards = new List<PlayerAward>();
        

        #region I/O
        
        public static void Load() {
            if (!File.Exists("text/awardsList.txt")) {
                using (CP437Writer w = new CP437Writer("text/awardsList.txt")) {
                    w.WriteLine("#This is a full list of awards. The server will load these and they can be awarded as you please");
                    w.WriteLine("#Format is:");
                    w.WriteLine("# AwardName : Description of award goes after the colon");
                    w.WriteLine();
                    w.WriteLine("Gotta start somewhere : Built your first house");
                    w.WriteLine("Climbing the ladder : Earned a rank advancement");
                    w.WriteLine("Do you live here? : Joined the server a huge bunch of times");
                }
            }

            AwardsList = new List<Award>();
            PropertiesFile.Read("text/awardsList.txt", AwardsListLineProcessor, ':');
            PlayerAwards = new List<PlayerAward>();
            PropertiesFile.Read("text/playerAwards.txt", PlayerAwardsLineProcessor, ':');
        }
        
        static void AwardsListLineProcessor(string key, string value) {
            if (value == "") return;
            Add(key, value);
        }
        
        static void PlayerAwardsLineProcessor(string key, string value) {
            if (value == "") return;
            PlayerAward pl;
            pl.Name = key.ToLower();
            pl.Awards = new List<string>();
            
            if (value.IndexOf(',') != -1)
                foreach (string award in value.Split(','))
                    pl.Awards.Add(award);
            else if (value != "")
                pl.Awards.Add(value);
            PlayerAwards.Add(pl);
        }

        static readonly object awardLock = new object();
        public static void SaveAwards() {
            lock (awardLock)
                using (CP437Writer w = new CP437Writer("text/awardsList.txt"))
            {
                w.WriteLine("# This is a full list of awards. The server will load these and they can be awarded as you please");
                w.WriteLine("# Format is:");
                w.WriteLine("# AwardName : Description of award goes after the colon");
                w.WriteLine();
                foreach (Award award in AwardsList)
                    w.WriteLine(award.Name + " : " + award.Description);
            }
        }
        
        static readonly object playerLock = new object();
        public static void SavePlayers() {
            lock (playerLock)
                using (StreamWriter w = new StreamWriter("text/playerAwards.txt"))
            {
                foreach (PlayerAward pA in PlayerAwards)
                    w.WriteLine(pA.Name.ToLower() + " : " + pA.Awards.Join(","));
            }
        }
        #endregion
        
        
        #region Player awards
        
        /// <summary> Adds the given award to that player's list of awards. </summary>
        public static bool GiveAward(string playerName, string name) {
            foreach (PlayerAward pl in PlayerAwards) {
                if (!pl.Name.CaselessEq(playerName)) continue;
                
                foreach (string award in pl.Awards) {
                    if (award.CaselessEq(name)) return false;
                }
                pl.Awards.Add(name);
                return true;
            }
            
            PlayerAward newPl;
            newPl.Name = playerName;
            newPl.Awards = new List<string>();
            newPl.Awards.Add(name);
            PlayerAwards.Add(newPl);
            return true;
        }
        
        /// <summary> Removes the given award from that player's list of awards. </summary>
        public static bool TakeAward(string playerName, string name) {
            foreach (PlayerAward pl in PlayerAwards) {
                if (!pl.Name.CaselessEq(playerName)) continue;
                
                for (int i = 0; i < pl.Awards.Count; i++) {
                    if (!pl.Awards[i].CaselessEq(name)) continue;
                    pl.Awards.RemoveAt(i); 
                    return true;
                }
                return false;
            }
            return false;
        }
        
        /// <summary> Returns the percentage of all the awards that the given player has. </summary>
        public static string AwardAmount(string playerName) {
            int numAwards = AwardsList.Count;
            foreach (PlayerAward pl in PlayerAwards) {
                if (!pl.Name.CaselessEq(playerName)) continue;
                double percentage = Math.Round(((double)pl.Awards.Count / numAwards) * 100, 2);
                return "&f" + pl.Awards.Count + "/" + numAwards + " (" + percentage + "%)" + Server.DefaultColor;
            }
            return "&f0/" + numAwards + " (0%)" + Server.DefaultColor;
        }
        
        /// <summary> Finds the list of awards that the given player has. </summary>
        public static List<string> GetPlayerAwards(string name) {
            foreach (PlayerAward pl in PlayerAwards)
                if (pl.Name.CaselessEq(name)) return pl.Awards;
            return new List<string>();
        }
        #endregion
        
        
        #region Awards management
        
        /// <summary> Adds a new award with the given name. </summary>
        public static bool Add(string name, string desc) {
            if (Exists(name)) return false;

            Award award = new Award();
            award.Name = name.Trim();
            award.Description = desc.Trim();
            AwardsList.Add(award);
            return true;
        }
        
        /// <summary> Removes the award with the given name. </summary>
        public static bool Remove(string name) {
            foreach (Award award in AwardsList) {
                if (!award.Name.CaselessEq(name)) continue;
                AwardsList.Remove(award);
                return true;
            }
            return false;
        }
        
        /// <summary> Whether an award with that name exists. </summary>
        public static bool Exists(string name) {
            foreach (Award award in AwardsList)
                if (award.Name.CaselessEq(name)) return true;
            return false;
        }
        
        /// <summary> Whether an award with that name exists. </summary>
        public static string FindExact(string name) {
            foreach (Award award in AwardsList)
                if (award.Name.CaselessEq(name)) return award.Name;
            return null;
        }
        
        public static string FindMatches(Player p, string name, out int matches) {
            Award award = Utils.FindMatches<Award>(p, name, out matches, AwardsList,
                                                   a => true, a => a.Name, "awards");
            return award == null ? null : award.Name;
        }
        
        /// <summary> Gets the description of the award matching the given name, 
        /// or an empty string if no matching award was found. </summary>
        public static string GetDescription(string name) {
            foreach (Award award in AwardsList)
                if (award.Name.CaselessEq(name)) return award.Description;
            return "";
        }
        #endregion
    }
}
