using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public sealed class SceneMgr
{
    private readonly EventCenter events;
    private TaskCompletionSource<bool> pending;
    private AsyncOperation operation;
    private int targetIndex;
    private bool stopped;
    public bool IsLoading => pending != null;

    internal SceneMgr(MonoBehaviour coroutineRunner, EventCenter events)
    {
        this.events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public void LoadScene(int sceneIndex, UnityAction action)
    {
        Validate(sceneIndex);
        if (IsLoading) throw new InvalidOperationException("A scene transition is already in progress.");
        SceneManager.LoadScene(sceneIndex);
        action?.Invoke();
    }

    // Same target joins the current operation; a different target fails instead of racing it.
    public Task SwitchAsync(int sceneIndex)
    {
        try { Validate(sceneIndex); }
        catch (Exception exception) { return Task.FromException(exception); }
        if (pending != null)
            return targetIndex == sceneIndex ? pending.Task :
                Task.FromException(new InvalidOperationException("A different scene transition is already in progress."));
        try
        {
            pending = new TaskCompletionSource<bool>();
            var result = pending.Task;
            targetIndex = sceneIndex;
            operation = SceneManager.LoadSceneAsync(sceneIndex);
            if (operation == null) throw new InvalidOperationException($"Failed to load scene {sceneIndex}.");
            operation.completed += Complete;
            return result;
        }
        catch (Exception exception)
        {
            var failed = pending;
            pending = null;
            operation = null;
            if (failed != null) { failed.TrySetException(exception); return failed.Task; }
            return Task.FromException(exception);
        }
    }

    public Task SwitchAsync(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return Task.FromException(new ArgumentException("A full scene asset path is required.", nameof(scenePath)));
        return SwitchAsync(SceneUtility.GetBuildIndexByScenePath(scenePath));
    }

    public async void LoadSceneAsync(int sceneIndex, UnityAction action, UnityAction<Exception> onError = null)
    {
        try { await SwitchAsync(sceneIndex); action?.Invoke(); }
        catch (Exception exception)
        {
            if (onError == null) Debug.LogException(exception);
            else
            {
                try { onError(exception); }
                catch (Exception callbackError) { Debug.LogException(callbackError); }
            }
        }
    }

    internal void Tick()
    {
        if (operation != null) events.Publish(FrameworkEvents.SceneProgressChangedId, operation);
    }

    private void Complete(AsyncOperation completed)
    {
        if (operation != completed) return;
        operation.completed -= Complete;
        var completion = pending;
        operation = null;
        pending = null;
        // Publish while no new request can overwrite the previous completion identity.
        events.Publish(FrameworkEvents.SceneProgressChangedId, completed);
        if (stopped) completion.TrySetCanceled();
        else completion.TrySetResult(true);
    }

    private void Validate(int sceneIndex)
    {
        if (stopped) throw new ObjectDisposedException(nameof(SceneMgr));
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            throw new ArgumentOutOfRangeException(nameof(sceneIndex), "Scene must be enabled in Build Settings.");
    }

    internal void Shutdown()
    {
        stopped = true;
        if (operation != null) operation.completed -= Complete;
        operation = null;
        var completion = pending;
        pending = null;
        completion?.TrySetCanceled(); // Unity's already-started scene load itself cannot be cancelled.
    }
}
