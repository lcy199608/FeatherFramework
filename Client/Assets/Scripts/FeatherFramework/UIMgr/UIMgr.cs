using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public enum UILayer { Bottom, Middle, Top, System }
public enum UIType { Root, Page, Child }

/// <summary>一次打开的身份；关闭后不可再次用作 Child 的 owner。</summary>
public sealed class PanelHandle
{
    internal PanelHandle(UIMgr manager, string name, UIType type)
    {
        Manager = manager;
        Name = name;
        Type = type;
    }

    internal UIMgr Manager { get; }
    internal string Name { get; }
    internal readonly List<PanelHandle> Children = new List<PanelHandle>();
    internal bool Committed;
    internal bool Overlay;
    internal bool Visible;
    internal UILayer ActualLayer;
    public PanelBase Panel { get; internal set; }
    public UIType Type { get; internal set; }
    public PanelHandle Owner { get; internal set; }
    public bool IsOpen { get; internal set; } = true;
    public bool IsVisible => IsOpen && Visible;
}

public class UIInfo
{
    public string Name;
    public UILayer Layer;
    public object UIData;
    public Action<PanelBase> OnComplete;
    public UIInfo(string name, UILayer layer = UILayer.Middle, object uiData = null, Action<PanelBase> onComplete = null)
    {
        Name = name; Layer = layer; UIData = uiData; OnComplete = onComplete;
    }
}

public sealed class UIMgr : MonoBehaviour
{
    private sealed class OpenRequest
    {
        internal long Id;
        internal PanelHandle Handle;
        internal UIType? ExpectedType;
        internal Type ComponentType;
        internal PanelHandle RootScope;
        internal object Data;
        internal UILayer? Layer;
        internal Action<PanelBase> Callback;
        internal TaskCompletionSource<PanelHandle> Completion;
        internal GameObject Prefab;
        internal Exception Error;
        internal bool Ready;
    }

    public const string uiPath = "Res/UI/";
    private Func<string, GameObject> loadPrefab;
    private Action<string, UnityAction<GameObject>> loadPrefabAsync;
    private readonly Dictionary<string, PanelBase> cache = new Dictionary<string, PanelBase>();
    private readonly Dictionary<string, Type> panelTypes = new Dictionary<string, Type>();
    private readonly HashSet<PanelBase> instances = new HashSet<PanelBase>();
    private readonly List<PanelHandle> opened = new List<PanelHandle>();
    private readonly List<PanelHandle> pages = new List<PanelHandle>();
    private readonly List<OpenRequest> requests = new List<OpenRequest>();
    private readonly Queue<UIInfo> legacyQueue = new Queue<UIInfo>();
    private PanelHandle currentRoot;
    private long requestId;
    private long latestRootRequest;
    private bool shuttingDown;
    private bool removingPanels;
    private bool draining;
    private int operationDepth;
    private bool shutdownFinished;
    private Transform inactiveRoot;
    private Transform bot, mid, top, sys;
    public Canvas UICanvas;
    private Camera uiCamera;
    public Camera UICamera
    {
        get
        {
            if (uiCamera == null)
            {
                var cameraObject = UICanvas == null ? null : UICanvas.transform.Find("UICamera");
                cameraObject = cameraObject ?? transform.Find("UICamera");
                if (cameraObject != null) uiCamera = cameraObject.GetComponent<Camera>();
            }
            return uiCamera;
        }
        set { uiCamera = value; }
    }

    internal void Initialize(ResMgr assets)
    {
        if (assets == null) throw new ArgumentNullException(nameof(assets));
        Initialize(assets.Load<GameObject>, assets.LoadAsync<GameObject>);
    }

    internal void Initialize(Func<string, GameObject> load, Action<string, UnityAction<GameObject>> loadAsync)
    {
        loadPrefab = load ?? throw new ArgumentNullException(nameof(load));
        loadPrefabAsync = loadAsync ?? throw new ArgumentNullException(nameof(loadAsync));
    }

    public Task<PanelHandle> OpenRoot<T>(object data = null) where T : PanelBase =>
        Open<T>(UIType.Root, null, data);
    public Task<PanelHandle> OpenPage<T>(object data = null) where T : PanelBase =>
        Open<T>(UIType.Page, null, data);
    public Task<PanelHandle> OpenChild<T>(PanelHandle owner, object data = null) where T : PanelBase =>
        Open<T>(UIType.Child, owner, data);

