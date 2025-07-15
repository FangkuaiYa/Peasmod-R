namespace PeasAPI.Options;

public class CustomHeaderOption : CustomOption
{
    public CustomHeaderOption(MultiMenu menu, string name) : base(num++, menu, name,
        CustomOptionType.Header, 0)
    {
    }

    public override void OptionCreated()
    {
        base.OptionCreated();
        Setting.Cast<ToggleOption>().TitleText.text = GetName();
    }
}