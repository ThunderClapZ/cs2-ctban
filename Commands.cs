using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Nexd.MySQL;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using StarCore.Utils;

namespace CTBans;

public partial class CTBans
{
    [ConsoleCommand("css_ctsessionban", "Ban player to CT")]
    public void addsessionban(CCSPlayerController? player, CommandInfo info)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/ban"))
        {
            info.ReplyToCommand($" {Config.Prefix} 你没有权限使用此指令!");
            return;
        }
        var Player = info.ArgByIndex(1);
        var Reason = info.GetArg(2);

        if (Reason == null)
        {
            info.ReplyToCommand($" {Config.Prefix} 理由不合法");
            return;
        }

        foreach (var find_player in Utilities.GetPlayers())
        {
            if (find_player.PlayerName.ToString() == Player)
            {
                info.ReplyToCommand($" {Config.Prefix} 玩家名 '{Player}' 已被CTBAN!");
            }
        }
        info.ReplyToCommand($" {Config.Prefix} 成功CTBAN {Player}");
        foreach (var find_player in Utilities.GetPlayers())
        {
            if (!Lib.IsPlayerValid(player)) continue;
            if (!Lib.IsPlayerValid(find_player)) continue;
            if (find_player.PlayerName.ToString() == Player)
            {
                find_player.PrintToChat($" {Config.Prefix} 你已被禁止加入 {ChatColors.LightBlue}CT{ChatColors.Default} -来自管理员 {ChatColors.Red}{player.PlayerName}{ChatColors.Default} -理由: {ChatColors.Gold}{Reason} ");
                Showinfo[find_player.Index] = 1;
                banned[find_player.Index] = true;
                reason[find_player.Index] = $"{Reason}";
                session[find_player.Index] = true;
                find_player.ChangeTeam(CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist);
            }
        }
    }
    [ConsoleCommand("css_ctban", "Ban player to CT")]
    public void addban(CCSPlayerController? player, CommandInfo info)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/ban"))
        {
            info.ReplyToCommand($" {Config.Prefix} 你没有权限使用此指令!");
            return;
        }
        var SteamID = info.ArgByIndex(1);
        var TimeHours = info.ArgByIndex(2);
        var Reason = info.GetArg(3);
        var Bannedby = "";
        if (player == null)
        {
            Bannedby = "CONSOLE";
        }
        else
        {
            Bannedby = player.SteamID.ToString();
        }

        foreach (var find_player in Utilities.GetPlayers())
        {
            if (find_player.PlayerName.ToString() == SteamID)
            {
                info.ReplyToCommand($" {Config.Prefix} 已被CTBAN!");
                SteamID = find_player.SteamID.ToString();
            }
            else
            {
                if (SteamID == null || !IsInt(SteamID))
                {
                    info.ReplyToCommand($" {Config.Prefix} 玩家名未找到");
                    return;
                }
            }
        }

        if (TimeHours == null || !IsInt(TimeHours))
        {
            info.ReplyToCommand($" {Config.Prefix} 时间单位必须是小时");
            return;
        }
        else if (Reason == null || IsInt(Reason))
        {
            info.ReplyToCommand($" {Config.Prefix} 理由不合法");
        }
        else
        {


            var TimeToUTC = DateTime.UtcNow.AddHours(Convert.ToInt32(TimeHours)).GetUnixEpoch();
            var BanTime = 0;
            if (TimeHours == "0")
            {
                BanTime = 0;
            }
            else
            {
                BanTime = DateTime.UtcNow.AddHours(Convert.ToInt32(TimeHours)).GetUnixEpoch();
            }


            var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(TimeToUTC) - DateTimeOffset.UtcNow;
            var timeRemainingFormatted = $"{timeRemaining.Days}d {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

            MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);

            MySqlQueryResult result = MySql!.Table("deadswim_ctbans").Where(MySqlQueryCondition.New("ban_steamid", "=", SteamID)).Select();
            if (result.Rows == 0)
            {
                MySqlQueryValue values = new MySqlQueryValue()
                .Add("ban_steamid", $"{SteamID}")
                .Add("end", $"{BanTime}")
                .Add("reason", $"{Reason}")
                .Add("banned_by", $"{Bannedby}");
                MySql.Table("deadswim_ctbans").Insert(values);

                info.ReplyToCommand($" {Config.Prefix} 成功封禁 {SteamID}");
                foreach (var find_player in Utilities.GetPlayers())
                {
                    if (!Lib.IsPlayerValid(find_player)) continue;
                    if (!Lib.IsPlayerValid(player)) continue;
                    if(find_player.SteamID.ToString() == SteamID)
                    {
                        find_player.PrintToChat($" {Config.Prefix} 你已被禁止加入 {ChatColors.LightBlue}CT{ChatColors.Default} -来自管理员 {ChatColors.Red}{player.PlayerName}{ChatColors.Default} -理由: {ChatColors.Gold}{Reason} ");
                        find_player.ChangeTeam(CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist);
                        Server.PrintToChatAll($" {find_player.PlayerName} {Config.Prefix} 已被禁止加入 {ChatColors.LightBlue}CT{ChatColors.Default} -来自管理员 {ChatColors.Red}{player.PlayerName}{ChatColors.Default} -理由: {ChatColors.Gold}{Reason} ");
                    }
                }
            }
            else
            {
                info.ReplyToCommand($" {Config.Prefix} 已经被ctban了!");
            }
        }
    }
    [ConsoleCommand("css_unctban", "UNBan player to CT")]
    public void UnbanCT(CCSPlayerController? player, CommandInfo info)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/ban"))
        {
            info.ReplyToCommand($" {Config.Prefix} 你无权使用此指令!");
            return;
        }
        var SteamID = info.ArgByIndex(1);
        if (SteamID == null || !IsInt(SteamID))
        {
            info.ReplyToCommand($" {Config.Prefix} Steamid不合法");
            return;
        }

        MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);

        MySqlQueryResult result = MySql!.Table("deadswim_ctbans").Where(MySqlQueryCondition.New("ban_steamid", "=", SteamID)).Select();
        if (result.Rows == 0)
        {
            info.ReplyToCommand($" {Config.Prefix} 这个steamid还没有被CTBAN!");
        }
        else
        {
            MySql.Table("deadswim_ctbans").Where($"ban_steamid = '{SteamID}'").Delete();
            info.ReplyToCommand($" {Config.Prefix} 成功解封.");
        }
    }
    [ConsoleCommand("css_isctbanned", "Info about CT Ban")]
    public void InfobanCT(CCSPlayerController? player, CommandInfo info)
    {
        if (!Lib.IsPlayerValid(player)) return;
        if (!AdminManager.PlayerHasPermissions(player, "@css/ban"))
        {
            info.ReplyToCommand($" {Config.Prefix} 无权使用!");
            return;
        }
        var SteamID = info.ArgByIndex(1);
        if (SteamID == null || !IsInt(SteamID))
        {
            info.ReplyToCommand($" {Config.Prefix} Steamid必须是数字");
            return;
        }

        MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);

        MySqlQueryResult result = MySql!.Table("deadswim_ctbans").Where(MySqlQueryCondition.New("ban_steamid", "=", SteamID)).Select();
        if (result.Rows == 0)
        {
            info.ReplyToCommand($" {Config.Prefix} 这人没被CTBAN!");
        }
        else
        {
            var time = result.Get<int>(0, "end");
            string reason = result.Get<string>(0, "reason");

            var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(time) - DateTimeOffset.UtcNow;
            var nowtimeis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeRemainingFormatted =
            $"{timeRemaining.Days}d {timeRemaining.Hours}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            player.PrintToChat($" {ChatColors.Red}|-------------| {ChatColors.Default}封禁信息 {SteamID} {ChatColors.Red}|-------------|");
            player.PrintToChat($" {ChatColors.Default}SteamID {ChatColors.Red}{SteamID}{ChatColors.Default} 处于 {ChatColors.Red}封禁状态.");
            player.PrintToChat($" {ChatColors.Default}理由 {ChatColors.Red}{reason}{ChatColors.Default}.");
            player.PrintToChat($" {ChatColors.Default}时间 {ChatColors.Red}{timeRemainingFormatted}{ChatColors.Default}.");
            player.PrintToChat($" {ChatColors.Red}|-------------| {ChatColors.Default}封禁信息 {SteamID} {ChatColors.Red}|-------------|");
        }
    }
}
