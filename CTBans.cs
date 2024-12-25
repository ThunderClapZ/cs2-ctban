using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes;
using Nexd.MySQL;
using StarCore.Utils;

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
    private static readonly string?[] remaining = new string?[64];
    private static readonly string?[] reason = new string?[64];
    private static readonly int?[] Showinfo = new int?[64];
    private static readonly bool?[] session = new bool?[64];


    public required ConfigBan Config { get; set; }


    public void OnConfigParsed(ConfigBan config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        WriteColor("CT BANS - Plugins has been [*LOADED*]", ConsoleColor.Green);
        CreateDatabase();
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);

    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!Lib.IsPlayerValidAlive(player)) return HookResult.Continue;
        if (player.PawnIsAlive)
        {
            var playerTeam = (CsTeam)player.TeamNum;
            if (playerTeam == CsTeam.CounterTerrorist && CheckBan(player) == true)
            {
                player.ChangeTeam(CsTeam.Terrorist);
                player.PrintToChat($" {ChatColors.Red}|-------------| {ChatColors.Default}你已被CTBAN{ChatColors.Red}|-------------|");
                player.PrintToChat($" {ChatColors.Red}|-------------| {ChatColors.Default}剩余时间:{remaining[player.Index]}{ChatColors.Red}|-------------|");
                player.PrintToChat($" {ChatColors.Red}|-------------| {ChatColors.Default}理由:{reason[player.Index]}{ChatColors.Red}|-------------|");
                player.PrintToChat($" {ChatColors.Red}|-------------| {ChatColors.Default}你已被CTBAN{ChatColors.Red}|-------------|");

                var tSpawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist").ToList();
                if (tSpawns.Count <= 0) return HookResult.Continue;
                Random random = new Random();
                int randomPostion = random.Next(1, tSpawns.Count);
                Vector cellPostion = tSpawns[randomPostion].AbsOrigin!.With();
                if (!player.PlayerPawn.IsValid) return HookResult.Continue;
                if (player.PlayerPawn.Value == null) return HookResult.Continue;
                var pawn = Lib.GetAlivePawn(player);
                if (pawn == null) return HookResult.Continue;
                pawn.Teleport(cellPostion);
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!Lib.IsPlayerValid(player)) return HookResult.Continue;
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
}
