using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CTAPI;
using Nexd.MySQL;
using System;
using System.ComponentModel;
using System.Drawing;

namespace CTBans;
[MinimumApiVersion(100)]

public static class GetUnixTime
{
    public static int GetUnixEpoch(this DateTime dateTime)
    {
        var unixTime = dateTime.ToUniversalTime() -
                       new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return (int)unixTime.TotalSeconds;
    }
}

public partial class CTBans : BasePlugin, IPluginConfig<ConfigBan>
{
    public override string ModuleName => "CTBans";
    public override string ModuleAuthor => "DeadSwim";
    public override string ModuleDescription => "Banning players to join in CT.";
    public override string ModuleVersion => "V. 1.0.0";

    private static readonly bool?[] banned = new bool?[64];
    public static readonly string?[] remaining = new string?[64];
    public static readonly string?[] reason = new string?[64];
    private static readonly int?[] Showinfo = new int?[64];
    private static readonly bool?[] session = new bool?[64];

    public CoreAPI CoreAPI { get; set; } = null!;
    #pragma warning disable CS0436
    private static PluginCapability<IAPI> APICapability { get; } = new("ctban:api");
    #pragma warning disable CS8618
    public ConfigBan Config { get; set; }


    public void OnConfigParsed(ConfigBan config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        CoreAPI = new CoreAPI(this);
        if (CoreAPI != null)
        {
            Capabilities.RegisterPluginCapability(APICapability, () => CoreAPI);
        }

        WriteColor("CT BANS - Plugins has been [*LOADED*]", ConsoleColor.Green);
        CreateDatabase();

        AddCommand(Config.SessionBan, "Session Ban Command", addsessionban);
        AddCommand(Config.CTBan, "Ban Command", addban);
        AddCommand(Config.UNBan, "UNBan Command", UnbanCT);
        AddCommand(Config.IsBanned, "IsBanned Command", InfobanCT);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);

        AddCommandListener("jointeam", OnPlayerChangeTeam);

    }
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event.Userid == null) return HookResult.Continue;
        CCSPlayerController player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;
        var client = player.Index;
        if(CheckBan(player) == true)
        {
            var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(GetPlayerBanTime(player)) - DateTimeOffset.UtcNow;
            var nowtimeis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeRemainingFormatted =
            $"{timeRemaining.Days}d {timeRemaining.Hours}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

            if (GetPlayerBanTime(player) < nowtimeis)
            {
                MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);
                var steamid = player.SteamID.ToString();
                MySql.Table("deadswim_ctbans").Where($"ban_steamid = '{steamid}'").Delete();
                banned[client] = false;
                remaining[client] = null;
                reason[client] = null;
                Showinfo[client] = null;
                session[client] = false;
            }
            else
            {
                banned[client] = true;
                remaining[client] = $"{timeRemainingFormatted}";
                reason[client] = GetPlayerBanReason(player);
            }
        }
        else
        {
            banned[client] = false;
            remaining[client] = null;
            reason[client] = null;
            session[client] = false;
        }
        return HookResult.Continue;
    }
    public HookResult OnPlayerChangeTeam(CCSPlayerController? player, CommandInfo command)
    {
        var client = player!.Index;

        if (!Int32.TryParse(command.ArgByIndex(1), out int team_switch))
        {
            return HookResult.Continue;
        }

        if (player == null || !player.IsValid)
            return HookResult.Continue;
        CheckIfIsBanned(player);

        CCSPlayerPawn? playerpawn = player.PlayerPawn.Value;
        var player_team = team_switch;


        if(player_team == Config.TeamOfBan)
        {
            if (banned[client] == true)
            {
                Showinfo[client] = 1;
                player.ExecuteClientCommand($"play {Config.DennySound}");
                return HookResult.Stop;
            }
        }

        return HookResult.Continue;
    }
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event.Userid == null) return HookResult.Continue;
        CCSPlayerController player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        if(CheckBan(player) && player.Team == CsTeam.CounterTerrorist)
        {
            player.CommitSuicide(false, false);
            player.SwitchTeam(CsTeam.Terrorist);
        }
        
        return HookResult.Continue;
    }
}