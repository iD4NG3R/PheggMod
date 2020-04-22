﻿using GameCore;
using MEC;
using Mirror;
using PheggMod.API.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PheggMod.Commands
{
    public class CustomInternalCommands
    {
        public CustomInternalCommands()
        {
            nodamageplayers = new Dictionary<int, GameObject>();
        }

        internal static char[] validUnits = { 'm', 'h', 'd', 'w', 'M', 'y' };
        internal static TimeSpan GetBanDuration(char unit, int amount)
        {
            switch (unit)
            {
                default:
                    return new TimeSpan(0, 0, amount, 0);
                case 'h':
                    return new TimeSpan(0, amount, 0, 0);
                case 'd':
                    return new TimeSpan(amount, 0, 0, 0);
                case 'w':
                    return new TimeSpan(7 * amount, 0, 0, 0);
                case 'M':
                    return new TimeSpan(30 * amount, 0, 0, 0);
                case 'y':
                    return new TimeSpan(365 * amount, 0, 0, 0);
            }
        }
        public static bool CheckPermissions(CommandSender sender, string queryZero, PlayerPermissions perm)
        {
            if (ServerStatic.IsDedicated && sender.FullPermissions)
            {
                return true;
            }
            if (PermissionsHandler.IsPermitted(sender.Permissions, perm))
            {
                return true;
            }

            sender.RaReply(queryZero.ToUpper() + "#You don't have permissions to execute this command.\nMissing permission: " + perm, false, true, "");
            return false;
        }
        public static List<GameObject> GetPlayersFromString(string users)
        {
            string[] playerStrings = users.Split('.');
            List<GameObject> playerList = new List<GameObject>();

            foreach (string player in playerStrings)
            {
                GameObject go = PlayerManager.players.Where(p => p.GetComponent<RemoteAdmin.QueryProcessor>().PlayerId.ToString() == player || p.GetComponent<NicknameSync>().MyNick == player).FirstOrDefault();
                if (go.Equals(default(GameObject)) || go == null) continue;
                else
                {
                    playerList.Add(go);
                }
            }

            return playerList;
        }

        [PMCommand("oban"), PMAlias("offlineban", "ltapban"), PMParameters("userid", "duration", "reason"), PMCanExtend(true), PMPermission(PlayerPermissions.BanningUpToDay)]
        public void cmd_oban(CommandInfo info)
        {
            CommandSender sender = info.commandSender;
            string[] arg = info.commandArgs;

            if (arg.Count() < 4)
            {
                sender.RaReply(arg[0].ToUpper() + "#Command expects 3 or more arguments ([UserID], [Minutes], [Reason])", false, true, "");
                return;
            }
            else if (!arg[1].Contains('@'))
            {
                sender.RaReply(arg[0].ToUpper() + "#Invalid UserID given", false, true, "");
                return;
            }

            char unit = arg[2].ToString().Where(Char.IsLetter).ToArray()[0];
            if (!int.TryParse(new string(arg[2].Where(Char.IsDigit).ToArray()), out int amount) || !CustomInternalCommands.validUnits.Contains(unit) || amount < 1)
            {
                sender.RaReply(arg[0].ToUpper() + "#Invalid duration", false, true, "");
                return;
            }

            TimeSpan duration = CustomInternalCommands.GetBanDuration(unit, amount);
            string reason = string.Join(" ", arg.Skip(3));

            if (duration.Minutes > 60 && !CustomInternalCommands.CheckPermissions(sender, arg[0], PlayerPermissions.KickingAndShortTermBanning))
                return;
            else if (duration.Minutes > 1440 && !CustomInternalCommands.CheckPermissions(sender, arg[0], PlayerPermissions.BanningUpToDay))
                return;

            BanHandler.IssueBan(new BanDetails
            {
                OriginalName = "Offline player",
                Id = arg[1],
                Issuer = sender.Nickname,
                IssuanceTime = DateTime.UtcNow.Ticks,
                Expires = DateTime.UtcNow.Add(duration).Ticks,
                Reason = reason.Replace(Environment.NewLine, "")
            }, BanHandler.BanType.UserId);

            sender.RaReply(arg[0].ToUpper() + $"#{arg[1]} was offline banned for {arg[2]}", true, true, "");
        }

        [PMCommand("nukelock"), PMAlias("nlock", "nukel", "locknuke"), PMParameters(), PMPermission(PlayerPermissions.WarheadEvents)]
        public void cmd_NukeLock(CommandInfo info)
        {
            EventTriggers.PMAlphaWarheadController.nukeLock = !EventTriggers.PMAlphaWarheadController.nukeLock;

            info.commandSender.RaReply(info.commandArgs[0] + $"#Warhead lock has been {(EventTriggers.PMAlphaWarheadController.nukeLock ? "enabled" : "disabled")}", true, true, "");
        }

        [PMCommand("nuke"), PMParameters("enable/disable"), PMPermission(PlayerPermissions.WarheadEvents)]
        public void cmd_Nuke(CommandInfo info)
        {
            switch (info.commandArgs[1].ToLower())
            {
                case "enable":
                case "on":
                    EventTriggers.PMAlphaWarheadNukesitePanel.Enable();
                    info.commandSender.RaReply(info.commandName + $"#Warhead has been enabled", true, true, "");
                    break;
                default:
                    EventTriggers.PMAlphaWarheadNukesitePanel.Disable();
                    info.commandSender.RaReply(info.commandName + $"#Warhead has been disabled", true, true, "");
                    break;
            }
        }

        [PMCommand("pbc"), PMAlias("personalbroadcast", "privatebroadcast"), PMParameters("playerid", "seconds", "message"), PMCanExtend(true)]
        public void cmd_PBC(CommandInfo info)
        {
            string[] arg = info.commandArgs;
            CommandSender sender = info.commandSender;

            if (!CustomInternalCommands.CheckPermissions(sender, arg[0], PlayerPermissions.Broadcasting))
                return;

            bool success = uint.TryParse(arg[2], out uint duration);

            if (arg.Count() < 4)
            {
                sender.RaReply(arg[0].ToUpper() + "#Command expects 3 or more arguments ([Players], [Seconds], [Message])", false, true, "");
                return;
            }
            else if (!success || duration < 1 || duration > 255)
            {
                sender.RaReply(arg[0].ToUpper() + "#Invalid duration given", false, true, "");
                return;
            }

            List<GameObject> playerList = CustomInternalCommands.GetPlayersFromString(arg[1]);

            string message = string.Join(" ", arg.Skip(3));

            foreach (GameObject player in playerList)
                player.GetComponent<Broadcast>().TargetAddElement(player.GetComponent<NetworkConnection>(), message, duration, false);

            sender.RaReply(arg[0].ToUpper() + "Broadcast sent!", true, true, "");
        }

        [PMCommand("drop"), PMAlias("dropall", "dropinv"), PMParameters("playerid"), PMPermission(PlayerPermissions.PlayersManagement)]
        public void cmd_Drop(CommandInfo info)
        {
            string[] arg = info.commandArgs;

            if (!CustomInternalCommands.CheckPermissions(info.commandSender, arg[0], PlayerPermissions.PlayersManagement))
                return;

            List<GameObject> playerList = CustomInternalCommands.GetPlayersFromString(arg[2]);

            foreach (GameObject player in playerList)
                player.GetComponent<Inventory>().ServerDropAll();

            info.commandSender.RaReply(info.commandName + $"#Player {(playerList.Count > 1 ? "inventories" : "inventory")} dropped", true, true, "");
        }

        [PMCommand("kill"), PMParameters("playerid"), PMPermission(PlayerPermissions.PlayersManagement)]
        public void cmd_Kill(CommandInfo info)
        {
            string[] arg = info.commandArgs;
            CommandSender sender = info.commandSender;

            if (!CustomInternalCommands.CheckPermissions(sender, arg[0], PlayerPermissions.PlayersManagement))
                return;

            List<GameObject> playerList = CustomInternalCommands.GetPlayersFromString(arg[1]);

            foreach (GameObject player in playerList)
                player.GetComponent<PlayerStats>().HurtPlayer(new PlayerStats.HitInfo(9999f, sender.Nickname, DamageTypes.None, info.gameObject.GetComponent<RemoteAdmin.QueryProcessor>().PlayerId), player);

            info.commandSender.RaReply(info.commandName + $"#Killed {playerList.Count} {(playerList.Count > 1 ? "players" : "player")}", true, true, "");
        }

        internal static bool isLightsout = false;
        [PMCommand("lightsout"), PMParameters(), PMPermission(PlayerPermissions.FacilityManagement)]
        public void cmd_Lightsout(CommandInfo info) => Timing.RunCoroutine(Lightsout(info));
        private IEnumerator<float> Lightsout(CommandInfo info)
        {
            if (!isLightsout)
            {
                isLightsout = true;

                info.commandSender.RaReply(info.commandName + $"#Facility lights have been disabled!", true, true, "");

                foreach (GameObject player in PlayerManager.players)
                    player.GetComponent<Broadcast>().TargetAddElement(player.GetComponent<NetworkConnection>(), $"Lightsout has been enabled. This will cause occasional rapid flickering of lights throughout HCZ and LCZ", 20, false);

                yield return Timing.WaitForSeconds(9f);

                PlayerManager.localPlayer.GetComponent<MTFRespawn>().RpcPlayCustomAnnouncement("ERROR IN FACILITY LIGHT CONTROL . SYSTEM TERMINATION IN 3 . 2 . 1", false, true);

                foreach (GameObject player in PlayerManager.players)
                    player.GetComponent<Inventory>().AddNewItem(ItemType.Flashlight);

                yield return Timing.WaitForSeconds(11f);

                Timing.RunCoroutine(CheckLights());
            }
            else if (isLightsout)
            {
                isLightsout = false;
                info.commandSender.RaReply(info.commandName + $"#Facility lights will be enabled next cycle!", true, true, "");
            }

            yield return 0f;
        }
        private IEnumerator<float> CheckLights()
        {
            yield return Timing.WaitForSeconds(33f);

            if (!isLightsout)
            {
                PlayerManager.localPlayer.GetComponent<MTFRespawn>().RpcPlayCustomAnnouncement("FACILITY LIGHT CONTROL SYSTEM REPAIR COMPLETE . LIGHT SYSTEM ENGAGED", false, true);
                yield return 0f;
            }
            else
            {
                foreach (Generator079 gen in UnityEngine.Object.FindObjectsOfType<Generator079>())
                    gen.RpcCustomOverchargeForOurBeautifulModCreators(30f, false);

                Timing.RunCoroutine(CheckLights());
            }
        }

        internal static bool reloadPlugins = false;
        [PMCommand("pluginreload"), PMAlias("reloadplugins", "plreload"), PMParameters(), PMPermission(PlayerPermissions.ServerConfigs)]
        public void cmd_ReloadPlugins(CommandInfo info)
        {
            if (!reloadPlugins)
            {
                reloadPlugins = true;
                info.commandSender.RaReply(info.commandName + $"#Server plugins will be reloaded upon round restart.", true, true, "");

                Base.Warn($"{info.commandSender.Nickname} ({info.gameObject.GetComponent<CharacterClassManager>().UserId}) has triggered the pluginreload command!\nAll plugins and custom commands will be reloaded upon round restart");
            }
            else
            {
                info.commandSender.RaReply(info.commandName + $"#Server plugins are already set to reload upon round restart.", true, true, "");
            }
        }

        [PMCommand("nevergonna"), PMParameters("give", "you", "up")]
        public void cmd_RickRoll(CommandInfo info)
        {
            info.commandSender.RaReply(info.commandName + $"#Give you up,\nNever gonna let you down.\nNever gonna run around,\nDesert you.\nNever gonna make you cry,\nNever gonna say goodbye.\nNever gonna tell a lie,\nAnd hurt you.", true, true, "");
        }

        public static Dictionary<int, GameObject> nodamageplayers { get; private set; }
        [PMCommand("nodamage"), PMParameters("playerid")]
        public void cmd_nodamage(CommandInfo info)
        {
            string[] arg = info.commandArgs;
            CommandSender sender = info.commandSender;

            if (!CustomInternalCommands.CheckPermissions(sender, arg[0], PlayerPermissions.PlayersManagement))
                return;

            List<GameObject> playerList = CustomInternalCommands.GetPlayersFromString(arg[1]);

            foreach (GameObject player in playerList)
            {
                int id = player.GetComponent<RemoteAdmin.QueryProcessor>().PlayerId;

                if (!nodamageplayers.ContainsKey(id))
                {
                    nodamageplayers.Add(id, player);
                }
                else
                {
                    nodamageplayers.Remove(id);
                }
            }
        }

        /* //[PMCommand("endround"), PMAlias("forceend"), PMParameters(), PMPermission(PlayerPermissions.RoundEvents)]
        //public void cmd_ForceEnd(CommandInfo info) => Timing.RunCoroutine(ForceEnd(info));
        //private IEnumerator<float> ForceEnd(CommandInfo info)
        //{
        //    RoundSummary rS = RoundSummary.singleton;

        //    RoundSummary.SumInfo_ClassList newList = new RoundSummary.SumInfo_ClassList();
        //    foreach (GameObject plr in PlayerManager.players)
        //    {
        //        CharacterClassManager ccm = plr.GetComponent<CharacterClassManager>();

        //        switch (ccm.Classes.SafeGet(ccm.CurClass).team)
        //        {
        //            case Team.CHI:
        //                newList.chaos_insurgents++; break;
        //            case Team.MTF:
        //                newList.mtf_and_guards++; break;
        //            case Team.CDP:
        //                newList.class_ds++; break;
        //            case Team.RSC:
        //                newList.scientists++; break;
        //            case Team.SCP:
        //                if (ccm.CurClass == RoleType.Scp0492)
        //                    newList.zombies++;
        //                else
        //                    newList.scps_except_zombies++;
        //                break;
        //            default:
        //                break;
        //        }
        //    }

        //    newList.warhead_kills = (AlphaWarheadController.Host.detonated ? AlphaWarheadController.Host.warheadKills : -1);
        //    newList.time = TimeSpan.FromSeconds((int)Time.realtimeSinceStartup - DateTime.Now.Second).Seconds;

        //    int ttr = ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10);
        //    EventTriggers.PMRoundSummary.singleton.CallRpcShowRoundSummary(rS.classlistStart, newList, RoundSummary.LeadingTeam.Draw, RoundSummary.escaped_ds, RoundSummary.escaped_scientists, RoundSummary.kills_by_scp, ttr);

        //    yield return Timing.WaitForSeconds(ttr);

        //    PlayerManager.localPlayer.GetComponent<PlayerStats>().Roundrestart();
        //} */
    }
}