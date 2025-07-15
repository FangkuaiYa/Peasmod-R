namespace PeasAPI.Options;

public class CustomStringOption : CustomOption
{
    public CustomStringOption(MultiMenu menu, string optionName, string[] values,
        int startingId = 0) :
        base(num++, menu, optionName, CustomOptionType.String, startingId)
    {
        Values = values;
        Format = value => Values[(int)value];
    }

    public string[] Values { get; set; }
    public int Value => (int)ValueObject;
    public string StringValue => Values[Value];

    public void Increase()
    {
        if (Value >= Values.Length - 1)
            Set(0);
        else
            Set(Value + 1);
    }

    public void Decrease()
    {
        if (Value <= 0)
            Set(Values.Length - 1);
        else
            Set(Value - 1);
    }

    public override void OptionCreated()
    {
        base.OptionCreated();
        var str = Setting.Cast<StringOption>();
        str.Value = str.oldValue = Value;
        str.ValueText.text = ToString();
    }
}