    private Task<PanelHandle> Open<T>(UIType type, PanelHandle owner, object data) where T : PanelBase
    {
        RegisterPanelType(typeof(T));
        if (type == UIType.Child)
        {
            if (owner == null || owner.Manager != this || !owner.IsOpen || !owner.Committed ||
                owner.Panel == null || owner.Type == UIType.Child)
                throw new ArgumentException("Child requires an open Root or Page owned by this UI service.", nameof(owner));
        }
        var completion = new TaskCompletionSource<PanelHandle>();
        var request = BeginOpen(typeof(T).Name, type, owner, data, null, null, completion);
        if (request != null)
        {
            request.ComponentType = typeof(T);
            StartLoad(request, true);
        }
        return completion.Task;
    }

    private void RegisterPanelType(Type componentType)
    {
        string panelName = componentType.Name;
        if (panelTypes.TryGetValue(panelName, out var registeredType) && registeredType != componentType)
            throw new InvalidOperationException($"UI name '{panelName}' is shared by {registeredType.FullName} and {componentType.FullName}. Panel names must be unique.");
        if (cache.TryGetValue(panelName, out var existingPanel) && existingPanel != null && !componentType.IsInstanceOfType(existingPanel))
            throw new InvalidOperationException($"UI name '{panelName}' is already cached with a different component type.");
        panelTypes[panelName] = componentType;
    }

    public void ShowUI<T>(UILayer layer = UILayer.Middle, object uiData = null, Action<PanelBase> onComplete = null)
        where T : PanelBase { RegisterPanelType(typeof(T)); ShowUI(typeof(T).Name, layer, uiData, onComplete); }
    public void ShowUIAsync<T>(UILayer layer = UILayer.Middle, object uiData = null, Action<PanelBase> onComplete = null)
        where T : PanelBase { RegisterPanelType(typeof(T)); ShowUIAsync(typeof(T).Name, layer, uiData, onComplete); }
    public void ShowUI(string uiName, UILayer layer = UILayer.Middle, object uiData = null, Action<PanelBase> onComplete = null) =>
        OpenLegacy(uiName, layer, uiData, onComplete, false);
    public void ShowUIAsync(string uiName, UILayer layer = UILayer.Middle, object uiData = null, Action<PanelBase> onComplete = null) =>
        OpenLegacy(uiName, layer, uiData, onComplete, true);

    private void OpenLegacy(string name, UILayer layer, object data, Action<PanelBase> callback, bool async)
    {
        var request = BeginOpen(name, null, null, data, layer, callback, null);
        if (request != null) StartLoad(request, async);
    }

    private OpenRequest BeginOpen(string name, UIType? type, PanelHandle owner, object data, UILayer? layer,
        Action<PanelBase> callback, TaskCompletionSource<PanelHandle> completion)
    {
        if (shuttingDown || removingPanels)
        {
            completion?.TrySetCanceled();
            return null;
        }
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("UI name is required.", nameof(name));
        if (loadPrefab == null) throw new InvalidOperationException("UI service is not initialized.");
        if (UICanvas == null) CreateUICanvas();
        // Root/Page retain one instance per name. Child instances are scoped to their owner.
        if (type != UIType.Child)
            foreach (var previous in requests.Where(r => r.Handle.Name == name && r.ExpectedType != UIType.Child).ToArray())
                Cancel(previous);

        var handle = new PanelHandle(this, name, type ?? UIType.Page) { Owner = owner };
        var request = new OpenRequest
        {
            Id = ++requestId, Handle = handle, ExpectedType = type, RootScope = currentRoot,
            Data = data, Layer = layer, Callback = callback, Completion = completion
        };
        opened.Add(handle);
        owner?.Children.Add(handle);
        requests.Add(request);
        if (type == UIType.Root)
        {
            foreach (var previous in requests.Where(r => r != request && r.ExpectedType == UIType.Root).ToArray())
                Cancel(previous);
        }
        return request;
    }

