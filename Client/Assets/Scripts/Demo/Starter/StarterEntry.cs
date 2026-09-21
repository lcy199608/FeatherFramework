using System;
using UnityEngine;

// Attach only to the two starter scenes. Production scenes do not run this example.
public sealed class StarterEntry : FrameworkBehaviour
{
    public bool gameplay;
    public string menuScene = "Assets/Scenes/Starter/Menu.unity";
    public string gameScene = "Assets/Scenes/Starter/Game.unity";
    private ResMgr.AssetScope levelAssets;
    private async void Start()
    {
        try
        {
            var routes = new StarterRoutes { Menu = menuScene, Game = gameScene };
            if (gameplay)
            {
                levelAssets = Services.Assets.CreateScope();
                levelAssets.LoadAsync<TextAsset>("Res/Starter/Level.txt", text =>
                {
                    if (text != null) Debug.Log(text.text);
                });
                await Services.UI.OpenRoot<StarterGamePanel>(routes);
            }
            else await Services.UI.OpenRoot<StarterMenuPanel>(routes);
        }
        catch (Exception exception) { Debug.LogException(exception); }
    }
    private void OnDestroy() => levelAssets?.Dispose();
}

public sealed class StarterRoutes
{
    public string Menu;
    public string Game;
}
