﻿using System.Collections.Generic;
using BepInEx.Unity.IL2CPP;
using PeasAPI;
using PeasAPI.Components;
using PeasAPI.CustomButtons;
using PeasAPI.Options;
using PeasAPI.Roles;
using UnityEngine;

namespace Peasmod.Roles.Impostor;

[RegisterCustomRole]
public class EvilForensic : BaseRole
{
    public EvilForensic(BasePlugin plugin) : base(plugin)
    {
    }

    public override string Name => "Evil Forensic";
    public override string Description => "Poison the other crewmates";
    public override string LongDescription => "Inject a poison into the blood of the other crewmates which will kill them after a short time";
    public override string TaskText => "Poison the other crewmates";
    public override Color Color => Palette.ImpostorRed;
    public override Visibility Visibility => Visibility.Impostor;
    public override Team Team => Team.Impostor;
    public override bool HasToDoTasks => false;
    public override int MaxCount => 3;
    public override Dictionary<string, CustomOption> AdvancedOptions { get; set; } = new Dictionary<string, CustomOption>()
    {
        {
            "PoisonCooldown", new CustomNumberOption(MultiMenu.Impostor, "Poison-Cooldown", 30, 180, 1, 30, CustomOption.Seconds)
        },
        {
            "PoisonDuration", new CustomNumberOption(MultiMenu.Impostor, "Poison-Duration", 20, 60, 1, 20, CustomOption.Seconds)
        }
    };
    public override bool CanVent => true;
    public override bool CanKill(PlayerControl victim = null) => !victim || !victim.Data.Role.IsImpostor;
    public override bool CanSabotage(SystemTypes? sabotage) => true;

    public CustomButton Button;
    public byte PoisonVictim;
    
    public override void OnGameStart()
    {
        PoisonVictim = byte.MaxValue;
        Button = CustomButton.AddButton(() =>
            {
                PoisonVictim = PlayerControl.LocalPlayer.Data.Role.FindClosestTarget().PlayerId;
            }, ((CustomNumberOption) AdvancedOptions["PoisonCooldown"]).Value, Utility.CreateSprite("Peasmod.Resources.Buttons.Default.png"),
            p => p.IsCustomRole(this) && !p.Data.IsDead, p => PlayerControl.LocalPlayer.Data.Role.FindClosestTarget() != null, text: "<size=40%>Poison", textOffset: new Vector2(0f, 0.5f),
            onEffectEnd: () =>
            {
                var target = PoisonVictim.GetPlayer();
                PoisonVictim = byte.MaxValue;
                target.RpcMurderPlayer(target, true);
            }, effectDuration: ((CustomNumberOption) AdvancedOptions["PoisonDuration"]).Value, target: CustomButton.TargetType.Player, targetColor: Color);
    }
}