    private void StartLoad(OpenRequest request, bool async)
    {
        try
        {
            if (request.ExpectedType != UIType.Child && cache.TryGetValue(request.Handle.Name, out var cached) && cached != null)
                Loaded(request, null);
            else if (async)
                loadPrefabAsync(uiPath + request.Handle.Name + ".prefab", prefab => Loaded(request, prefab));
            else
                Loaded(request, loadPrefab(uiPath + request.Handle.Name + ".prefab"));
        }
        catch (Exception exception)
        {
            if (requests.Contains(request))
            {
                request.Error = exception;
                request.Ready = true;
                Drain();
            }
            else Debug.LogException(exception);
        }
    }

    private void Loaded(OpenRequest request, GameObject prefab)
    {
        if (!request.Handle.IsOpen || !requests.Contains(request)) return;
        request.Prefab = prefab;
        request.Ready = true;
        Drain();
    }

    // Typed Page opens commit in request order, independent of asset completion order.
    private void Drain()
    {
        if (draining || removingPanels || shuttingDown) return;
        draining = true;
        try
        {
            while (true)
            {
                var next = requests.FirstOrDefault(r => r.Ready &&
                    (r.ExpectedType != UIType.Page || !requests.Any(earlier =>
                        earlier.ExpectedType == UIType.Page && earlier.Id < r.Id)));
                if (next == null) break;
                CompleteOpen(next);
            }
        }
        finally { draining = false; }
    }

    private void CompleteOpen(OpenRequest request)
    {
        operationDepth++;
        try { CompleteOpenCore(request); }
        finally { EndOperation(); }
    }

