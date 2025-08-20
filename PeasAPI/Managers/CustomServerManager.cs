using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using AmongUs.Data.Player;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace PeasAPI.Managers
{
    public class CustomServerManager
    {
        public static List<StaticHttpRegionInfo> CustomServer = new();
        
        /// <summary>
        /// Adds a custom region to the game
        /// </summary>
        public static void RegisterServer(string name, string ip, ushort port)
        {
            CustomServer.Add(new StaticHttpRegionInfo(name, StringNames.NoTranslation, ip,
                new[] { new ServerInfo(name + "-1", ip, port, false) }));
        }

        //Skidded from https://github.com/edqx/Edward.SkipAuth
        [HarmonyPatch(typeof(AuthManager._CoConnect_d__4), nameof(AuthManager._CoConnect_d__4.MoveNext))]
        public static class DoNothingInConnect
        {
            public static bool Prefix(AuthManager __instance)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(AuthManager._CoWaitForNonce_d__6), nameof(AuthManager._CoWaitForNonce_d__6.MoveNext))]
        public static class DontWaitForNonce
        {
            public static bool Prefix(AuthManager __instance)
            {
                return false;
            }
        }
        
        [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.Awake))]
        class ServerManagerAwakePatch
        {
            public static void Postfix(ServerManager __instance)
            {
                var defaultRegions = new List<IRegionInfo>();
                foreach (var server in CustomServer)
                {
                    defaultRegions.Add(server.Cast<IRegionInfo>());
                }
                ServerManager.DefaultRegions = defaultRegions.ToArray();
                __instance.AvailableRegions = defaultRegions.ToArray();
            }
        }
        
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        public static class MainMenuManagerStartPatch
        {
            private static bool _initialized;

            public static void Postfix(MainMenuManager __instance)
            {
                if (!_initialized && CustomServer.Count != 0 && ServerManager.Instance.CurrentRegion.Name != CustomServer[0].Name)
                {
                    ServerManager.Instance.SetRegion(CustomServer[0].Cast<IRegionInfo>());
                }
                _initialized = true;
            }
        }
        
        [HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.IsBanned), MethodType.Getter)]
        public static class IsBannedPatch
        {
            public static void Postfix(out bool __result)
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(ServerDropdown), nameof(ServerDropdown.FillServerOptions))]
        public static class ServerDropdownPatch
        {
            public static bool Prefix(ServerDropdown __instance)
            {
                var num = 0;
                __instance.background.size = new Vector2(8.4f, 4.8f);

                foreach (var regionInfo in DestroyableSingleton<ServerManager>.Instance.AvailableRegions)
                {
                    if (DestroyableSingleton<ServerManager>.Instance.CurrentRegion.Equals(regionInfo))
                    {
                        __instance.defaultButtonSelected = __instance.firstOption;
                        __instance.firstOption.ChangeButtonText(
                            DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(
                                regionInfo.TranslateName,
                                regionInfo.Name));
                    }
                    else
                    {
                        var region = regionInfo;
                        var serverListButton = __instance.ButtonPool.Get<ServerListButton>();
                        var x = num % 2 == 0 ? -2 : 2;
                        var y = -0.55f * (num / 2);
                        serverListButton.transform.localPosition = new Vector3(x, __instance.y_posButton + y, -1f);
                        serverListButton.transform.localScale = Vector3.one;
                        serverListButton.Text.text =
                            DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(
                                regionInfo.TranslateName,
                                regionInfo.Name);
                        serverListButton.Text.ForceMeshUpdate();
                        serverListButton.Button.OnClick.RemoveAllListeners();
                        serverListButton.Button.OnClick.AddListener((UnityAction)(() => { __instance.ChooseOption(region); }));
                        __instance.controllerSelectable.Add(serverListButton.Button);
                        __instance.background.transform.localPosition = new Vector3(
                            0f,
                            __instance.initialYPos + (-0.3f * (num / 2)),
                            0f);
                        __instance.background.size = new Vector2(__instance.background.size.x, 1.2f + (0.6f * (num / 2)));
                        num++;
                    }
                }

                return false;
            }
        }
    }
}