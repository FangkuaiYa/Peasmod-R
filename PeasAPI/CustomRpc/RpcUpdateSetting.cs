using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using PeasAPI.Options;
using UnityEngine;

namespace PeasAPI.CustomRpc
{
    public static class RpcUpdateSetting
    {
        public static IEnumerator SendRpc(CustomOption optionn = null, int RecipientId = -1)
        {
            yield return new WaitForSecondsRealtime(0.5f);

            List<CustomOption> options;
            if (optionn != null)
                options = new List<CustomOption> { optionn };
            else
                options = CustomOption.AllOptions;

            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRpcCalls.UpdateSetting, SendOption.Reliable, RecipientId);

            foreach (var option in options)
            {
                if (option.Type == CustomOptionType.Header) continue;

                if (writer.Position > 1000)
                {
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRpcCalls.UpdateSetting, SendOption.Reliable, RecipientId);
                }

                writer.WritePacked(option.ID);

                switch (option.Type)
                {
                    case CustomOptionType.Toggle:
                        writer.Write((bool)option.ValueObject);
                        break;
                    case CustomOptionType.Number:
                    {
                        switch (option.CustomRoleOptionType)
                        {
                            case CustomRoleOptionType.None:
                                switch ((option as CustomNumberOption).IntSafe)
                                {
                                    case true:
                                        writer.WritePacked((int)(float)option.ValueObject);
                                        break;
                                    case false:
                                        writer.Write((float)option.ValueObject);
                                        break;
                                }

                                break;
                            case CustomRoleOptionType.Chance:
                                writer.Write(Convert.ToInt32(option.ValueObject));
                                option.BaseRole.Chance = Convert.ToInt32(option.ValueObject);
                                break;
                            case CustomRoleOptionType.Count:
                                writer.Write(Convert.ToInt32(option.ValueObject));
                                option.BaseRole.Count =
                                    option.BaseRole.MaxCount = Convert.ToInt32(option.ValueObject);
                                break;
                        }
                    }
                        break;
                    case CustomOptionType.String:
                        writer.WritePacked((int)option.ValueObject);
                        break;
                }
            }

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRpc(MessageReader reader, bool AllOptions)
        {
            PeasAPI.Logger.LogInfo(
                $"Options received - {reader.BytesRemaining} bytes");
            while (reader.BytesRemaining > 0)
            {
                var id = reader.ReadPackedInt32();
                var customOption =
                    CustomOption.AllOptions.FirstOrDefault(option =>
                        option.ID == id); // Works but may need to change to gameObject.name check
                var type = customOption?.Type;
                object value = null;

                switch (type)
                {
                    case CustomOptionType.Toggle:
                        value = reader.ReadBoolean();
                        break;
                    case CustomOptionType.Number:
                        switch ((customOption as CustomNumberOption).IntSafe)
                        {
                            case true:
                                value = (float)reader.ReadPackedInt32();
                                break;
                            case false:
                                value = reader.ReadSingle();
                                break;
                        }

                        break;
                    case CustomOptionType.String:
                        value = reader.ReadPackedInt32();
                        break;
                }

                customOption?.Set(value, Notify: !AllOptions);

                if (LobbyInfoPane.Instance.LobbyViewSettingsPane.gameObject.activeSelf)
                {
                    var panels = GameObject.FindObjectsOfType<ViewSettingsInfoPanel>();
                    foreach (var panel in panels)
                        if (panel.titleText.text == customOption.GetName() &&
                            customOption.Type != CustomOptionType.Header)
                            panel.settingText.text = customOption.ToString();
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class HandleRpc
        {
            private static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
            {
                switch (callId)
                {
                    case (byte)CustomRpcCalls.UpdateSetting:
                        ReceiveRpc(reader, reader.BytesRemaining > 8);
                        break;
                }
            }
        }
    }
}