using System;
using UnityEngine;
using UnityEngine.UI;

public class StarterMenuPanel : PanelBase
{
    public Text status;
    public Button startButton;
    public override UIType Type => UIType.Root;
    public override void OnInit() => startButton.onClick.AddListener(EnterGame);
    public override void OnShow() { startButton.interactable = true; status.text = "A small game built with FeatherFramework"; }
    public override void OnHide() { }
    public override void OnDispose() => startButton.onClick.RemoveListener(EnterGame);
    private async void EnterGame()
    {
        startButton.interactable = false;
        try { await Framework.Services.Scenes.SwitchAsync(((StarterRoutes)uiData).Game); }
        catch (Exception exception)
        {
            if (this == null) return;
            status.text = exception.Message;
            startButton.interactable = true;
        }
    }
}
