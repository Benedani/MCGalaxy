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
using System.Collections.Generic;
using System.ComponentModel;
using MCGalaxy.Games;

namespace MCGalaxy.Gui {
    public sealed class ZombieProperties {
        
        [Description("Whether at the end of each round, different levels are randomly picked for the next round. " +
                     "You should generallly leave this as true.")]
        [Category("Levels settings")]
        [DisplayName("Change levels")]
        public bool ChangeLevels { get; set; }
        
        [Description("Whether worlds with a '+' in their name (i.e. from /os map add) are ignored " +
                     "when choosing levels for zombie survival.")]
        [Category("Levels settings")]
        [DisplayName("Ignore personal worlds")]
        public bool IgnorePersonalWorlds { get; set; }
        
        [Description("Comma separated list of levels that are never chosen for zombie survival. (e.g. main,spawn)")]
        [Category("Levels settings")]
        [DisplayName("Ignored level list")]
        public string IgnoredLevelsList { get; set; }
        
        [Description("Comma separated list of levels to use for zombie survival. (e.g. map1,map2,map3) " +
                     "If this is left blank, then all levels are used.")]
        [Category("Levels settings")]
        [DisplayName("Levels list")]
        public string LevelsList { get; set; }
        
        [Description("Whether changes made to a map during a round of zombie survival are permanently saved. " +
                     "It is HIGHLY recommended that you leave this as false.")]
        [Category("Levels settings")]
        [DisplayName("Save level changes")]
        public bool SaveLevelChanges { get; set; }
        
        
        [Description("Whether players are allowed to pillar in zombie survival. " +
                     "Note this can be overriden for specific maps using /mset.")]
        [Category("General settings")]
        [DisplayName("Pillaring allowed")]
        public bool Pillaring { get; set; }
        
        [Description("Whether players are allowed to use /spawn in zombie survival. " +
                     "You should generallly leave this as false.")]
        [Category("Levels settings")]
        [DisplayName("Respawning allowed")]
        public bool Respawning { get; set; }
        
        [Description("Whether the main/spawn level is always set to the current level of zombie survival. " +
                     "You should set this to true if the server is purely for zombie survival. ")]
        [Category("General settings")]
        [DisplayName("Set main level")]
        public bool SetMainLevel { get; set; }
        
        [Description("Whether zombie survival should start when the server starts.")]
        [Category("General settings")]
        [DisplayName("Start immediately")]
        public bool StartImmediately { get; set; }    
        
        
        [Description("Max distance players are allowed to move between packets (for speedhack detection). " +
                     "32 units equals one block.")]
        [Category("Other settings")]
        [DisplayName("Max move distance")]
        public int MaxMoveDistance { get; set; }
        
        [Description("Distance between players before they are considered to have 'collided'. (for infecting). " +
                     "32 units equals one block.")]
        [Category("Other settings")]
        [DisplayName("Hitbox precision")]
        public int HitboxPrecision { get; set; }
        
        [Description("Whether the current map's name is included when a hearbeat is sent. " +
                     "This means it shows up in the server list as: \"Server name (current map name)\"")]
        [Category("Other settings")]
        [DisplayName("Include map in heartbeat")]
        public bool IncludeMapInHeartbeat { get; set; }
        

        [Description("Name to show above infected players. If this is left blank, then the player's name is used instead.")]
        [Category("Zombie settings")]
        [DisplayName("Name")]
        public string Name { get; set; }

        [Description("Model to use for infected players. If this is left blank, then 'zombie' model is used.")]
        [Category("Zombie settings")]
        [DisplayName("Model")]
        public string Model { get; set; }
        

        [Description("How many seconds an invisibility potion bought using /buy invisibility lasts.")]
        [Category("Human settings")]
        [DisplayName("Invisibility duration")]
        public int InvisibilityDuration { get; set; }
        
        [Description("Maximum number of invisibility potions a human is allowed to buy in a round.")]
        [Category("Human settings")]
        [DisplayName("Invisibility potions")]        
        public int InvisibilityPotions { get; set; }
        
        [Description("How many seconds an invisibility potion bought using /buy zinvisibility lasts.")]
        [Category("Zombie settings")]
        [DisplayName("Invisibility duration")]
        public int ZInvisibilityDuration { get; set; }
        
        [Description("Maximum number of invisibility potions a zombie is allowed to buy in a round.")]
        [Category("Zombie settings")]
        [DisplayName("Invisibility potions")]        
        public int ZInvisibilityPotions { get; set; }
        
        
        [Description("The percentage chance that a revive potion will actually disinfect a zombie.")]
        [Category("Revive settings")]
        [DisplayName("Chance")]
        public int Chance { get; set; }  
        
        [Description("The minimum number of seconds left in a round, below which /buy revive will not work.")]
        [Category("Revive settings")]
        [DisplayName("Insufficient time")]
        public int InsufficientTime { get; set; }
        
