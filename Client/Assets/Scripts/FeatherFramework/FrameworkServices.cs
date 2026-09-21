using System;

public sealed class FrameworkServices
{
    public GameConfig GameConfig { get; }
    public EventCenter Events { get; }
    public ResMgr Assets { get; }
    public SaveDataMgr Save { get; }
    public ConfigMgr Config { get; }
    private readonly LanguageMgr localization;
    private readonly RedDotSystem redDots;
    public bool HasLocalization => localization != null;
    public bool HasRedDots => redDots != null;
    public LanguageMgr Localization => localization ?? throw new InvalidOperationException("Localization is disabled in GameConfig.");
    public RedDotSystem RedDots => redDots ?? throw new InvalidOperationException("Red dots are disabled in GameConfig.");
    public TimerMgr Timers { get; }
    public SceneMgr Scenes { get; }
    public PoolMgr Pools { get; }
    public AudioMgr Audio { get; }
    public UIMgr UI { get; }

    internal FrameworkServices(
        GameConfig gameConfig,
        EventCenter events,
        ResMgr assets,
        SaveDataMgr save,
        ConfigMgr config,
        LanguageMgr localization,
        RedDotSystem redDots,
        TimerMgr timers,
        SceneMgr scenes,
        PoolMgr pools,
        AudioMgr audio,
        UIMgr ui)
    {
        GameConfig = gameConfig ?? throw new ArgumentNullException(nameof(gameConfig));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        Save = save ?? throw new ArgumentNullException(nameof(save));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        this.localization = localization;
        this.redDots = redDots;
        Timers = timers ?? throw new ArgumentNullException(nameof(timers));
        Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        Pools = pools ?? throw new ArgumentNullException(nameof(pools));
        Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
    }
}

public static class Framework
{
    private static FrameworkServices services;

    public static bool IsReady => services != null;

    public static FrameworkServices Services => services
        ?? throw new InvalidOperationException("FeatherFramework is not initialized. Access services after FrameworkReady.");

    internal static void SetServices(FrameworkServices value)
    {
        services = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal static void ClearServices()
    {
        services = null;
    }
}

public abstract class FrameworkBehaviour : UnityEngine.MonoBehaviour
{
    protected FrameworkServices Services => Framework.Services;
}
