namespace Merkato.Models;

public class InfoRow
{
    public string Label { get; set; }
    public string Value { get; set; }
    public bool IsExpandable { get; set; }

    public InfoRow(string label, string value)
    { 
        Label = label; 
        Value = value;
    }

    public InfoRow(string label, string value, bool isExpandable)
    {
        Label = label;
        Value = value;
        IsExpandable = isExpandable;
    }
}
