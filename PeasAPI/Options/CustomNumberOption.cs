using System;
using PeasAPI.Roles;
using UnityEngine;

namespace PeasAPI.Options;

public class CustomNumberOption : CustomOption
{
    public CustomNumberOption(MultiMenu multiMenu, string optionName, float value,
        float increment, float min, float max,
        Func<object, string> format = null, CustomRoleOptionType customRoleOptionType = CustomRoleOptionType.None,
        BaseRole baseRole = null)
        : base(num++, multiMenu, optionName, CustomOptionType.Number, value, format, customRoleOptionType,
            baseRole)
    {
        Min = min;
        Max = max;
        Increment = increment;
        IntSafe = Min % 1 == 0 && Max % 1 == 0 && Increment % 1 == 0;
    }

    protected float Min { get; set; }
    protected float Max { get; set; }
    protected float Increment { get; set; }
    public bool IntSafe { get; private set; }

    public float Value => (float)ValueObject;

    public void Increase()
    {
        var increment = Increment > 5 && Input.GetKeyInt(KeyCode.LeftShift) ? 5 : Increment;

        if (Value + increment >
            Max + 0.001f) // the slight increase is because of the stupid float rounding errors in the Giant speed
            Set(Min);
        else
            Set(Value + increment);
    }

    public void Decrease()
    {
        var increment = Increment > 5 && Input.GetKeyInt(KeyCode.LeftShift) ? 5 : Increment;

        if (Value - increment < Min - 0.001f) // added it here to in case I missed something else
            Set(Max);
        else
            Set(Value - increment);
    }

    public override void OptionCreated()
    {
        base.OptionCreated();
        var number = Setting.Cast<NumberOption>();
        number.ValidRange = new FloatRange(Min, Max);
        number.Increment = Increment;
        number.Value = number.oldValue = Value;
        number.ValueText.text = ToString();
    }
}