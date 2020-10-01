using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;


namespace Oxide.Plugins
{
    [Info("Life Support", "OG61", "1.1.6")]
    [Description("Use reward points to prevent player from dying")]
    public class LifeSupport : CovalencePlugin
    {
        #region Plugin References
        [PluginReference]
        private Plugin ServerRewards;
        [PluginReference]
        private Plugin RaidableBases;
   
        
        #endregion

        #region Config
        private class Perms
        {
            public string Permission { get; set; }
            public int Cost { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Use Server Rewards (true/false)")]
            public bool UseServerRewards = false;


            [JsonProperty(PropertyName = "Exclude RaidableBases Zones (true/false)")]
            public bool UseRaidableBases = false;

            [JsonProperty(PropertyName = "Enable Log file (true/false)")]
            public bool LogToFile = true;

            [JsonProperty(PropertyName = "Log output to console (true/false)")]
            public bool LogToConsole = true;

            [JsonProperty(PropertyName = "Permissions and cost", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Perms> perms = new List<Perms>()
            { 
                new Perms() {Permission = "lifesupport.default", Cost = 400 },
                new Perms() {Permission = "lifesupport.vip", Cost = 200},
                new Perms() {Permission = "lifesupport.admin", Cost = 0}
            };
            
        }

        private PluginConfig config;

        protected override void LoadConfig()
        {
             base.LoadConfig();
            try
            {
                config = Config.ReadObject<PluginConfig>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
                Logger("ConfigError");
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new PluginConfig();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion //Config

        #region Data
        private Data data;

        private class Data
        {
             public List<string> activatedIDs = new List<string>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, data);
        #endregion //Data

        #region Oxide Hooks
        private void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.ReadObject<Data>(Name);

            config.perms.ForEach(p =>
            {
                    permission.RegisterPermission(p.Permission, this);
            });
        }
        
        //Prevent player from entering wounded state
        private object OnPlayerWound(BasePlayer player)
        {
           if (player == null) return null;
           if (config.UseRaidableBases && (RaidableBases?.Call<bool>("EventTerritory", player.transform.position) ?? false)) return null;
            
           bool preventWounded = false;
            
           config.perms.ForEach(p =>
            {
                 if (permission.UserHasPermission(player.IPlayer.Id, p.Permission)&&
                     data.activatedIDs.Contains(player.IPlayer.Id)) 
                {
                    preventWounded = true;
                }
             });
            if (preventWounded) return true; else return null;
        }

       
        private object OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            bool preventDeath = false;
            int costOfLife = int.MaxValue;
            if (player.IsNpc  || player == null) return null;
            if (config.UseRaidableBases && (RaidableBases?.Call<bool>("EventTerritory", player.transform.position) ?? false)) return null;
            if (data.activatedIDs.Contains(player.IPlayer.Id))
            {
                config.perms.ForEach(p => 
                {
                    if (permission.UserHasPermission(player.IPlayer.Id, p.Permission))
                    {
                        preventDeath = true;
                        if (p.Cost < costOfLife) costOfLife = p.Cost;
                    }
                });
                if (!preventDeath) return null; //Player does not have permission so exit
                if (config.UseServerRewards)
                {
                    if (ServerRewards == null || !ServerRewards.IsLoaded)
                    {
                        //Server Rewards enabled but not present. Log error and return
                        Message("ServerRewardsNull", player.IPlayer);
                        Logger("ServerRewardsNull");
                        return null;
                    }
                    if (ServerRewards.Call<int>("CheckPoints", player.IPlayer.Id) >= costOfLife)
                    {
                        ServerRewards.Call<bool>("TakePoints", player.IPlayer.Id, costOfLife);
                    }
                    else
                    {
                        Message("CantAfford", player.IPlayer);
                        Logger("DiedCouldntAfford", player.IPlayer);
                        return null; //Player can't afford so exit
                    }
                    Message("SavedYourLifeCost", player.IPlayer, costOfLife);
                    Logger("SavedLife", player.IPlayer, costOfLife);
                }
                else //Not using ServerRewards
                {
                    Message("SavedYourLife", player.IPlayer);
                    Logger("ServerRewardsInactiveSavedLife", player.IPlayer);
                }
                player.health= 100f;
                if (player.IsWounded()) player.StopWounded();
                return true;
            }
			Logger("DiedNotActive", player.IPlayer);
            return null; //Life Support not activated for this player so exit
        }

        bool CanDropActiveItem(BasePlayer player)
        {
            if (player == null) return true;

            if (config.UseRaidableBases)
            {
                if (RaidableBases == null || !RaidableBases.IsLoaded) return true;
            {
                if (RaidableBases == null || !RaidableBases.IsLoaded) return true;
                if (RaidableBases?.Call<bool>("EventTerritory", player.transform.position) ?? false) return true;
            }
            return data.activatedIDs.Contains(player.IPlayer.Id) ? false : true;
        }
            return data.activatedIDs.Contains(player.IPlayer.Id) ? false : true;
        }


        #endregion //Oxide Hooks  

        #region Helpers

        private string GetLang(string key, string id = "", params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, id), args);
        }

