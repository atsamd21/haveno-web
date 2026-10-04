namespace Merkato.Models;

public class Tabs
{
    public int TabIndex { get; }
    public List<string> TabNames { get; set; }

    public Tabs(List<string> tabNames, int tabIndex)
    {
        TabNames = tabNames;
        TabIndex = tabIndex;
    }
}
