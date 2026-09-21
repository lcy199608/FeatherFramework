#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Opt-in built-player check. Ordinary Play/build runs never execute it.
public sealed class StarterPlayerSmoke : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!Environment.GetCommandLineArgs().Any(value => value.StartsWith("-feather-smoke-"))) return;
        var holder = new GameObject("StarterPlayerVerification");
        DontDestroyOnLoad(holder);
        holder.AddComponent<StarterPlayerSmoke>();
    }

    private static async Task Wait(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Player verification timed out.");
            await Task.Delay(20);
        }
    }

    private static void Capture(Camera camera, string path)
    {
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            if (!image.GetPixels32().Any(pixel => pixel.r > 180 && pixel.g > 180 && pixel.b > 180))
                throw new InvalidOperationException("UI verification image contains no visible text.");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            Destroy(image);
        }
    }

    private async void Start()
    {
        try
        {
            bool write = Environment.GetCommandLineArgs().Contains("-feather-smoke-write");
            await Wait(() => Framework.IsReady);
            var services = Framework.Services;
            await Wait(() => services.UI.TryGetUI<StarterMenuPanel>(out var panel) && panel.Handle != null && panel.Handle.IsVisible);
            services.UI.TryGetUI<StarterMenuPanel>(out var menu);
            menu.startButton.onClick.Invoke();
            await Wait(() => services.UI.TryGetUI<StarterGamePanel>(out var panel) && panel.Handle != null && panel.Handle.IsVisible);
            services.UI.TryGetUI<StarterGamePanel>(out var game);
            Canvas.ForceUpdateCanvases();
            var camera = services.UI.UICamera;
            foreach (var graphic in game.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                if (camera != null && (camera.cullingMask & (1 << graphic.gameObject.layer)) == 0)
                    throw new InvalidOperationException("UI graphic is outside the UI camera mask.");
                if (graphic.rectTransform.rect.width <= 0 || graphic.rectTransform.rect.height <= 0)
                    throw new InvalidOperationException("UI graphic has an empty layout.");
            }
            const string key = "FeatherStarter.VerificationExpected";
            if (write)
            {
                game.collectButton.onClick.Invoke();
                int coins = services.Save.GetData("FeatherStarter.Progress", new StarterGamePanel.Progress()).Coins;
                services.Save.SetData(key, coins, true);
            }
            else
            {
                int expected = services.Save.GetData(key, -1);
                int actual = services.Save.GetData("FeatherStarter.Progress", new StarterGamePanel.Progress()).Coins;
                if (expected < 1 || expected != actual) throw new InvalidOperationException("Player restart did not restore saved coins.");
            }
            var voice = services.Audio.PlayLoopAudio("VerificationTone", AudioType.EFFECT);
            await Wait(() => FindObjectsOfType<AudioSource>().Any(source => source.isPlaying));
            voice.Stop();
            if (voice.IsValid) throw new InvalidOperationException("Voice did not stop.");
            string screenshot = Environment.GetEnvironmentVariable("FEATHER_SMOKE_SCREENSHOT");
            if (!string.IsNullOrEmpty(screenshot)) Capture(camera, screenshot);
            game.backButton.onClick.Invoke();
            await Wait(() => menu.Handle != null && menu.Handle.IsVisible);
            Debug.Log("FEATHER_SMOKE_PASS " + (write ? "write" : "read") + " save=" + Application.persistentDataPath);
            Application.Quit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); Application.Quit(1); }
    }
}
#endif