    private void CompleteOpenCore(OpenRequest request)
    {
        var handle = request.Handle;
        try
        {
            if (request.Error != null) throw request.Error;
            if (request.ExpectedType == UIType.Page && request.RootScope != currentRoot)
            {
                Cancel(request);
                return;
            }

            var panel = PreparePanel(request, out bool created);
            if (!handle.IsOpen || shuttingDown)
            {
                if (created) DestroyPanel(panel);
                return;
            }
            var type = ValidatePanel(request, panel);
            handle.Type = type;
            handle.Overlay = !request.ExpectedType.HasValue && type == UIType.Page && panel.IsStackable;
            if (type == UIType.Root && request.Id < latestRootRequest)
            {
                Cancel(request);
                return;
            }

            // Repeated opens reuse a live Root/Page identity; cached closed panels get a new identity.
            var existing = panel.Handle;
            if (!created && existing != null && existing.IsOpen)
            {
                bool overlay = handle.Overlay;
                handle.IsOpen = false;
                opened.Remove(handle);
                request.Handle = handle = existing;
                handle.Overlay = overlay;
            }
            handle.Panel = panel;
            panel.Handle = handle;
            panel.uiData = request.Data;
            if (type != UIType.Child) cache[handle.Name] = panel;
            requests.Remove(request);
            handle.Committed = true;

            handle.ActualLayer = request.Layer ?? panel.Layer;
            SetLayout(panel, handle.ActualLayer);
            foreach (var child in handle.Children.ToArray())
            {
                if (!child.IsOpen || child.Panel == null || !child.Committed) continue;
                child.ActualLayer = child.Panel.Layer;
                SetLayout(child.Panel, child.ActualLayer);
            }
            CommitNavigation(request, handle);
            if (!handle.IsOpen || shuttingDown)
            {
                request.Completion?.TrySetCanceled();
                return;
            }
            if (handle.Owner == null || handle.Owner.IsVisible)
            {
                if (handle.Visible) panel.OnShow(); // Refresh data on an already visible cached panel.
                else Resume(handle);
            }
            if (!handle.IsOpen || shuttingDown)
            {
                request.Completion?.TrySetCanceled();
                return;
            }
            request.Completion?.TrySetResult(handle);
            if (request.Callback != null)
            {
                try { request.Callback(panel); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
        catch (Exception exception)
        {
            requests.Remove(request);
            CloseInternal(handle, true);
            if (handle.Panel != null && !cache.ContainsValue(handle.Panel)) DestroyPanel(handle.Panel);
            if (request.Completion != null) request.Completion.TrySetException(exception);
            else Debug.LogException(exception);
        }
    }

    private PanelBase PreparePanel(OpenRequest request, out bool created)
    {
        var handle = request.Handle;
        PanelBase panel = null;
        created = handle.Type == UIType.Child || !cache.TryGetValue(handle.Name, out panel) || panel == null;
        if (created)
        {
            if (request.Prefab == null) throw new InvalidOperationException($"UI prefab not found: {handle.Name}");
            var instance = Instantiate(request.Prefab, GetInactiveRoot(), false);
            instance.name = handle.Name;
            instance.SetActive(false);
            panel = instance.GetComponent<PanelBase>();
            if (panel == null || !(instance.transform is RectTransform))
            {
                DestroyOwnedObject(instance);
                throw new InvalidOperationException($"UI {handle.Name} requires PanelBase and RectTransform components.");
            }
            instances.Add(panel);
            handle.Panel = panel;
            panel.Handle = handle;
            panel.uiData = request.Data;
            panel.OnInit();
        }
        return panel;
    }

    private static UIType ValidatePanel(OpenRequest request, PanelBase panel)
    {
        var handle = request.Handle;
        if (request.ComponentType != null && !request.ComponentType.IsInstanceOfType(panel))
            throw new InvalidOperationException($"UI {handle.Name} does not contain {request.ComponentType.Name}.");
        var type = panel.Type;
        if (request.ExpectedType.HasValue && type != request.ExpectedType.Value)
            throw new InvalidOperationException($"UI {handle.Name} is {type}, expected {request.ExpectedType.Value}.");
        if (type == UIType.Child && handle.Owner == null)
            throw new InvalidOperationException("Child panels must be opened with OpenChild and an explicit owner.");
        return type;
    }

    private void CommitNavigation(OpenRequest request, PanelHandle handle)
    {
        if (handle.Type == UIType.Root)
        {
            latestRootRequest = request.Id;
            bool wasRemoving = removingPanels;
            removingPanels = true;
            try
            {
                legacyQueue.Clear();
                // Typed requests belong to the old root flow. Preserve only newer legacy requests.
                foreach (var other in requests.Where(r => r.Id < request.Id ||
                    r.ExpectedType == UIType.Page || r.ExpectedType == UIType.Child).ToArray())
                    Cancel(other);
                foreach (var other in opened.Where(h => h != handle && h.Committed).ToArray())
                    CloseInternal(other, false);
                foreach (var child in handle.Children.ToArray()) CloseInternal(child, false);
                currentRoot = handle;
            }
            finally { removingPanels = wasRemoving; }
        }
        else if (handle.Type == UIType.Page && !handle.Overlay)
        {
            var previous = pages.LastOrDefault();
            if (previous != handle) Suspend(previous);
            if (!handle.IsOpen || shuttingDown)
            {
                request.Completion?.TrySetCanceled();
                return;
            }
            pages.Remove(handle);
            pages.Add(handle);
        }
    }

    private void SetLayout(PanelBase panel, UILayer layer)
    {
        Transform parent;
        switch (layer)
        {
            case UILayer.Bottom: parent = bot; break;
            case UILayer.Middle: parent = mid; break;
            case UILayer.Top: parent = top; break;
            case UILayer.System: parent = sys; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer));
        }
        var rect = (RectTransform)panel.transform;
        rect.SetParent(parent, false);
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();
    }

    private void Suspend(PanelHandle handle)
    {
        if (handle == null || !handle.IsOpen) return;
        foreach (var child in handle.Children.ToArray()) Suspend(child);
        HideInstance(handle);
    }

    private void Resume(PanelHandle handle)
    {
        if (handle == null || !handle.IsOpen || !handle.Committed || handle.Panel == null || shuttingDown) return;
        if (handle.Owner != null && !handle.Owner.IsVisible) return;
        if (!handle.Visible)
        {
            handle.Visible = true;
            handle.Panel.transform.SetAsLastSibling();
            handle.Panel.gameObject.SetActive(true);
            if (handle.IsOpen && handle.Visible && handle.Panel != null) handle.Panel.OnShow();
        }
        foreach (var child in handle.Children.ToArray())
        {
            // OnShow (including a previous Child's OnShow) may navigate away or close the owner.
            if (!handle.IsVisible || handle.Panel == null || shuttingDown) return;
            Resume(child);
        }
    }

    private void HideInstance(PanelHandle handle)
    {
        if (!handle.Visible) return;
        handle.Visible = false;
        var panel = handle.Panel;
        if (panel == null) return;
        panel.gameObject.SetActive(false);
        if (panel != null) InvokeCleanup(panel.OnHide);
    }

    public void Close(PanelHandle handle)
    {
        if (handle == null) return;
        if (handle.Manager != this) throw new ArgumentException("Handle belongs to another UI service.", nameof(handle));
        CloseInternal(handle, true);
        Drain();
    }

    private void CloseInternal(PanelHandle handle, bool restore)
    {
        operationDepth++;
        try { CloseCore(handle, restore); }
        finally { EndOperation(); }
    }

    private void CloseCore(PanelHandle handle, bool restore)
    {
        if (handle == null || !handle.IsOpen) return;
        bool wasTop = pages.LastOrDefault() == handle;
        bool wasRoot = currentRoot == handle;
        handle.IsOpen = false;
        opened.Remove(handle);
        pages.Remove(handle);
        handle.Owner?.Children.Remove(handle);
        if (wasRoot) currentRoot = null;
        foreach (var pending in requests.Where(r => r.Handle == handle).ToArray())
        {
            requests.Remove(pending);
            pending.Completion?.TrySetCanceled();
        }
        foreach (var child in handle.Children.ToArray()) CloseInternal(child, false);
        if (wasRoot)
        {
            foreach (var pending in requests.Where(r => r.ExpectedType == UIType.Page && r.RootScope == handle).ToArray())
                Cancel(pending);
            foreach (var page in pages.ToArray()) CloseInternal(page, false);
        }
        HideInstance(handle);
        if (handle.Committed && handle.Panel != null) InvokeCleanup(handle.Panel.OnClose);
        if (handle.Panel != null && handle.Panel.Handle == handle) handle.Panel.Handle = null;
        if (handle.Type == UIType.Child || !handle.Committed) DestroyPanel(handle.Panel);

        if (restore && wasTop && !removingPanels && !shuttingDown)
        {
            Resume(pages.LastOrDefault());
            if (pages.Count == 0 && legacyQueue.Count > 0) OpenNextQueuedUI();
        }
    }

    private void Cancel(OpenRequest request)
    {
        requests.Remove(request);
        CloseInternal(request.Handle, false);
        request.Completion?.TrySetCanceled();
    }

    public void Back()
    {
        var owner = pages.LastOrDefault() ?? currentRoot;
        var child = owner?.Children.LastOrDefault(h => h.IsOpen && h.IsVisible);
        if (child != null) Close(child);
        else if (pages.Count > 0) Close(pages.Last());
    }

    public bool TryGetUI<T>(out T panel)
    {
        var matches = opened.Where(h => h.Committed && h.Panel is T).ToArray();
        if (matches.Length == 1) { panel = matches[0].Panel.GetComponent<T>(); return true; }
        if (matches.Length == 0 && cache.TryGetValue(typeof(T).Name, out var cached) && cached != null)
        {
            panel = cached.GetComponent<T>();
            return panel != null;
        }
        panel = default(T);
        return false;
    }

    public void HideUI<T>() where T : PanelBase => HideUI(typeof(T).Name);
    public void HideUI(string uiName)
    {
        foreach (var handle in opened.Where(h => h.Name == uiName).ToArray()) CloseInternal(handle, true);
        Drain();
    }

    public void HideAllUI()
    {
        bool previous = removingPanels;
        removingPanels = true;
        try
        {
            legacyQueue.Clear();
            foreach (var request in requests.ToArray()) Cancel(request);
            foreach (var handle in opened.ToArray()) CloseInternal(handle, false);
        }
        finally { removingPanels = previous; }
    }

    public void RemoveSpecifiedUI<T>() where T : PanelBase => RemoveSpecifiedUI(typeof(T).Name);
    public void RemoveSpecifiedUI(string uiName)
    {
        HideUI(uiName);
        if (cache.TryGetValue(uiName, out var panel))
        {
            cache.Remove(uiName);
            DestroyPanel(panel);
        }
    }

    public void RemoveSpecifiedUIFromStack(string uiName)
    {
        foreach (var handle in pages.Where(h => h.Name == uiName).ToArray()) CloseInternal(handle, true);
    }

    public void ShowUIQueue(params UIInfo[] infos)
    {
        if (shuttingDown || removingPanels || infos == null || infos.Length == 0) return;
        removingPanels = true;
        try
        {
            legacyQueue.Clear();
            foreach (var request in requests.ToArray()) Cancel(request);
            foreach (var page in pages.ToArray()) CloseInternal(page, false);
        }
        finally { removingPanels = false; }
        if (shuttingDown) return;
        foreach (var info in infos) legacyQueue.Enqueue(info);
        OpenNextQueuedUI();
    }

    private void OpenNextQueuedUI()
    {
        if (legacyQueue.Count == 0) return;
        var info = legacyQueue.Dequeue();
        ShowUI(info.Name, info.Layer, info.UIData, info.OnComplete);
    }

    public void RemoveAllUI()
    {
        bool previous = removingPanels;
        removingPanels = true;
        try
        {
            HideAllUI();
            foreach (var panel in instances.ToArray()) DestroyPanel(panel);
            cache.Clear();
        }
        finally { removingPanels = previous; }
    }

    private void DestroyPanel(PanelBase panel)
    {
        if (ReferenceEquals(panel, null)) return;
        instances.Remove(panel);
        if (panel == null) return;
        panel.DisposeOnce();
        if (panel != null) DestroyOwnedObject(panel.gameObject);
    }

    private void InvokeCleanup(Action cleanup)
    {
        bool previous = removingPanels;
        removingPanels = true;
        try { cleanup(); }
        catch (Exception exception) { Debug.LogException(exception); }
        finally { removingPanels = previous; }
    }

    internal void Shutdown()
    {
        if (shuttingDown) return;
        shuttingDown = true;
        if (operationDepth == 0) FinishShutdown();
    }

    private void EndOperation()
    {
        operationDepth--;
        if (operationDepth == 0 && shuttingDown && !shutdownFinished) FinishShutdown();
    }

    private void FinishShutdown()
    {
        if (shutdownFinished) return;
        shutdownFinished = true;
        RemoveAllUI();
        if (uiCamera != null && (UICanvas == null || !uiCamera.transform.IsChildOf(UICanvas.transform)))
            DestroyOwnedObject(uiCamera.gameObject);
        uiCamera = null;
        if (UICanvas != null) DestroyOwnedObject(UICanvas.gameObject);
        if (inactiveRoot != null) DestroyOwnedObject(inactiveRoot.gameObject);
        UICanvas = null;
        inactiveRoot = null;
        loadPrefab = null;
        loadPrefabAsync = null;
    }

    private void OnDestroy() => Shutdown();

    private static void DestroyOwnedObject(GameObject instance)
    {
        instance.SetActive(false);
        if (Application.isPlaying) Destroy(instance);
        else DestroyImmediate(instance);
    }

    internal void CreateUICanvas(bool activate = true)
    {
        if (shuttingDown || loadPrefab == null)
        {
            throw new InvalidOperationException("UI canvas cannot be created before initialization or after shutdown.");
        }
        if (UICanvas == null)
        {
            var prefab = loadPrefab(uiPath + "UICanvas.prefab");
            if (prefab == null) throw new InvalidOperationException("UI canvas prefab was not found.");
            var instance = Instantiate(prefab, GetInactiveRoot(), false);
            instance.SetActive(false);
            try
            {
                UICanvas = instance.GetComponent<Canvas>();
                if (UICanvas == null) throw new InvalidOperationException("UI canvas prefab does not contain a Canvas component.");
                var canvas = UICanvas.transform;
                bot = canvas.Find("BottomLayer");
                mid = canvas.Find("MiddleLayer");
                top = canvas.Find("TopLayer");
                sys = canvas.Find("SystemLayer");
                if (bot == null || mid == null || top == null || sys == null)
                    throw new InvalidOperationException("UI canvas is missing one or more required layer transforms.");
                uiCamera = canvas.Find("UICamera")?.GetComponent<Camera>();
                canvas.SetParent(transform, false);
                // Screen-space Canvas sizing must not also move/scale the camera rendering it.
                if (uiCamera != null) uiCamera.transform.SetParent(transform, false);
            }
            catch
            {
                UICanvas = null;
                bot = mid = top = sys = null;
                DestroyOwnedObject(instance);
                throw;
            }
        }
        if (activate) ActivateUICanvas();
    }

    internal void ActivateUICanvas()
    {
        if (shuttingDown || UICanvas == null)
            throw new InvalidOperationException("UI canvas is not available.");
        UICanvas.gameObject.SetActive(true);
    }

    private Transform GetInactiveRoot()
    {
        if (inactiveRoot == null)
        {
            var root = new GameObject("UIStaging");
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            inactiveRoot = root.transform;
        }
        return inactiveRoot;
    }
}
