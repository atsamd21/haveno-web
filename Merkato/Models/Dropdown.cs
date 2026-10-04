using System.Runtime.CompilerServices;

namespace Merkato.Models;

public class Dropdown
{
    // Value, display name
    public Dictionary<string, string> DropdownItems { get; set; }
    // Property in parent to bind to
    public string PropertyName { get; set; }
    public string PropertyValue { get; set; } = string.Empty;

    // CallerArgumentExpression brittle? works fine for when passing properties
    public Dropdown(
        Dictionary<string, string> dropdownItems, 
        string propertyValue, 
        [CallerArgumentExpression(nameof(propertyValue))] string? propertyName = null)
    {
        PropertyName = propertyName ?? throw new Exception();
        DropdownItems = dropdownItems;
        PropertyValue = propertyValue;
    }
}
