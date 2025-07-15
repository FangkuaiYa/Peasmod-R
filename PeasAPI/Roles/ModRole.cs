using Il2CppSystem.Text;
using PeasAPI;
using Reactor.Utilities.Attributes;

namespace PeasAPI.Roles;

[RegisterInIl2Cpp]
public class ModRole : RoleBehaviour
{
    public override bool IsDead => false;

    public void Update()
    {
        if (!PlayerControl.LocalPlayer)
            return;

        if (!PlayerControl.LocalPlayer.IsCustomRole())
            return;

        if (CanUseKillButton != PlayerControl.LocalPlayer.GetCustomRole().CanKill())
        {
            CanUseKillButton = !CanUseKillButton;
            HudManager.Instance.SetHudActive(true);
        }
    }

    public override bool CanUse(IUsable usable)
    {
        var role = PlayerControl.LocalPlayer.GetCustomRole();
        if (role != null && role.CanVent)
        {
            CanVent = role.CanVent;
            return usable.TryCast<Vent>() != null;
        }

        var console = usable.TryCast<Console>();

        if (!role.HasToDoTasks)
            return !(console != null) || console.AllowImpostor;

        return console != null;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        /*var customRole = PlayerControl.LocalPlayer.GetCustomRole();
        if (customRole != null)
            return customRole.DidWin(gameOverReason);*/
        PeasAPI.Logger.LogInfo(gameOverReason);
        return false;
    }

    public override void AppendTaskHint(StringBuilder taskStringBuilder)
    {
        if (BlurbMed.IsNullOrWhiteSpace())
            return;

        taskStringBuilder.AppendFormat("\n{0}{1} {2}</color>\n{3}", NameColor.ToTextColor(), NiceName,
            DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.RoleHint), BlurbMed);
    }

    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (!playerControl.IsLocal())
            return;

        var importantTask = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl);
        importantTask.Text = string.Concat(NameColor.ToTextColor(), Blurb, "\r",
            TasksCountTowardProgress
                ? ""
                : "\n" + DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FakeTasks),
            "</color>");
    }

    public override PlayerControl FindClosestTarget()
    {
        if (PlayerControl.LocalPlayer.IsCustomRole())
        {
            var playersInAbilityRangeSorted = GetPlayersInAbilityRangeSorted(GetTempPlayerList());
            if (playersInAbilityRangeSorted.Count <= 0) return null;
            return playersInAbilityRangeSorted.ToArray()[0];
        }

        return null;
    }
}