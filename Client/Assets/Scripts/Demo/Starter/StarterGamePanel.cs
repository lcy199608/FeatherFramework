using System;
using UnityEngine.UI;

public class StarterGamePanel : PanelBase
{
    public Text status;
    public Button collectButton;
    public Button backButton;
    private const string ProgressKey = "FeatherStarter.Progress";
    private Progress progress;
    [Serializable]
    public sealed class Progress
    {
        public int Version = 1;
        public int Coins;
    }
    public override UIType Type => UIType.Root;
    public override void OnInit()
    {
        collectButton.onClick.AddListener(Collect);
        backButton.onClick.AddListener(ReturnToMenu);
    }
    public override void OnShow()
    {
        progress = Framework.Services.Save.GetData(ProgressKey, new Progress());
        // Version 0 is the initial unversioned shape; it had the same Coins field.
        if (progress.Version == 0) progress.Version = 1;
        bool supported = progress.Version == 1;
        collectButton.interactable = supported;
        backButton.interactable = true;
        status.text = supported ? $"Saved coins: {progress.Coins}" : "Save belongs to a newer demo version.";
    }
    public override void OnHide() { }
    private void Collect()
    {
        var updated = new Progress { Coins = progress.Coins + 1 };
        try
        {
            Framework.Services.Save.SetData(ProgressKey, updated, true);
            progress = updated;
            status.text = $"Saved coins: {progress.Coins}";
        }
        catch (Exception exception) { status.text = exception.Message; }
    }
    private async void ReturnToMenu()
    {
        backButton.interactable = false;
        try { await Framework.Services.Scenes.SwitchAsync(((StarterRoutes)uiData).Menu); }
        catch (Exception exception)
        {
            if (this == null) return;
            status.text = exception.Message;
            backButton.interactable = true;
        }
    }
    public override void OnDispose()
    {
        collectButton.onClick.RemoveListener(Collect);
        backButton.onClick.RemoveListener(ReturnToMenu);
    }
}
