﻿using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using PeasAPI.CustomButtons;
using PeasAPI.CustomRpc;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace PeasAPI.Roles
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        [HarmonyPrefix]
        public static void OnGameEndPatch(AmongUsClient __instance)
        {
            RoleManager.Roles.Do(r => r.OnGameStop());
        }
        
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
        [HarmonyPrefix]
        public static void ResetRolePatch(AmongUsClient __instance)
        {
            PlayerControl.AllPlayerControls.ToArray().Where(player => player != null).Do(player => player.SetCustomRole(null));
        }

        [HarmonyPatch(typeof(global::RoleManager), nameof(global::RoleManager.SelectRoles))]
        [HarmonyPostfix]
        public static void InitializeRolesPatch()
        {
            Rpc<RpcInitializeRoles>.Instance.Send();
        }

        [HarmonyPatch(typeof(global::LogicRoleSelectionNormal), nameof(global::LogicRoleSelectionNormal.AssignRolesFromList))]
        [HarmonyPrefix]
        public static bool ChangeImpostors(global::LogicRoleSelectionNormal __instance,
            [HarmonyArgument(0)] List<NetworkedPlayerInfo> players, [HarmonyArgument(1)] int teamMax,
            [HarmonyArgument(2)] List<RoleTypes> roleList, [HarmonyArgument(3)] ref int rolesAssigned)
        {
            while (roleList.Count > 0 && players.Count > 0 && rolesAssigned < teamMax)
            {
                int index = HashRandom.FastNext(roleList.Count);
                RoleTypes roleType = roleList.ToArray()[index];
                roleList.RemoveAt(index);
                int index2 = global::RoleManager.IsImpostorRole(roleType) && RoleManager.HostMod.IsImpostor
                    ? 0
                    : HashRandom.FastNext(players.Count);
                players.ToArray()[index2].Object.RpcSetRole(roleType);
                players.RemoveAt(index2);
                rolesAssigned++;
            }

            return false;
        }

        [HarmonyPatch(typeof(IntroCutscene._ShowRole_d__41), nameof(IntroCutscene._ShowRole_d__41.MoveNext))]
        [HarmonyPostfix]
        public static void RoleTextPatch(IntroCutscene._ShowRole_d__41 __instance)
        {
            if (PlayerControl.LocalPlayer.GetCustomRole() != null)
            {
                var role = PlayerControl.LocalPlayer.GetCustomRole();
                var scene = __instance.__4__this;

                scene.RoleText.text = role.Name;
                scene.RoleBlurbText.text = role.Description;
                scene.RoleText.color = role.Color;
                scene.RoleBlurbText.color = role.Color;
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        [HarmonyPostfix]
        public static void TeamTextPatch(IntroCutscene __instance)
        {
            if (PlayerControl.LocalPlayer.GetCustomRole() != null)
            {
                var role = PlayerControl.LocalPlayer.GetCustomRole();
                var scene = __instance;

                scene.TeamTitle.text = role.Name;
                scene.ImpostorText.gameObject.SetActive(true);
                scene.ImpostorText.text = role.Description;
                scene.BackgroundBar.material.color = role.Color;
                scene.TeamTitle.color = role.Color;
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        [HarmonyPrefix]
        public static void RoleTeamPatch(IntroCutscene __instance,
            [HarmonyArgument(0)] ref List<PlayerControl> yourTeam)
        {
            if (PlayerControl.LocalPlayer.GetCustomRole() != null)
            {
                var role = PlayerControl.LocalPlayer.GetCustomRole();
                if (role.Team == Team.Alone)
                {
                    yourTeam = new List<PlayerControl>();
                    yourTeam.Add(PlayerControl.LocalPlayer);
                }
                else if (role.Team == Team.Role)
                {
                    yourTeam = new List<PlayerControl>();
                    yourTeam.Add(PlayerControl.LocalPlayer);
                    foreach (var player in role.Members)
                    {
                        if (player != PlayerControl.LocalPlayer.PlayerId)
                            yourTeam.Add(player.GetPlayer());
                    }
                }
                else if (role.Team == Team.Impostor)
                {
                    yourTeam = new List<PlayerControl>();
                    yourTeam.Add(PlayerControl.LocalPlayer);
                    foreach (var player in role.Members)
                    {
                        if (player != PlayerControl.LocalPlayer.PlayerId &&
                            player.GetPlayer().Data.Role.IsImpostor)
                            yourTeam.Add(player.GetPlayer());
                    }
                }
            }
        }

        /*[HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), typeof(StringNames),
            typeof(Il2CppReferenceArray<Object>))]
        public class TranslationControllerPatch
        {
            public static bool Prefix(ref string __result, [HarmonyArgument(0)] StringNames name)
            {
                if (ExileController.Instance != null && ExileController.Instance.exiled != null && (name == StringNames.ExileTextPN || name == StringNames.ExileTextSN))
                {
                    var role = ExileController.Instance.exiled.Object.GetRole();
                    if (role != null)
                    {
                        var article = role.Members.Count > 1 ? "a" : "the";
                        __result = $"{ExileController.Instance.exiled.PlayerName} was {article} {role.Name}.";
                        return false;
                    }
                }

                return true;
            }
        }*/
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.BeginForGameplay))]
        [HarmonyPostfix]
        public static void ChangeExileTextPatch(ExileController __instance, [HarmonyArgument(0)] NetworkedPlayerInfo player, [HarmonyArgument(1)] bool voteTie)
        {
            if (voteTie || player == null)
                return;
            
            var role = player.Object.GetCustomRole();
            if (role != null)
            {
                var article = role.Members.Count > 1 ? "a" : "the";
                __instance.completeString = $"{ExileController.Instance.initData.networkedPlayer.PlayerName} was {article} {role.Name}.";
            }
        } 

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class HudManagerUpdatePatch
        {
            public static void Prefix(HudManager __instance)
            {
                if (PeasAPI.GameStarted)
                {
                    RoleManager.Roles.Do(r => r._OnUpdate());
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        public static class MeetingUpdatePatch
        {
            public static void Postfix(MeetingHud __instance)
            {
                RoleManager.Roles.Do(r => r._OnMeetingUpdate(__instance));
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class PlayerControlFixedUpdatePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (PeasAPI.GameStarted)
                {
                    var localRole = PlayerControl.LocalPlayer.GetCustomRole();

                    if (localRole != null && __instance.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        HudManager.Instance.KillButton.gameObject.SetActive(!PlayerControl.LocalPlayer.Data.IsDead &&
                                                                            localRole.CanKill(null) && CustomButton.HudActive);

                        if (localRole.CanKill(null) && __instance.CanMove && !__instance.Data.IsDead)
                        {
                            if (!__instance.Data.Role.IsImpostor)
                                __instance.SetKillTimer(__instance.killTimer - Time.fixedDeltaTime);
                            PlayerControl target = __instance.Data.Role.FindClosestTarget();
                            HudManager.Instance.KillButton.SetTarget(target);
                        }
                        else
                        {
                            HudManager.Instance.KillButton.SetTarget(null);
                            HudManager.Instance.KillButton.SetDisabled();
                        }

                        HudManager.Instance.SabotageButton.gameObject.SetActive(
                            !PlayerControl.LocalPlayer.Data.IsDead && localRole.CanSabotage(null) && CustomButton.HudActive);

                        if (localRole.CanSabotage(null) && __instance.CanMove && !__instance.Data.IsDead)
                        {
                            HudManager.Instance.SabotageButton.SetEnabled();
                        }
                        else
                        {
                            HudManager.Instance.SabotageButton.SetDisabled();
                        }

                        HudManager.Instance.ImpostorVentButton.gameObject.SetActive(
                            !PlayerControl.LocalPlayer.Data.IsDead && localRole.CanVent && CustomButton.HudActive);

                        if (localRole.CanVent && __instance.CanMove && !__instance.Data.IsDead)
                        {
                            HudManager.Instance.ImpostorVentButton.SetEnabled();
                        }
                        else
                        {
                            HudManager.Instance.ImpostorVentButton.SetDisabled();
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
        [HarmonyPrefix]
        public static bool RemoveCheckMurder(KillButton __instance)
        {
            var target = __instance.currentTarget;
            var killer = PlayerControl.LocalPlayer;
            if (__instance.isActiveAndEnabled && target && !__instance.isCoolingDown && !killer.Data.IsDead && killer.CanMove)
            {
                if (AmongUsClient.Instance.IsGameOver)
                {
                    return false;
                }
                if (!target || killer.Data.IsDead || killer.Data.Disconnected)
                {
                    int num = target ? target.PlayerId : -1;
                    Debug.LogWarning(string.Format("Bad kill from {0} to {1}", killer.PlayerId, num));
                    return false;
                }
                NetworkedPlayerInfo data = target.Data;
                if (data == null || data.IsDead || target.inVent)
                {
                    Debug.LogWarning("Invalid target data for kill");
                    return false;
                }
                PlayerControl.LocalPlayer.RpcMurderPlayer(__instance.currentTarget, true);
                __instance.SetTarget(null);
            }
            return false;
        }

        [HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
        public static class KillButtonManagerSetTargetPatch
        {
            public static bool Prefix(KillButton __instance, [HarmonyArgument(0)] PlayerControl target)
            {
                if (!PlayerControl.LocalPlayer || PlayerControl.LocalPlayer.Data == null || !PlayerControl.LocalPlayer.Data.Role)
                    return false;
                RoleTeamTypes teamType = PlayerControl.LocalPlayer.GetCustomRole() == null ? PlayerControl.LocalPlayer.Data.Role.TeamType : PlayerControl.LocalPlayer.GetCustomRole().CanKill() ? RoleTeamTypes.Impostor : RoleTeamTypes.Crewmate;
                if (__instance.currentTarget && __instance.currentTarget != target)
                {
                    __instance.currentTarget.ToggleHighlight(false, teamType);
                }
                __instance.currentTarget = target;
                if (__instance.currentTarget)
                {
                    __instance.currentTarget.ToggleHighlight(true, teamType);
                    __instance.SetEnabled();
                    return false;
                }
                __instance.SetDisabled();
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
        public static class PlayerControlSetKillTimerPatch
        {
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] float time)
            {
                if (__instance.GetCustomRole() != null && __instance.GetCustomRole().CanKill() || __instance.Data.Role.CanUseKillButton)
                {
                    if (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown <= 0f)
                        return false;
                    __instance.killTimer = Mathf.Clamp(time, 0f, GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
                    DestroyableSingleton<HudManager>.Instance.KillButton.SetCoolDown(__instance.killTimer, GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
                }
                return false;
            }
        }
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class PlayerControlMurderPlayerPatch
        {
            public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
            {
                RoleManager.Roles.Do(r => r.OnKill(__instance, target));
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
        [HarmonyPostfix]
        public static void OnPlayerExiledPatch(PlayerControl __instance)
        {
            RoleManager.Roles.Do(r => r.OnExiled(__instance));
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
        [HarmonyPrefix]
        public static void OnMeetingStart(MeetingHud __instance)
        {
            RoleManager.Roles.Do(r => r.OnMeetingStart(__instance));
        }
        
        [HarmonyPatch(typeof(PlayerControl._CoSetTasks_d__103), nameof(PlayerControl._CoSetTasks_d__103.MoveNext))]
        public static class PlayerControlSetTasks
        {
            public static void Postfix(PlayerControl._CoSetTasks_d__103 __instance)
            {
                if (__instance == null)
                    return;

                var player = __instance.__4__this;
                var role = player.GetCustomRole();

                if (role == null)
                    return;

                if (player.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    return;

                if (!role.AssignTasks)
                    player.ClearTasks();

                if (role.TaskText == null)
                    return;

                if (!player.Data.Role.IsImpostor && !role.HasToDoTasks && role.AssignTasks)
                {
                    var fakeTasks = new GameObject("FakeTasks").AddComponent<ImportantTextTask>();
                    fakeTasks.transform.SetParent(player.transform, false);
                    fakeTasks.Text = $"</color>{role.Color.GetTextColor()}Fake Tasks:</color>";
                    player.myTasks.Insert(0, fakeTasks);
                }
                
                var roleTask = new GameObject(role.Name + "Task").AddComponent<ImportantTextTask>();
                roleTask.transform.SetParent(player.transform, false);
                roleTask.Text = $"</color>Role: {role.Color.GetTextColor()}{role.Name}\n{role.TaskText}</color>";
                player.myTasks.Insert(0, roleTask);
            }
        }

        [HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
        public static class UseButtonManagerDoClickPatch
        {
            public static bool Prefix(SabotageButton __instance)
            {
                if (__instance.isActiveAndEnabled && PeasAPI.GameStarted)
                {
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    if (role == null)
                        return true;
            
                    var mapOptions = new MapOptions
                    {
                        Mode = MapOptions.Modes.Sabotage
                    };
            
                    MapBehaviour.Instance.Show(mapOptions);
            
                    foreach (MapRoom mapRoom in MapBehaviour.Instance.infectedOverlay.rooms.ToArray())
                    {
                        mapRoom.gameObject.SetActive(role.CanSabotage(mapRoom.room));
                    }
            
                    return false;
                }
        
                return true;
            }
        }
        
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
        public static class MapBehaviourShowSabotageMapPatch
        {
            public static bool Prefix(MapBehaviour __instance)
            {
                if (PeasAPI.GameStarted)
                {
                    var role = PlayerControl.LocalPlayer.GetCustomRole();

                    if (role == null)
                        return true;
                    
                    foreach (MapRoom mapRoom in __instance.infectedOverlay.rooms.ToArray())
                    {
                        mapRoom.gameObject.SetActive(role.CanSabotage(mapRoom.room));
                    }
                    
                    //return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
        [HarmonyPriority(Priority.First)]
        public static class VentCanUsePatch
        {
            public static void Postfix(Vent __instance, [HarmonyArgument(1)] ref bool canUse,
                [HarmonyArgument(2)] ref bool couldUse, ref float __result)
            {
                BaseRole role = PlayerControl.LocalPlayer.GetCustomRole();

                if (role == null)
                    return;

                couldUse = canUse = role.CanVent;
                __result = float.MaxValue;

                if (canUse)
                {
                    Vector3 center = PlayerControl.LocalPlayer.Collider.bounds.center;
                    Vector3 position = __instance.transform.position;

                    __result = Vector2.Distance(center, position);
                    canUse &= (__result <= __instance.UsableDistance &&
                               !PhysicsHelpers.AnythingBetween(PlayerControl.LocalPlayer.Collider, center, position,
                                   Constants.ShipOnlyMask, false));
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
        [HarmonyPrefix]
        private static bool ShouldGameEndPatch(GameManager __instance, [HarmonyArgument(0)] GameOverReason endReason)
        {
            return RoleManager.Roles.Count(r => r.Members.Count != 0 && !r.ShouldGameEnd(endReason)) == 0;
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
        [HarmonyPrefix]
        private static void OnTaskCompletePatch(PlayerControl __instance, [HarmonyArgument(0)] uint idx)
        {
            PlayerTask playerTask = __instance.myTasks.ToArray().ToList().Find(p => p.Id == idx);
            RoleManager.Roles.Do(r => r.OnTaskComplete(__instance, playerTask));
        }
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Revive))]
        [HarmonyPrefix]
        private static void OnRevivePatch(PlayerControl __instance)
        {
            RoleManager.Roles.Do(r => r.OnRevive(__instance));
        }
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
        [HarmonyPrefix]
        private static bool PreExiledPatch(PlayerControl __instance)
        {
            return RoleManager.Roles.Count(r => r.Members.Count != 0 && !r.PreExile(__instance)) == 0;
        }
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        [HarmonyPrefix]
        private static bool PreKillPatch(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            return RoleManager.Roles.Count(r => r.Members.Count != 0 && !r.PreKill(__instance, target)) == 0;
        }
        
        [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
        [HarmonyPrefix]
        private static bool DoTasksCountPatch(GameData __instance)
        {

            __instance.TotalTasks = 0;
            __instance.CompletedTasks = 0;
            foreach (var playerInfo in __instance.AllPlayers)
            {
                if (!playerInfo.Disconnected && playerInfo.Tasks != null && playerInfo.Object && (GameOptionsManager.Instance.currentNormalGameOptions.GhostsDoTasks || !playerInfo.IsDead) && playerInfo.Role && playerInfo.Role.TasksCountTowardProgress && (playerInfo.GetCustomRole() == null || playerInfo.GetCustomRole().HasToDoTasks))
                {
                    foreach (var task in playerInfo.Tasks)
                    {
                        __instance.TotalTasks++;
                        if (task.Complete)
                        {
                            __instance.CompletedTasks++;
                        }
                    }
                }
            }
            /*for (int i = 0; i < __instance.AllPlayers.Count; i++)
            {
                NetworkedPlayerInfo playerInfo = __instance.AllPlayers[i];
                if (!playerInfo.Disconnected && playerInfo.Tasks != null && playerInfo.Object && (GameOptionsManager.Instance.currentNormalGameOptions.GhostsDoTasks || !playerInfo.IsDead) && playerInfo.Role && playerInfo.Role.TasksCountTowardProgress)
                {
                    for (int j = 0; j < playerInfo.Tasks.Count; j++)
                    {
                        __instance.TotalTasks++;
                        if (playerInfo.Tasks.ToArray()[j].Complete)
                        {
                            __instance.CompletedTasks++;
                        }
                    }
                }
            }*/

            return false;
        }
    }
}