        [Description("Message shown when using /buy revive and the seconds left in a round is less than 'InsufficientTime'.")]
        [Category("Revive settings")]
        [DisplayName("Insufficient time message")]
        public string InsufficientTimeMessage { get; set; }
        
        [Description("The maximum number of seconds after a human is infected, after which /buy revive will not work.")]
        [Category("Revive settings")]
        [DisplayName("Expiry time")]
        public int ExpiryTime { get; set; }
        
        [Description("Message shown when a player uses /buy revive, and it actually disinfects them.")]
        [Category("Revive settings")]
        [DisplayName("Success message")]
        public string SuccessMessage { get; set; }
        
        [Description("Message shown when a player uses /buy revive, but does not disinfect them.")]
        [Category("Revive settings")]
        [DisplayName("Failure message")]
        public string FailureMessage { get; set; }
        
        public void LoadFromServer() {
            ChangeLevels = ZombieGameProps.ChangeLevels;
            IgnoredLevelsList = ZombieGameProps.IgnoredLevelList.Join(",");
            LevelsList = ZombieGameProps.LevelList.Join(",");
            SaveLevelChanges = ZombieGameProps.SaveLevelBlockchanges;
            IgnorePersonalWorlds = ZombieGameProps.IgnorePersonalWorlds;
            
            Pillaring = !ZombieGameProps.NoPillaring;
            Respawning = !ZombieGameProps.NoRespawn;
            SetMainLevel = ZombieGameProps.SetMainLevel;
            StartImmediately = ZombieGameProps.StartImmediately;
            
            MaxMoveDistance = ZombieGameProps.MaxMoveDistance;
            HitboxPrecision = ZombieGameProps.HitboxPrecision;
            IncludeMapInHeartbeat = ZombieGameProps.IncludeMapInHeartbeat;
            
            Name = ZombieGameProps.ZombieName;
            Model = ZombieGameProps.ZombieModel;
            InvisibilityDuration = ZombieGameProps.InvisibilityDuration;
            InvisibilityPotions = ZombieGameProps.InvisibilityPotions;
            ZInvisibilityDuration = ZombieGameProps.ZombieInvisibilityDuration;
            ZInvisibilityPotions = ZombieGameProps.ZombieInvisibilityPotions;
            
            Chance = ZombieGameProps.ReviveChance;
            InsufficientTime = ZombieGameProps.ReviveNoTime;
            InsufficientTimeMessage = ZombieGameProps.ReviveNoTimeMessage;
            ExpiryTime = ZombieGameProps.ReviveTooSlow;
            FailureMessage = ZombieGameProps.ReviveFailureMessage;
            SuccessMessage = ZombieGameProps.ReviveSuccessMessage;
        }
        
        public void ApplyToServer() {
            ZombieGameProps.ChangeLevels = ChangeLevels;
            string list = IgnoredLevelsList.Replace(" ", "");
            if (list == "") ZombieGameProps.IgnoredLevelList = new List<string>();
            else ZombieGameProps.IgnoredLevelList = new List<string>(list.Replace(" ", "").Split(','));
                
            list = LevelsList.Replace(" ", "");
            if (list == "") ZombieGameProps.LevelList = new List<string>();
            else ZombieGameProps.LevelList = new List<string>(list.Replace(" ", "").Split(','));
            ZombieGameProps.SaveLevelBlockchanges = SaveLevelChanges;
            ZombieGameProps.IgnorePersonalWorlds = IgnorePersonalWorlds;
            
            ZombieGameProps.NoPillaring = !Pillaring;
            ZombieGameProps.NoRespawn = !Respawning;
            ZombieGameProps.SetMainLevel = SetMainLevel;
            ZombieGameProps.StartImmediately = StartImmediately;
            
            ZombieGameProps.MaxMoveDistance = MaxMoveDistance;
            ZombieGameProps.HitboxPrecision = HitboxPrecision;
            ZombieGameProps.IncludeMapInHeartbeat = IncludeMapInHeartbeat;
            
            ZombieGameProps.ZombieName = Name.Trim();
            ZombieGameProps.ZombieModel = Model.Trim();
            if (ZombieGameProps.ZombieModel == "")
                ZombieGameProps.ZombieModel = "zombie";
            ZombieGameProps.InvisibilityDuration = InvisibilityDuration;
            ZombieGameProps.InvisibilityPotions = InvisibilityPotions;
            ZombieGameProps.ZombieInvisibilityDuration = ZInvisibilityDuration;
            ZombieGameProps.ZombieInvisibilityPotions = ZInvisibilityPotions; 
            
            ZombieGameProps.ReviveChance = Chance;
            ZombieGameProps.ReviveNoTime = InsufficientTime;
            ZombieGameProps.ReviveNoTimeMessage = InsufficientTimeMessage;
            ZombieGameProps.ReviveTooSlow = ExpiryTime;
            ZombieGameProps.ReviveFailureMessage = FailureMessage;
            ZombieGameProps.ReviveSuccessMessage = SuccessMessage;
        }
    }
}
