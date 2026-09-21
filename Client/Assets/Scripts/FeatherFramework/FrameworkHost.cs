using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[DefaultExecutionOrder(-10000)]
public sealed class FrameworkHost : MonoBehaviour
{
    public enum InitializationState
    {
        NotStarted,
        Initializing,
        Ready,
        Failed
    }

    private const string GameFile = "GameFile";
    private static FrameworkHost current;
    private ResMgr assets;
    private AudioMgr audioMgr;
    private UIMgr uiMgr;
    private TimerMgr timers;
    private PoolMgr pools;
    private EventCenter events;
    private SceneMgr scenes;
    private SaveDataMgr save;
    private LanguageMgr localization;
    private RedDotSystem redDots;

    public bool IsReady { get; private set; }
    public InitializationState State { get; private set; }
    public Exception InitializationError { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureHostExists()
    {
        if (current != null)
        {
            return;
        }

        FrameworkHost existing = FindObjectOfType<FrameworkHost>(true);
        if (existing != null)
        {
            current = existing;
            return;
        }

        new GameObject(nameof(FrameworkHost)).AddComponent<FrameworkHost>();
    }

    private void Awake()
    {
        if (current != null && current != this)
        {
            Destroy(gameObject);
            return;
        }

        current = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (State == InitializationState.Initializing || State == InitializationState.Ready)
        {
            return;
        }

        State = InitializationState.Initializing;
        IsReady = false;
        InitializationError = null;
        try
        {
            var initialization = Addressables.InitializeAsync(false);
            try
            {
                initialization.WaitForCompletion();
                if (initialization.Status != AsyncOperationStatus.Succeeded)
                    throw new InvalidOperationException("Addressables initialization failed.");
            }
            finally
            {
                if (initialization.IsValid()) Addressables.Release(initialization);
            }

            events = new EventCenter();
            assets = new ResMgr(this);
            GameConfig gameConfig = assets.Load<GameConfig>("Res/GameConfig.asset");
            if (gameConfig == null)
            {
                throw new InvalidOperationException("GameConfig could not be loaded from Res/GameConfig.asset.");
            }

            Reporter.IsEnableLog = gameConfig.isDebug;
            Debug.unityLogger.logEnabled = gameConfig.isDebug;
            SetFrameRate(gameConfig.targetFrameRate);

            save = new SaveDataMgr();
            save.Initialize();
            save.LoadData(save.GetSystemData(GameFile, 0));

            var config = new ConfigMgr();
            config.InitConfig(gameConfig.enableLocalization, gameConfig.enableRedDots);

            timers = new TimerMgr(this);
            scenes = new SceneMgr(this, events);
            pools = new PoolMgr(assets, transform);
            localization = gameConfig.enableLocalization ? new LanguageMgr(save, config, events, gameConfig) : null;
            redDots = gameConfig.enableRedDots ? new RedDotSystem(config) : null;

            audioMgr = gameObject.AddComponent<AudioMgr>();
            audioMgr.Initialize(assets, save);
            uiMgr = gameObject.AddComponent<UIMgr>();
            uiMgr.Initialize(assets);

            var services = new FrameworkServices(
                gameConfig,
                events,
                assets,
                save,
                config,
                localization,
                redDots,
                timers,
                scenes,
                pools,
                audioMgr,
                uiMgr);

            redDots?.InitRedDotTreeNode();
            // Validate under an inactive parent so Canvas Awake/OnEnable cannot observe partial services.
            uiMgr.CreateUICanvas(false);
            IsReady = true;
            State = InitializationState.Ready;
            Framework.SetServices(services);
            uiMgr.ActivateUICanvas();
            events.Publish(FrameworkEvents.FrameworkReadyId);
        }
        catch (Exception exception)
        {
            InitializationError = exception;
            Debug.unityLogger.logEnabled = true;
            Debug.LogException(exception);
            IsReady = false;
            ShutdownServices();
            if (audioMgr != null) Destroy(audioMgr);
            if (uiMgr != null) Destroy(uiMgr);
            audioMgr = null;
            uiMgr = null;
            State = InitializationState.Failed;
        }
    }

    public bool RetryInitialize()
    {
        if (State != InitializationState.Failed)
        {
            return IsReady;
        }
        Initialize();
        return IsReady;
    }

    private void Update()
    {
        if (IsReady)
        {
            Framework.Services.Timers.Tick();
            scenes?.Tick();
        }
    }

    private void OnDestroy()
    {
        if (current == this)
        {
            IsReady = false;
            ShutdownServices();
            current = null;
            State = InitializationState.NotStarted;
            InitializationError = null;
        }
    }

    private void ShutdownServices()
    {
        // Keep the service surface alive while owners release their subscriptions and handles.
        Cleanup(() => { if (uiMgr != null) uiMgr.Shutdown(); });
        Cleanup(() => { if (audioMgr != null) audioMgr.Shutdown(); });
        Cleanup(() => localization?.Shutdown());
        Cleanup(() => redDots?.Shutdown());
        Cleanup(() => save?.Shutdown());
        Cleanup(() => timers?.Shutdown());
        Cleanup(() => pools?.Shutdown());
        Cleanup(() => scenes?.Shutdown());
        Cleanup(StopAllCoroutines);
        Cleanup(() => events?.Shutdown());
        Cleanup(() => assets?.Shutdown());
        save = null;
        localization = null;
        redDots = null;
        timers = null;
        pools = null;
        events = null;
        scenes = null;
        assets = null;
        Framework.ClearServices();
    }

    private static void Cleanup(Action cleanup)
    {
        try { cleanup(); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && IsReady && save != null && !save.IsReadOnly) Cleanup(save.ApplyChangesToDatabase);
    }

    private void OnApplicationQuit()
    {
        if (IsReady && save != null && !save.IsReadOnly) Cleanup(save.ApplyChangesToDatabase);
    }

    private static void SetFrameRate(int frameRate)
    {
        QualitySettings.vSyncCount = frameRate == -1 ? 1 : 0;
        Application.targetFrameRate = frameRate;
    }
}
