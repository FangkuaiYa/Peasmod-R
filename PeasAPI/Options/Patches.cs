using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Text;
using PeasAPI.CustomRpc;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PeasAPI.Options;

public static class Patches
{
    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.CreateSettings))]
    private class MoreTasks
    {
        public static void Postfix(GameOptionsMenu __instance)
        {
            if (__instance.gameObject.name == "GAME SETTINGS TAB")
                try
                {
                    var commonTasks = __instance.Children.ToArray().FirstOrDefault(x =>
                            x.TryCast<NumberOption>()?.intOptionName == Int32OptionNames.NumCommonTasks)
                        .Cast<NumberOption>();
                    if (commonTasks != null) commonTasks.ValidRange = new FloatRange(0f, 4f);

                    var shortTasks = __instance.Children.ToArray()
                        .FirstOrDefault(x => x.TryCast<NumberOption>()?.intOptionName == Int32OptionNames.NumShortTasks)
                        .Cast<NumberOption>();
                    if (shortTasks != null) shortTasks.ValidRange = new FloatRange(0f, 26f);

                    var longTasks = __instance.Children.ToArray()
                        .FirstOrDefault(x => x.TryCast<NumberOption>()?.intOptionName == Int32OptionNames.NumLongTasks)
                        .Cast<NumberOption>();
                    if (longTasks != null) longTasks.ValidRange = new FloatRange(0f, 15f);
                }
                catch
                {
                }
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
    private class ChangeTab
    {
        public static void Postfix(GameSettingMenu __instance, int tabNum, bool previewOnly)
        {
            if (previewOnly) return;
            foreach (var tab in SettingsUpdate.Tabs)
                if (tab != null)
                    tab.SetActive(false);
            foreach (var button in SettingsUpdate.Buttons) button.SelectButton(false);
            if (tabNum > 2)
            {
                tabNum -= 3;
                SettingsUpdate.Tabs[tabNum].SetActive(true);

                if (tabNum > 4) return;
                SettingsUpdate.Buttons[tabNum].SelectButton(true);

                __instance.StartCoroutine(Effects.Lerp(1f, new Action<float>(p =>
                {
                    foreach (var option in CustomOption.AllOptions)
                        if (option.Type == CustomOptionType.Number)
                        {
                            var number = option.Setting.Cast<NumberOption>();
                            number.TitleText.text = option.GetName();
                            if (number.TitleText.text.StartsWith("<color="))
                                number.TitleText.fontSize = 3f;
                            else if (number.TitleText.text.Length > 20)
                                number.TitleText.fontSize = 2.25f;
                            else if (number.TitleText.text.Length > 40)
                                number.TitleText.fontSize = 2f;
                            else number.TitleText.fontSize = 2.75f;
                        }

                        else if (option.Type == CustomOptionType.Toggle)
                        {
                            var tgl = option.Setting.Cast<ToggleOption>();
                            tgl.TitleText.text = option.GetName();
                            if (tgl.TitleText.text.Length > 20)
                                tgl.TitleText.fontSize = 2.25f;
                            else if (tgl.TitleText.text.Length > 40)
                                tgl.TitleText.fontSize = 2f;
                            else tgl.TitleText.fontSize = 2.75f;
                        }

                        else if (option.Type == CustomOptionType.String)
                        {
                            var playerCount = GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers;
                            var str = option.Setting.Cast<StringOption>();
                            str.TitleText.text = option.GetName();
                            if (str.TitleText.text.Length > 20)
                                str.TitleText.fontSize = 2.25f;
                            else if (str.TitleText.text.Length > 40)
                                str.TitleText.fontSize = 2f;
                            else str.TitleText.fontSize = 2.75f;
                        }
                })));
            }
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
    private class CloseSettings
    {
        public static void Prefix(GameSettingMenu __instance)
        {
            LobbyInfoPane.Instance.EditButton.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    internal class SettingsUpdate
    {
        public static List<PassiveButton> Buttons = new();
        public static List<GameObject> Tabs = new();

        public static void Postfix(GameSettingMenu __instance)
        {
            LobbyInfoPane.Instance.EditButton.gameObject.SetActive(false);
            Buttons.ForEach(x => x?.Destroy());
            Tabs.ForEach(x => x?.Destroy());
            Buttons = new List<PassiveButton>();
            Tabs = new List<GameObject>();

            if (GameOptionsManager.Instance.currentGameOptions.GameMode == AmongUs.GameOptions.GameModes.HideNSeek) return;

            GameObject.Find("What Is This?")?.Destroy();
            GameObject.Find("RoleSettingsButton")?.Destroy();
            GameObject.Find("GamePresetButton")?.Destroy();
            __instance.ChangeTab(1, false);

            var settingsButton = GameObject.Find("GameSettingsButton");
            settingsButton.transform.localPosition += new Vector3(0f, 2f, 0f);
            settingsButton.transform.localScale *= 0.9f;

            CreateSettings(__instance, 3, "ModSettings", "Mod Settings", settingsButton, MultiMenu.Main);
            CreateSettings(__instance, 4, "CrewSettings", "Crewmate Settings", settingsButton,
                MultiMenu.Crewmate);
            CreateSettings(__instance, 5, "NeutralSettings", "Neutral Settings", settingsButton,
                MultiMenu.Neutral);
            CreateSettings(__instance, 6, "ImpSettings", "Impostor Settings", settingsButton,
                MultiMenu.Impostor);
        }

        internal static TextMeshPro SpawnExternalButton(GameSettingMenu __instance, GameOptionsMenu tabOptions,
            ref float num, string text, Action onClick)
        {
            const float scaleX = 7f;
            var baseButton = __instance.GameSettingsTab.checkboxOrigin.transform.GetChild(1);
            var baseText = __instance.GameSettingsTab.checkboxOrigin.transform.GetChild(0);

            var exportButtonGO = GameObject.Instantiate(baseButton, Vector3.zero, Quaternion.identity,
                tabOptions.settingsContainer);
            exportButtonGO.name = text;
            exportButtonGO.transform.localPosition = new Vector3(1f, num, -2f);
            exportButtonGO.GetComponent<BoxCollider2D>().offset = Vector2.zero;
            exportButtonGO.name = text.Replace(" ", "");

            var prevColliderSize = exportButtonGO.GetComponent<BoxCollider2D>().size;
            prevColliderSize.x *= scaleX;
            exportButtonGO.GetComponent<BoxCollider2D>().size = prevColliderSize;

            exportButtonGO.transform.GetChild(2).gameObject.DestroyImmediate();
            var exportButton = exportButtonGO.GetComponent<PassiveButton>();
            exportButton.ClickMask = tabOptions.ButtonClickMask;
            exportButton.OnClick.RemoveAllListeners();
            exportButton.OnClick.AddListener(onClick);

            var exportButtonTextGO = GameObject.Instantiate(baseText, exportButtonGO);
            exportButtonTextGO.transform.localPosition = new Vector3(0, 0, -3f);
            exportButtonTextGO.GetComponent<RectTransform>().SetSize(prevColliderSize.x, prevColliderSize.y);
            var exportButtonText = exportButtonTextGO.GetComponent<TextMeshPro>();
            exportButtonText.alignment = TextAlignmentOptions.Center;
            exportButtonText.SetText(text);

            SpriteRenderer[] componentsInChildren = exportButtonGO.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].material.SetInt(PlayerMaterial.MaskLayer, 20);
                componentsInChildren[i].transform.localPosition = new Vector3(0, 0, -1);
                var prevSpriteSize = componentsInChildren[i].size;
                prevSpriteSize.x *= scaleX;
                componentsInChildren[i].size = prevSpriteSize;
            }

            TextMeshPro[] componentsInChildren2 = exportButtonGO.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var obj in componentsInChildren2)
            {
                obj.fontMaterial.SetFloat("_StencilComp", 3f);
                obj.fontMaterial.SetFloat("_Stencil", 20);
            }

            num -= 0.6f;
            return exportButtonText;
        }

        public static void CreateSettings(GameSettingMenu __instance, int target, string name, string text,
            GameObject settingsButton, MultiMenu menu)
        {
            var panel = GameObject.Find("LeftPanel");
            var button = GameObject.Find(name);
            if (button == null)
            {
                button = GameObject.Instantiate(settingsButton, panel.transform);
                button.transform.localPosition += new Vector3(0f, -0.55f * target + 1.1f, 0f);
                button.name = name;
                __instance.StartCoroutine(Effects.Lerp(1f,
                    new Action<float>(p =>
                    {
                        button.transform.FindChild("FontPlacer").GetComponentInChildren<TextMeshPro>().text =
                            text;
                    })));
                var passiveButton = button.GetComponent<PassiveButton>();
                passiveButton.OnClick.RemoveAllListeners();
                passiveButton.OnClick.AddListener((Action)(() => { __instance.ChangeTab(target, false); }));
                passiveButton.SelectButton(false);
                Buttons.Add(passiveButton);
            }

            var settingsTab = GameObject.Find("GAME SETTINGS TAB");
            Tabs.RemoveAll(x => x == null);
            var tab = GameObject.Instantiate(settingsTab, settingsTab.transform.parent);
            tab.name = name;
            var tabOptions = tab.GetComponent<GameOptionsMenu>();
            foreach (var child in tabOptions.Children) child.Destroy();
            tabOptions.scrollBar.transform.FindChild("SliderInner").DestroyChildren();
            tabOptions.Children.Clear();
            var options = CustomOption.AllOptions.Where(x => x.Menu == menu).ToList();

            if (target < 8)
            {
                var num = 1.5f;

                foreach (var option in options)
                {
                    if (option.Type == CustomOptionType.Header)
                    {
                        var header = Object.Instantiate(tabOptions.categoryHeaderOrigin, Vector3.zero,
                            Quaternion.identity, tabOptions.settingsContainer);
                        header.SetHeader(StringNames.ImpostorsCategory, 20);
                        header.Title.text = option.GetName();
                        header.transform.localScale = Vector3.one * 0.65f;
                        header.transform.localPosition = new Vector3(-0.9f, num, -2f);
                        num -= 0.625f;
                        continue;
                    }

                    if (option.Type == CustomOptionType.Number)
                    {
                        OptionBehaviour optionBehaviour = Object.Instantiate(tabOptions.numberOptionOrigin,
                            Vector3.zero, Quaternion.identity, tabOptions.settingsContainer);
                        optionBehaviour.transform.localPosition = new Vector3(0.95f, num, -2f);
                        optionBehaviour.SetClickMask(tabOptions.ButtonClickMask);
                        SpriteRenderer[] components = optionBehaviour.GetComponentsInChildren<SpriteRenderer>(true);
                        for (var i = 0; i < components.Length; i++)
                            components[i].material.SetInt(PlayerMaterial.MaskLayer, 20);

                        var numberOption = optionBehaviour as NumberOption;
                        option.Setting = numberOption;

                        tabOptions.Children.Add(optionBehaviour);
                    }

                    else if (option.Type == CustomOptionType.Toggle)
                    {
                        OptionBehaviour optionBehaviour = Object.Instantiate(tabOptions.checkboxOrigin, Vector3.zero,
                            Quaternion.identity, tabOptions.settingsContainer);
                        optionBehaviour.transform.localPosition = new Vector3(0.95f, num, -2f);
                        optionBehaviour.SetClickMask(tabOptions.ButtonClickMask);
                        SpriteRenderer[] components = optionBehaviour.GetComponentsInChildren<SpriteRenderer>(true);
                        for (var i = 0; i < components.Length; i++)
                            components[i].material.SetInt(PlayerMaterial.MaskLayer, 20);

                        var toggleOption = optionBehaviour as ToggleOption;
                        option.Setting = toggleOption;

                        tabOptions.Children.Add(optionBehaviour);
                    }

                    else if (option.Type == CustomOptionType.String)
                    {
                        var playerCount = GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers;

                        OptionBehaviour optionBehaviour = Object.Instantiate(tabOptions.stringOptionOrigin,
                            Vector3.zero, Quaternion.identity, tabOptions.settingsContainer);
                        optionBehaviour.transform.localPosition = new Vector3(0.95f, num, -2f);
                        optionBehaviour.SetClickMask(tabOptions.ButtonClickMask);
                        SpriteRenderer[] components = optionBehaviour.GetComponentsInChildren<SpriteRenderer>(true);
                        for (var i = 0; i < components.Length; i++)
                            components[i].material.SetInt(PlayerMaterial.MaskLayer, 20);

                        var stringOption = optionBehaviour as StringOption;
                        option.Setting = stringOption;

                        tabOptions.Children.Add(optionBehaviour);
                    }

                    num -= 0.45f;
                    tabOptions.scrollBar.SetYBoundsMax(-num - 1.65f);
                    option.OptionCreated();
                }
            }

            for (var i = 0; i < tabOptions.Children.Count; i++)
            {
                var optionBehaviour = tabOptions.Children[i];
                if (AmongUsClient.Instance && !AmongUsClient.Instance.AmHost) optionBehaviour.SetAsPlayer();
            }

            Tabs.Add(tab);
            tab.SetActive(false);
        }

        public static void ImportSlot(string preset)
        {
            System.Console.WriteLine(preset);

            string text;

            try
            {
                var path = Path.Combine(Application.persistentDataPath, $"{preset}.txt");
                text = File.ReadAllText(path);
            }
            catch
            {
                return;
            }

            var splitText = text.Split("\n").ToList();

            while (splitText.Count > 0)
            {
                var name = splitText[0].Trim();
                splitText.RemoveAt(0);
                var option =
                    CustomOption.AllOptions.FirstOrDefault(o => o.GetName().Equals(name, StringComparison.Ordinal));
                if (option == null)
                {
                    try
                    {
                        splitText.RemoveAt(0);
                    }
                    catch
                    {
                    }

                    continue;
                }

                var value = splitText[0];
                splitText.RemoveAt(0);
                switch (option.Type)
                {
                    case CustomOptionType.Number:
                        option.Set(float.Parse(value), false);
                        break;
                    case CustomOptionType.Toggle:
                        option.Set(bool.Parse(value), false);
                        break;
                    case CustomOptionType.String:
                        option.Set(int.Parse(value), false);
                        break;
                }
            }

            Coroutines.Start(RpcUpdateSetting.SendRpc());
        }

        public static void ExportSlot(string preset)
        {
            System.Console.WriteLine($"Exporting settings to {preset}");

            var builder = new StringBuilder();
            foreach (var option in CustomOption.AllOptions)
            {
                if (option.Type is CustomOptionType.Button or CustomOptionType.Header) continue;
                builder.AppendLine(option.GetName());
                builder.AppendLine($"{option.ValueObject}");
            }

            try
            {
                var path = Path.Combine(Application.persistentDataPath, $"{preset}.txt");
                File.WriteAllText(path, builder.ToString());
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.SetTab))]
    private class SetTabPane
    {
        public static bool Prefix(LobbyViewSettingsPane __instance)
        {
            if ((int)__instance.currentTab < 6)
            {
                ChangeTabPane.Postfix(__instance, __instance.currentTab);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.ChangeTab))]
    private class ChangeTabPane
    {
        public static void Postfix(LobbyViewSettingsPane __instance, StringNames category)
        {
            var tab = (int)category;

            foreach (var button in SettingsAwake.Buttons) button.SelectButton(false);
            if (tab > 5) return;
            __instance.taskTabButton.SelectButton(false);

            if (tab > 0)
            {
                tab -= 1;
                SettingsAwake.Buttons[tab].SelectButton(true);
                SettingsAwake.AddSettings(__instance, SettingsAwake.ButtonTypes[tab]);
            }
        }
    }

    [HarmonyPatch(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.Update))]
    private class UpdatePane
    {
        public static void Postfix(LobbyViewSettingsPane __instance)
        {
            if (SettingsAwake.Buttons.Count == 0) SettingsAwake.Postfix(__instance);
        }
    }

    [HarmonyPatch(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.Awake))]
    private class SettingsAwake
    {
        public static readonly List<PassiveButton> Buttons = new();
        public static readonly List<MultiMenu> ButtonTypes = new();

        public static void Postfix(LobbyViewSettingsPane __instance)
        {
            Buttons.ForEach(x => x?.Destroy());
            Buttons.Clear();
            ButtonTypes.Clear();

            if (GameOptionsManager.Instance.currentGameOptions.GameMode == AmongUs.GameOptions.GameModes.HideNSeek) return;

            GameObject.Find("RolesTabs")?.Destroy();
            var overview = GameObject.Find("OverviewTab");
            overview.transform.localScale += new Vector3(-0.35f, 0f, 0f);
            overview.transform.localPosition += new Vector3(-1f, 0f, 0f);
            overview.transform.GetChild(0).GetChild(0).transform.localScale += new Vector3(0.35f, 0f, 0f);
            overview.transform.GetChild(0).GetChild(0).transform.localPosition += new Vector3(-1f, 0f, 0f);

            CreateButton(__instance, 1, "ModTab", "Mod Settings", MultiMenu.Main, overview);
            CreateButton(__instance, 2, "CrewmateTab", "Crewmate Settings", MultiMenu.Crewmate, overview);
            CreateButton(__instance, 3, "NeutralTab", "Neutral Settings", MultiMenu.Neutral, overview);
            CreateButton(__instance, 4, "ImpostorTab", "Impostor Settings", MultiMenu.Impostor, overview);
        }

        public static void CreateButton(LobbyViewSettingsPane __instance, int target, string name, string text,
            MultiMenu menu, GameObject overview)
        {
            var tab = GameObject.Find(name);
            if (tab == null)
            {
                tab = GameObject.Instantiate(overview, overview.transform.parent);
                tab.transform.localPosition += new Vector3(2.5f, 0f, 0f) * target;
                tab.transform.GetChild(0).GetChild(0).transform.localPosition += new Vector3(-0.5f, 0f, 0f); 
                tab.name = name;
                __instance.StartCoroutine(Effects.Lerp(1f,
                    new Action<float>(p =>
                    {
                        tab.transform.FindChild("FontPlacer").GetComponentInChildren<TextMeshPro>().text =
                            text;
                    })));
                var pTab = tab.GetComponent<PassiveButton>();
                pTab.OnClick.RemoveAllListeners();
                pTab.OnClick.AddListener((Action)(() => { __instance.ChangeTab((StringNames)target); }));
                pTab.SelectButton(false);
                Buttons.Add(pTab);
                ButtonTypes.Add(menu);
            }
        }

        public static void AddSettings(LobbyViewSettingsPane __instance, MultiMenu menu)
        {
            var options = CustomOption.AllOptions.Where(x => x.Menu == menu).ToList();

            var num = 1.5f;
            var headingCount = 0;
            var settingsThisHeader = 0;
            var settingRowCount = 0;

            for (int j = 0; j < __instance.settingsInfo.Count; j++)
            {
                __instance.settingsInfo[j].gameObject.Destroy();
            }

            __instance.settingsInfo.Clear();

            foreach (var option in options)
                if (option.Type == CustomOptionType.Header)
                {
                    if (settingsThisHeader % 2 != 0) num -= 0.85f;
                    var header = Object.Instantiate(__instance.categoryHeaderOrigin);
                    header.SetHeader(StringNames.ImpostorsCategory, 61);
                    header.Title.text = option.GetName();
                    header.transform.SetParent(__instance.settingsContainer);
                    header.transform.localScale = Vector3.one;
                    header.transform.localPosition = new Vector3(-9.8f, num, -2f);
                    __instance.settingsInfo.Add(header.gameObject);
                    num -= 1f;
                    headingCount += 1;
                    settingsThisHeader = 0;
                }
                else
                {
                    var playerCount = GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers;
                    if (option.Name.StartsWith("Slot ")) continue;

                    var panel = Object.Instantiate(__instance.infoPanelOrigin);
                    panel.transform.SetParent(__instance.settingsContainer);
                    panel.transform.localScale = Vector3.one;
                    if (settingsThisHeader % 2 != 0)
                    {
                        panel.transform.localPosition = new Vector3(-3f, num, -2f);
                        num -= 0.85f;
                    }
                    else
                    {
                        settingRowCount += 1;
                        panel.transform.localPosition = new Vector3(-9f, num, -2f);
                    }

                    settingsThisHeader += 1;
                    panel.SetInfo(StringNames.ImpostorsCategory, option.ToString(), 61);
                    panel.titleText.text = option.GetName();
                    __instance.settingsInfo.Add(panel.gameObject);
                }

            float actual_spacing = (headingCount * 1.05f + settingRowCount * 0.85f) / (headingCount + settingRowCount) * 1.01f;
            __instance.scrollBar.CalculateAndSetYBounds(__instance.settingsInfo.Count + (headingCount + settingRowCount) * 2 + headingCount, 2f, 6f, actual_spacing);
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoSpawnPlayer))]
    private class PlayerJoinPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            if (PlayerControl.AllPlayerControls.Count < 2 || !AmongUsClient.Instance ||
                !PlayerControl.LocalPlayer || !AmongUsClient.Instance.AmHost) return;

            Coroutines.Start(RpcUpdateSetting.SendRpc(RecipientId: __instance.myPlayer.OwnerId));
        }
    }


    [HarmonyPatch(typeof(ToggleOption), nameof(ToggleOption.Toggle))]
    private class ToggleButtonPatch
    {
        public static bool Prefix(ToggleOption __instance)
        {
            var option =
                CustomOption.AllOptions.FirstOrDefault(option =>
                    option.Setting == __instance); // Works but may need to change to gameObject.name check
            if (option is CustomToggleOption toggle)
            {
                toggle.Toggle();
                return false;
            }

            if (GameOptionsManager.Instance.currentGameOptions.GameMode == AmongUs.GameOptions.GameModes.HideNSeek ||
                __instance.boolOptionName == BoolOptionNames.VisualTasks ||
                __instance.boolOptionName == BoolOptionNames.AnonymousVotes ||
                __instance.boolOptionName == BoolOptionNames.ConfirmImpostor) return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Initialize))]
    private class NumberOptionInitialise
    {
        public static bool Prefix(NumberOption __instance)
        {
            var option =
                CustomOption.AllOptions.FirstOrDefault(option =>
                    option.Setting == __instance);
            if (option is CustomNumberOption number)
            {
                __instance.MinusBtn.isInteractable = true;
                __instance.PlusBtn.isInteractable = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
    private class NumberOptionPatchIncrease
    {
        public static bool Prefix(NumberOption __instance)
        {
            var option =
                CustomOption.AllOptions.FirstOrDefault(option =>
                    option.Setting == __instance); // Works but may need to change to gameObject.name check
            if (option is CustomNumberOption number)
            {
                number.Increase();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
    private class NumberOptionPatchDecrease
    {
        public static bool Prefix(NumberOption __instance)
        {
            var option =
                CustomOption.AllOptions.FirstOrDefault(option =>
                    option.Setting == __instance); // Works but may need to change to gameObject.name check
            if (option is CustomNumberOption number)
            {
                number.Decrease();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    private class StringOptionPatchIncrease
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = CustomOption.AllOptions.FirstOrDefault(option => option.Setting == __instance);
            if (option is CustomStringOption str)
            {
                str.Increase();

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    private class StringOptionPatchDecrease
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = CustomOption.AllOptions.FirstOrDefault(option => option.Setting == __instance);
            if (option is CustomStringOption str)
            {
                str.Decrease();

                return false;
            }

            return true;
        }
    }
}