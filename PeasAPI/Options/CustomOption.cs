using System;
using System.Collections.Generic;
using PeasAPI.CustomRpc;
using PeasAPI.Roles;
using Reactor.Localization.Utilities;
using Reactor.Utilities;

namespace PeasAPI.Options;

public class CustomOption
{
    public static List<CustomOption> AllOptions = new();

    public static int num = 1;
    public readonly int ID;
    public readonly MultiMenu Menu;

    public BaseRole BaseRole;
    public CustomRoleOptionType CustomRoleOptionType;

    public Func<object, string> Format;
    public string Name;
    public bool IsRoleOption;

    public StringNames StringName;

    public CustomOption(int id, MultiMenu menu, string name, CustomOptionType type,
        object defaultValue,
        Func<object, string> format = null, CustomRoleOptionType customRoleOptionType = CustomRoleOptionType.None,
        BaseRole baseRole = null, bool isRoleOption = false)
    {
        ID = id;
        Menu = menu;
        Name = name;
        Type = type;
        DefaultValue = ValueObject = defaultValue;
        Format = format ?? (obj => $"{obj}");
        BaseRole = baseRole;
        CustomRoleOptionType = customRoleOptionType;

        if (Type == CustomOptionType.Button) return;
        AllOptions.Add(this);
        Set(ValueObject);

        StringName = CustomStringName.CreateAndRegister(
            customRoleOptionType == CustomRoleOptionType.Chance || customRoleOptionType == CustomRoleOptionType.Count
                ? Utility.ColorString(baseRole.Color, baseRole.Name) + $" {GetName()}"
                : GetName());

        IsRoleOption = isRoleOption;
    }

    public object ValueObject { get; set; }
    public OptionBehaviour Setting { get; set; }
    public CustomOptionType Type { get; set; }
    public object DefaultValue { get; set; }
    public static Func<object, string> Seconds { get; } = value => $"{value:0.0#}s";
    public static Func<object, string> Multiplier { get; } = value => $"{value:0.0#}x";

    public string GetName()
    {
        return Name;
    }

    public override string ToString()
    {
        return Format(ValueObject);
    }

    public virtual void OptionCreated()
    {
        Setting.name = Setting.gameObject.name = GetName();
    }

    public void Set(object value, bool SendRpc = true, bool Notify = false)
    {
        //PeasAPI.Logger.LogInfo($"{Name} set to {value}");

        ValueObject = value;

        if (Setting != null && AmongUsClient.Instance.AmHost && SendRpc)
            Coroutines.Start(RpcUpdateSetting.SendRpc(this));

        try
        {
            if (Setting is ToggleOption toggle)
            {
                var newValue = (bool)ValueObject;
                toggle.oldValue = newValue;
                if (toggle.CheckMark != null) toggle.CheckMark.enabled = newValue;
            }
            else if (Setting is NumberOption number)
            {
                var newValue = (float)ValueObject;

                number.Value = number.oldValue = newValue;
                number.ValueText.text = ToString();
            }
            else if (Setting is StringOption str)
            {
                var newValue = (int)ValueObject;

                str.Value = str.oldValue = newValue;
                str.ValueText.text = ToString();
            }
        }
        catch
        {
        }

        if (HudManager.InstanceExists && Type != CustomOptionType.Header && Notify)
            HudManager.Instance.Notifier.AddSettingsChangeMessage(StringName, ToString(),
                HudManager.Instance.Notifier.lastMessageKey != (int)StringName);
    }
}