        private void Logger(string key, IPlayer player = null, params object[] args)
        {
            string s = GetLang(key, player != null ? player.Id : "", args);
            string ps = "";
            if (player != null) ps = $"{player.Name} ({player.Id}) ";
            s = $"[{DateTime.Now}] {ps} {s}";
            if (config.LogToFile) LogToFile("LifeSupport", s, this);
            if (config.LogToConsole) Puts(s);
        }

        private void Message(string key, IPlayer player, params object[] args)
        {
           player.Reply(GetLang(key, player.Id, args));
        }

        #endregion //Helpers
            
        #region Commands
        [Command("lifesupport")]
        private void SaveLife(IPlayer player, string msg, string[] args)
        {
            int costOfLife = int.MaxValue;
            bool hasPermission = false;

            if (args.Length > 0)
            {
                if (args.Length == 1 && args[0].ToLower() == "help")
                {
                    Message("Help", player);
                    return;
                }
                Message("DontUnderstand", player);
                return;
            }
            config.perms.ForEach(p =>
            {
                if (permission.UserHasPermission(player.Id, p.Permission))
                {
                    hasPermission = true;
                    if (p.Cost < costOfLife) costOfLife = p.Cost; //Get the lowest cost if player has multiple permissions
                }
            });

            if (data.activatedIDs.Contains(player.Id))
            {
                data.activatedIDs.Remove(player.Id);
                Message("Deactivated", player);
                Logger("Deactivated" , player);
                SaveData();
            }
            else if(hasPermission) 
            {
                data.activatedIDs.Add(player.Id);
                Message("Activated", player,  config.UseServerRewards ? costOfLife: 0);
                Logger("Activated", player, costOfLife);
                SaveData();
            }
            else Message("NoPermission", player);
        }
        #endregion

        #region Localization
     
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ServerRewardsNull"] = "LifeSupport could not save your life. \n"+
                "ServerRewards is enabled but the ServerRewards plugin is not available.",
                ["DontUnderstand"] = "Don't Understand.",
                ["DiedNotActive"] = "Player died. LifeSupport not active.",
                ["ConfigError"] = "Error reading config file. Defaut configuration used.",
                ["NoPermission"] = "You do not have permission to use this command.",
                ["CantAfford"] = "Sorry, insufficent reward points to use LifeSupport.",
                ["DiedCouldntAfford"] = "Player died. Could not afford LifeSupport.",
                ["Deactivated"] = "Life Support de-activated.",
                ["Activated"] = "Life Support activated.  Cost per life {0} RP",
                ["SavedYourLifeCost"] = "Life Support saved your life. Cost: {0} RP",
                ["SavedYourLife"] = "Life Support saved your life.",
                ["SavedLife"] = "Prevented death. Cost: {0} RP",
                ["ServerRewardsInactiveSavedLife"] = "Prevented death. Server Rewards inactive",
                ["Help"] = "When active LifeSupport will prevent a player's death if\n" +
                "they have permission and a sufficent amount of reward points \n" +
                "or if Server Rewards is turned off. \n" +
                "It also prevents dropping their active item.\n" +
                "Type /LifeSupport in chat to toggle on and off."
            }, this);
        }
        #endregion
    }
}