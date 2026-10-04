namespace Merkato.Models;

public class Modal
{
    public string Title { get; set; }
    public string? SubmitMethodName { get; set; }
    public string SubmitButtonText { get; set; } = "Ok";
    public string CloseButtonText { get; set; } = "Cancel";
    public string[] Information { get; set; } = [];
    public ModalButtonOptions ModalButtonOptions { get; set; } = ModalButtonOptions.OK | ModalButtonOptions.CANCEL;
    public Dictionary<string, string> PostParameters { get; set; } = [];
    public Dictionary<string, string> CloseParameters { get; set; } = [];

    public Modal(string title)
    {
        Title = title;
    }

    public Modal(string title, string submitMethodName)
    {
        SubmitMethodName = submitMethodName;
        Title = title;
    }
}
