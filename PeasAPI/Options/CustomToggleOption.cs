namespace PeasAPI.Options;

public class CustomToggleOption : CustomOption
{
    public CustomToggleOption(MultiMenu menu, string optionName, bool value = true) : base(
        num++, menu, optionName, CustomOptionType.Toggle, value)
    {
        Format = val => (bool)val ? "On" : "Off";
    }

    public bool Value => (bool)ValueObject;

    public void Toggle()
    {
        Set(!Value);
    }

    public override void OptionCreated()
    {
        base.OptionCreated();
        var tgl = Setting.Cast<ToggleOption>();
        tgl.CheckMark.enabled = Value;
    }
}