using System;
using System.Collections.Generic;
using System.Linq;
using PeasAPI.Roles;

namespace PeasAPI.Options;

public class CustomRoleOption : CustomOption
{
    internal CustomNumberOption chanceOption;
    internal CustomNumberOption countOption;
    internal CustomOption[] AdvancedOptions;

    public CustomRoleOption(BaseRole baseRole, string prefix, CustomOption[] advancedOptions, MultiMenu menu = MultiMenu.NULL) : base(num++,
        menu == MultiMenu.NULL ? GetMultiMenu(baseRole) : menu,
        Utility.ColorString(baseRole.Color, baseRole.Name), CustomOptionType.Header, 0, isRoleOption: true)
    {
        List<CustomOption> removedOptions = new List<CustomOption>();
        if (advancedOptions != null)
        {
            foreach (var option in advancedOptions)
            {
                if (option != null && CustomOption.AllOptions.Contains(option))
                {
                    removedOptions.Add(option);
                    CustomOption.AllOptions.Remove(option);
                }
            }
        }

        chanceOption = new CustomNumberOption(menu == MultiMenu.NULL ? GetMultiMenu(baseRole) : menu,
            "Role Chance", 0, 10, 0, 100, PercentFormat,
            CustomRoleOptionType.Chance, baseRole);
        countOption = new CustomNumberOption(menu == MultiMenu.NULL ? GetMultiMenu(baseRole) : menu,
            "Role Count", 1, 1, 1, 15, null,
            CustomRoleOptionType.Count, baseRole);

        foreach (var option in removedOptions)
        {
            CustomOption.AllOptions.Add(option);
        }

        AdvancedOptions = advancedOptions;
        if (advancedOptions != null)
        {
            foreach (var option in advancedOptions.Where(o => o != null))
            {
                option.Name = $"{prefix}{option.Name}";
            }
        }
    }

    public static Func<object, string> PercentFormat { get; } = value => $"{value:0}%";

    private static MultiMenu GetMultiMenu(BaseRole baseRole)
    {
        switch (baseRole.Team)
        {
            case Team.Role:
                return MultiMenu.Neutral;
            case Team.Alone:
                return MultiMenu.Neutral;
            case Team.Crewmate:
                return MultiMenu.Crewmate;
            case Team.Impostor:
                return MultiMenu.Impostor;
            default:
                return MultiMenu.Main;
        }
    }

    public int GetChance()
    {
        return (int)chanceOption.Value;
    }

    public int GetCount()
    {
        return (int)countOption.Value;
    }
}