using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime;
using InnerNet;
using Reactor;
using Reactor.Patches;
using TMPro;
using UnityEngine;
using static PeasAPI.Managers.WatermarkManager;

namespace PeasAPI.Managers
{
    public class WatermarkManager
    {
        public class Watermark
        {
            /// <summary>
            /// Text that gets added to the version text
            /// </summary>
            public string VersionText { get; set; }

            /// <summary>
            /// Text that gets added to the ping text
            /// </summary>
            public string PingText { get; set; }

            public Watermark(string versionText, string pingText)
            {
                VersionText = versionText;
                PingText = pingText;
            }
        }

        private static List<Watermark> Watermarks = new List<Watermark>();

        public static Watermark PeasApiWatermark = new Watermark($"<color=#ff0000ff>PeasAPI {PeasAPI.Version} <color=#ffffffff> by <color=#ff0000ff>Peasplayer", 
            "\n<color=#ff0000ff>PeasAPI");

        public static void AddWatermark(string versionText, string pingText)
        {
            var watermark = new Watermark(versionText, pingText);
            Watermarks.Add(watermark);
        }

        static bool haveStart = false;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        public static class MainMenuManagerStartPatch
        {
            static void Postfix(MainMenuManager __instance)
            {
                if (!haveStart)
                {
                    haveStart = true;

                    foreach (var watermark in Watermarks)
                    {
                        if (watermark.VersionText != null)
                        {
                            ReactorVersionShower.TextUpdated += text =>
                            {
                                text.text += "\n" + watermark.VersionText;
                            };
                        }
                    }

                    if (PeasAPI.ShamelessPlug)
                    {
                        if (PeasApiWatermark.VersionText != null)
                        {
                            ReactorVersionShower.TextUpdated += text =>
                            {
                                text.text += "\n" + PeasApiWatermark.VersionText;
                            };
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingTrackerUpdatePatch
        {
            public static void Postfix(PingTracker __instance)
            {
                var position = __instance.GetComponent<AspectPosition>();
                if (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
                {
                    __instance.text.alignment = TextAlignmentOptions.Top;
                    position.Alignment = AspectPosition.EdgeAlignments.Top;
                    position.DistanceFromEdge = new Vector3(1.5f, 0.11f, 0);
                }
                else
                {
                    position.Alignment = AspectPosition.EdgeAlignments.LeftTop;
                    __instance.text.alignment = TextAlignmentOptions.TopLeft;
                    position.DistanceFromEdge = new Vector3(0.5f, 0.11f);
                }
                foreach (var watermark in Watermarks)
                {                
                    if (watermark.PingText != null)
                        __instance.text.text += watermark.PingText;
                }

                if (PeasAPI.ShamelessPlug)
                {
                    if (PeasApiWatermark.PingText != null)
                        __instance.text.text += PeasApiWatermark.PingText;
                }
            }
        }
    }
}
