using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Feather.Tests
{
    public sealed class UIRegressionPanel : PanelBase
    {
        public bool Root;
        public bool ThrowOnHide;
        public bool ThrowOnDispose;
        public bool ThrowOnInit;
        public int InitCount;
        public int DisposeCount;
        public Action WhenHidden;
        public Action WhenDisposed;

        public override void OnInit()
        {
            InitCount++;
            IsRoot = Root;
            IsStackable = !Root;
            if (ThrowOnInit) throw new InvalidOperationException("init failure");
        }
        public override void OnShow() { }
        public override void OnHide()
        {
            WhenHidden?.Invoke();
            if (ThrowOnHide) throw new InvalidOperationException("hide failure");
        }
        public override void OnDispose()
        {
            DisposeCount++;
            WhenDisposed?.Invoke();
            if (ThrowOnDispose) throw new InvalidOperationException("dispose failure");
        }
    }

    [ExecuteAlways]
    public sealed class CanvasActivationProbe : MonoBehaviour
    {
        public static int EnableCount;
        private void OnEnable() => EnableCount++;
    }

    public class TypedTestPanel : PanelBase
    {
        public int Shows, Hides, Closes, Disposes, Inits, EnableCount;
        public string EnabledParent;
        private void OnEnable() { EnableCount++; EnabledParent = transform.parent.name; }
        public Action OnShowing, OnClosing, OnHiding;
        public bool FailInit;
        public bool UseTopLayer;
        public override UILayer Layer => UseTopLayer ? UILayer.Top : base.Layer;
        public override void OnInit() { Inits++; if (FailInit) throw new InvalidOperationException("typed init failure"); }
        public override void OnShow() { Shows++; OnShowing?.Invoke(); }
        public override void OnHide() { Hides++; OnHiding?.Invoke(); }
        public override void OnClose() { Closes++; OnClosing?.Invoke(); }
        public override void OnDispose() { Disposes++; }
    }
    public sealed class TestRoot : TypedTestPanel { public override UIType Type => UIType.Root; }
    public sealed class OtherRoot : TypedTestPanel { public override UIType Type => UIType.Root; }
    public sealed class TestPage : TypedTestPanel { public override UIType Type => UIType.Page; }
    public sealed class OtherPage : TypedTestPanel { public override UIType Type => UIType.Page; }
    public sealed class TestChild : TypedTestPanel { public override UIType Type => UIType.Child; }
    public sealed class UIMgrTests
    {
        private GameObject owner;
        private UIMgr ui;
        private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, List<UnityAction<GameObject>>> pending = new Dictionary<string, List<UnityAction<GameObject>>>();
        private readonly List<GameObject> objects = new List<GameObject>();

        private GameObject Create(string name, params Type[] components)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.SetActive(false);
            foreach (var component in components) obj.AddComponent(component);
            objects.Add(obj);
            return obj;
        }

        private GameObject CanvasPrefab(bool layers = true)
        {
            var canvas = Create("CanvasTemplate", typeof(Canvas));
            if (layers)
                foreach (string layer in new[] { "BottomLayer", "MiddleLayer", "TopLayer", "SystemLayer" })
                {
                    var child = Create(layer);
                    child.transform.SetParent(canvas.transform, false);
                    child.SetActive(true);
                }
            prefabs[UIMgr.uiPath + "UICanvas.prefab"] = canvas;
            return canvas;
        }

        private UIRegressionPanel PanelPrefab(string name, bool root = false)
        {
            var prefab = Create(name, typeof(UIRegressionPanel));
            var panel = prefab.GetComponent<UIRegressionPanel>();
            panel.Root = root;
            prefabs[UIMgr.uiPath + name + ".prefab"] = prefab;
            return panel;
        }

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("UITests");
            ui = owner.AddComponent<UIMgr>();
            ui.Initialize(path => prefabs.TryGetValue(path, out var prefab) ? prefab : null, (path, callback) =>
            {
                if (!pending.TryGetValue(path, out var callbacks)) pending[path] = callbacks = new List<UnityAction<GameObject>>();
                callbacks.Add(callback);
            });
            CanvasPrefab();
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null) Object.DestroyImmediate(owner);
            foreach (var obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
            prefabs.Clear();
            pending.Clear();
        }

        private void Complete(string name)
        {
            string path = UIMgr.uiPath + name + ".prefab";
            var callback = pending[path][0];
            pending[path].RemoveAt(0);
            callback(prefabs[path]);
        }

        private string[] VisiblePanels() => ui.UICanvas.GetComponentsInChildren<UIRegressionPanel>()
            .Select(panel => panel.name).OrderBy(name => name).ToArray();

        [Test]
        public void SameShortNameInDifferentNamespacesIsRejectedBeforeNavigation()
        {
            TypedPrefab<FeatherCollisionOne.Collision>();
            var opening = ui.OpenPage<FeatherCollisionOne.Collision>();
            Complete(nameof(FeatherCollisionOne.Collision));
            var original = opening.Result;
            Assert.Throws<InvalidOperationException>(() => ui.OpenPage<FeatherCollisionTwo.Collision>());
            Assert.Throws<InvalidOperationException>(() => ui.ShowUI<FeatherCollisionTwo.Collision>());
            Assert.That(original.IsVisible, Is.True);
        }

        [Test]
        public void ChildInheritsActualLegacyOwnerLayer()
        {
            TypedPrefab<TestPage>();
            ui.ShowUI<TestPage>(UILayer.Top);
            Assert.That(ui.TryGetUI<TestPage>(out var page), Is.True);
            var child = OpenChild(page.Handle);
            Assert.That(child.Panel.transform.parent, Is.SameAs(page.transform.parent));
            Assert.That(child.Panel.transform.GetSiblingIndex(), Is.GreaterThan(page.transform.GetSiblingIndex()));
        }

        [Test]
        public void ReopeningOwnerOnNewLayerMovesExistingChild()
        {
            var page = OpenPage();
            var child = OpenChild(page);
            ui.ShowUI<TestPage>(UILayer.Top);
            Assert.That(child.Panel.transform.parent, Is.SameAs(page.Panel.transform.parent));
            Assert.That(child.Panel.transform.GetSiblingIndex(), Is.GreaterThan(page.Panel.transform.GetSiblingIndex()));
        }

        [Test]
        public void ReentrantNavigationDoesNotReshowHiddenOwnersChild()
        {
            var page = OpenPage();
            var child = OpenChild(page);
            TypedPrefab<OtherPage>();
            var next = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            ((TypedTestPanel)page.Panel).OnShowing = () => ui.OpenPage<OtherPage>();
            ui.Close(next.Result);
            Assert.That(page.IsVisible, Is.False);
            Assert.That(child.IsVisible, Is.False);
            Assert.That(child.Panel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ChildOnShowNavigation_DoesNotRestoreRemainingSiblings()
        {
            var page = OpenPage();
            var first = OpenChild(page);
            var second = OpenChild(page);
            TypedPrefab<OtherPage>();
            var next = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            ((TypedTestPanel)first.Panel).OnShowing = () => ui.OpenPage<OtherPage>();
            ui.Close(next.Result);
            Assert.That(page.IsVisible, Is.False);
            Assert.That(first.IsVisible, Is.False);
            Assert.That(second.IsVisible, Is.False);
        }

        private T TypedPrefab<T>() where T : TypedTestPanel
        {
            var prefab = Create(typeof(T).Name, typeof(T));
            prefabs[UIMgr.uiPath + typeof(T).Name + ".prefab"] = prefab;
            return prefab.GetComponent<T>();
        }
        private PanelHandle OpenRoot()
        {
            TypedPrefab<TestRoot>();
            var task = ui.OpenRoot<TestRoot>();
            Complete(nameof(TestRoot));
            return task.GetAwaiter().GetResult();
        }
        private PanelHandle OpenPage()
        {
            TypedPrefab<TestPage>();
            var task = ui.OpenPage<TestPage>();
            Complete(nameof(TestPage));
            return task.GetAwaiter().GetResult();
        }
        private PanelHandle OpenChild(PanelHandle parent)
        {
            TypedPrefab<TestChild>();
            var task = ui.OpenChild<TestChild>(parent, "item");
            Complete(nameof(TestChild));
            return task.GetAwaiter().GetResult();
        }

        [Test]
        public void TypedNavigation_PreservesRootAndRestoresPageWithChild()
        {
            var root = OpenRoot();
            var page = OpenPage();
            var child = OpenChild(page);
            var panel = (TypedTestPanel)page.Panel;
            TypedPrefab<OtherPage>();
            var nextTask = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            Assert.That(root.IsVisible, Is.True);
            Assert.That(page.IsVisible, Is.False);
            Assert.That(child.IsVisible, Is.False);
            Assert.That(panel.Closes, Is.Zero);
            ui.Close(nextTask.Result);
            Assert.That(page.IsVisible, Is.True);
            Assert.That(child.IsVisible, Is.True);
            Assert.That(panel.Shows, Is.EqualTo(2));
            Assert.That(child.Panel.uiData, Is.EqualTo("item"));
            Assert.That(root.Panel.transform.parent.name, Is.EqualTo("BottomLayer"));
        }

        [Test]
        public void ChildDefaultLayer_FollowsOwnerAboveOrdinaryPages()
        {
            TypedPrefab<TestPage>().UseTopLayer = true;
            var task = ui.OpenPage<TestPage>();
            Complete(nameof(TestPage));
            var child = OpenChild(task.Result);
            Assert.That(child.Panel.transform.parent.name, Is.EqualTo("TopLayer"));
            Assert.That(child.Panel.transform.GetSiblingIndex(), Is.GreaterThan(task.Result.Panel.transform.GetSiblingIndex()));
        }
        [Test]
        public void ClosedChild_DoesNotReturnWhenOwnerResumes()
        {
            OpenRoot();
            var page = OpenPage();
            var child = OpenChild(page);
            var childPanel = (TypedTestPanel)child.Panel;
            ui.Close(child);
            TypedPrefab<OtherPage>();
            var next = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            ui.Close(next.Result);
            Assert.That(page.IsVisible, Is.True);
            Assert.That(child.IsOpen, Is.False);
            Assert.That(childPanel.Closes, Is.EqualTo(1));
            Assert.That(childPanel.Disposes, Is.EqualTo(1));
        }
        [Test]
        public void OwnerClose_CancelsPendingChildImmediately_AndIgnoresLateAsset()
        {
            var page = OpenPage();
            TypedPrefab<TestChild>();
            var task = ui.OpenChild<TestChild>(page);
            ui.Close(page);
            Assert.That(task.IsCanceled, Is.True);
            Complete(nameof(TestChild));
            Assert.That(owner.GetComponentsInChildren<TestChild>(true), Is.Empty);
        }
        [Test]
        public void ChildLoadedWhileOwnerSuspended_RemainsHiddenUntilReturn()
        {
            var page = OpenPage();
            TypedPrefab<TestChild>();
            TypedPrefab<OtherPage>();
            var child = ui.OpenChild<TestChild>(page);
            var next = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            Complete(nameof(TestChild));
            Assert.That(child.Result.IsVisible, Is.False);
            Assert.That(((TypedTestPanel)child.Result.Panel).Shows, Is.Zero);
            ui.Close(next.Result);
            Assert.That(child.Result.IsVisible, Is.True);
        }
        [Test]
        public void SameChildType_HasIndependentOwnerInstances()
        {
            var root = OpenRoot();
            var rootChild = OpenChild(root);
            var page = OpenPage();
            var pageChild = OpenChild(page);
            Assert.That(rootChild.Panel, Is.Not.SameAs(pageChild.Panel));
            Assert.That(ui.TryGetUI<TestChild>(out _), Is.False);
            ui.Close(page);
            Assert.That(rootChild.IsVisible, Is.True);
            Assert.That(pageChild.IsOpen, Is.False);
        }
        [Test]
        public void PageRequests_CommitInRequestOrder()
        {
            TypedPrefab<TestPage>();
            TypedPrefab<OtherPage>();
            var first = ui.OpenPage<TestPage>();
            var second = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            Assert.That(second.IsCompleted, Is.False);
            Complete(nameof(TestPage));
            Assert.That(first.IsCompleted, Is.True);
            Assert.That(second.Result.IsVisible, Is.True);
            Assert.That(first.Result.IsVisible, Is.False);
            ui.Back();
            Assert.That(first.Result.IsVisible, Is.True);
        }
        [Test]
        public void FailedPage_DoesNotHideCurrentPage()
        {
            var current = OpenPage();
            TypedPrefab<OtherPage>().FailInit = true;
            var failed = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            Assert.That(failed.IsFaulted, Is.True);
            Assert.That(failed.Exception.InnerException.Message, Does.Contain("typed init failure"));
            Assert.That(current.IsVisible, Is.True);
            Assert.That(owner.GetComponentsInChildren<OtherPage>(true), Is.Empty);
        }
        [Test]
        public void RootSwitch_ClosesOldFlowAndCancelsPendingPage()
        {
            var root = OpenRoot();
            var page = OpenPage();
            var child = OpenChild(page);
            TypedPrefab<OtherPage>();
            var delayed = ui.OpenPage<OtherPage>();
            TypedPrefab<OtherRoot>();
            var next = ui.OpenRoot<OtherRoot>();
            Complete(nameof(OtherRoot));
            Assert.That(root.IsOpen, Is.False);
            Assert.That(page.IsOpen, Is.False);
            Assert.That(child.IsOpen, Is.False);
            Assert.That(delayed.IsCanceled, Is.True);
            Complete(nameof(OtherPage));
            Assert.That(next.Result.IsVisible, Is.True);
            Assert.That(owner.GetComponentsInChildren<OtherPage>(true), Is.Empty);
        }
        [Test]
        public void NewRootRequest_CancelsPreviousRootBeforeAssetsComplete()
        {
            TypedPrefab<TestRoot>();
            TypedPrefab<OtherRoot>();
            var first = ui.OpenRoot<TestRoot>();
            var second = ui.OpenRoot<OtherRoot>();
            Assert.That(first.IsCanceled, Is.True);
            Complete(nameof(OtherRoot));
            Complete(nameof(TestRoot));
            Assert.That(second.Result.IsVisible, Is.True);
            Assert.That(owner.GetComponentsInChildren<TestRoot>(true), Is.Empty);
        }
        [Test]
        public void CloseAndReopen_UsesNewHandleAndCachedPanel()
        {
            var first = OpenPage();
            var panel = (TypedTestPanel)first.Panel;
            ui.Close(first);
            var second = ui.OpenPage<TestPage>().Result;
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Panel, Is.SameAs(panel));
            Assert.That(panel.Inits, Is.EqualTo(1));
            Assert.That(panel.Closes, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => ui.OpenChild<TestChild>(first));
            ui.Close(first);
            Assert.That(second.IsOpen, Is.True);
        }
        [Test]
        public void Back_ClosesChildBeforePage()
        {
            var page = OpenPage();
            var child = OpenChild(page);
            ui.Back();
            Assert.That(child.IsOpen, Is.False);
            Assert.That(page.IsVisible, Is.True);
            ui.Back();
            Assert.That(page.IsOpen, Is.False);
        }

        [Test]
        public void LegacyQueue_KeepsRootAndAdvancesAfterClosingPage()
        {
            var root = OpenRoot();
            TypedPrefab<TestPage>();
            TypedPrefab<OtherPage>();
            ui.ShowUIQueue(new UIInfo(nameof(TestPage)), new UIInfo(nameof(OtherPage)));
            Assert.That(root.IsVisible, Is.True);
            Assert.That(ui.TryGetUI<TestPage>(out var first), Is.True);
            ui.Close(first.Handle);
            Assert.That(ui.TryGetUI<OtherPage>(out var second), Is.True);
            Assert.That(second.Handle.IsVisible, Is.True);
            Assert.That(root.IsVisible, Is.True);
        }
        [Test]
        public void TypeMismatch_FaultsAndDisposesPartialInstance()
        {
            TypedPrefab<TestChild>();
            var task = ui.OpenPage<TestChild>();
            Complete(nameof(TestChild));
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.InnerException.Message, Does.Contain("expected Page"));
            Assert.That(owner.GetComponentsInChildren<TestChild>(true), Is.Empty);
        }
        [Test]
        public void InvalidOwner_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => ui.OpenChild<TestChild>(null));
            var root = OpenRoot();
            var child = OpenChild(root);
            Assert.Throws<ArgumentException>(() => ui.OpenChild<TestChild>(child));
            var otherObject = Create("OtherManager");
            var other = otherObject.AddComponent<UIMgr>();
            Assert.Throws<ArgumentException>(() => other.OpenChild<TestChild>(root));
        }
        [Test]
        public void Shutdown_CancelsTasksAndClosesCachedInstancesOnce()
        {
            var page = OpenPage();
            var panel = (TypedTestPanel)page.Panel;
            TypedPrefab<TestChild>();
            var pendingChild = ui.OpenChild<TestChild>(page);
            ui.Shutdown();
            ui.Shutdown();
            Assert.That(pendingChild.IsCanceled, Is.True);
            Assert.That(panel.Closes, Is.EqualTo(1));
            Assert.That(panel.Disposes, Is.EqualTo(1));
            Complete(nameof(TestChild));
        }
        [Test]
        public void ClosingChildBeforeParent_ProvidesDeterministicCleanupOrder()
        {
            var page = OpenPage();
            var child = OpenChild(page);
            var order = new List<string>();
            ((TypedTestPanel)page.Panel).OnClosing = () => order.Add("page");
            ((TypedTestPanel)child.Panel).OnClosing = () => order.Add("child");
            ui.Close(page);
            Assert.That(order, Is.EqualTo(new[] { "child", "page" }));
        }

        [Test]
        public void ShutdownDuringHide_ClosesBeforeFinalDisposal()
        {
            var page = OpenPage();
            var panel = (TypedTestPanel)page.Panel;
            panel.OnHiding = ui.Shutdown;
            ui.Close(page);
            Assert.That(panel.Closes, Is.EqualTo(1));
            Assert.That(panel.Disposes, Is.EqualTo(1));
        }

        [Test]
        public void CleanupCannotReopenTheInstanceBeingClosed()
        {
            var page = OpenPage();
            var panel = (TypedTestPanel)page.Panel;
            System.Threading.Tasks.Task<PanelHandle> reopened = null;
            panel.OnClosing = () => reopened = ui.OpenPage<TestPage>();
            ui.Close(page);
            Assert.That(reopened.IsCanceled, Is.True);
            Assert.That(panel.Handle, Is.Null);
            Assert.That(panel.Closes, Is.EqualTo(1));
        }

        [Test]
        public void ClosingDuringOnShow_CancelsOpenTask()
        {
            TypedPrefab<TestPage>();
            // OnShow can close itself through business code; use a cached instance to inject the callback.
            var page = ui.OpenPage<TestPage>();
            Complete(nameof(TestPage));
            var panel = (TypedTestPanel)page.Result.Panel;
            ui.Close(page.Result);
            panel.OnShowing = () => ui.Close(panel.Handle);
            var reopened = ui.OpenPage<TestPage>();
            Assert.That(reopened.IsCanceled, Is.True);
            Assert.That(panel.Handle, Is.Null);
        }

        [Test]
        public void MissingPageAsset_FaultsAndUnblocksLaterPage()
        {
            TypedPrefab<TestPage>();
            prefabs[UIMgr.uiPath + nameof(TestPage) + ".prefab"] = null;
            TypedPrefab<OtherPage>();
            var failed = ui.OpenPage<TestPage>();
            var next = ui.OpenPage<OtherPage>();
            Complete(nameof(OtherPage));
            Complete(nameof(TestPage));
            Assert.That(failed.IsFaulted, Is.True);
            Assert.That(failed.Exception.InnerException.Message, Does.Contain("prefab not found"));
            Assert.That(next.Result.IsVisible, Is.True);
        }

        [Test]
        public void FailedRootLoad_PreservesExistingFlow()
        {
            var root = OpenRoot();
            var page = OpenPage();
            TypedPrefab<OtherRoot>().FailInit = true;
            var next = ui.OpenRoot<OtherRoot>();
            Complete(nameof(OtherRoot));
            Assert.That(next.IsFaulted, Is.True);
            Assert.That(next.Exception.InnerException.Message, Does.Contain("typed init failure"));
            Assert.That(root.IsVisible, Is.True);
            Assert.That(page.IsVisible, Is.True);
        }

        [Test]
        public void RepeatedOpen_RefreshesDataWithoutDuplicatingPage()
        {
            var page = OpenPage();
            var panel = (TypedTestPanel)page.Panel;
            var repeated = ui.OpenPage<TestPage>("updated").Result;
            Assert.That(repeated, Is.SameAs(page));
            Assert.That(panel.Shows, Is.EqualTo(2));
            Assert.That(panel.uiData, Is.EqualTo("updated"));
            ui.Back();
            Assert.That(page.IsOpen, Is.False);
        }

        [Test]
        public void HideAll_InvalidatesDelayedOpens()
        {
            PanelPrefab("Popup");
            int completed = 0;
            ui.ShowUIAsync("Popup", onComplete: panel => completed++);
            ui.HideAllUI();
            Complete("Popup");
            Assert.That(VisiblePanels(), Is.Empty);
            Assert.That(completed, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LatestRootWins_RegardlessOfCompletionOrder(bool reverse)
        {
            PanelPrefab("OldRoot", true);
            PanelPrefab("NewRoot", true);
            ui.ShowUIAsync("OldRoot");
            ui.ShowUIAsync("NewRoot");
            Complete(reverse ? "NewRoot" : "OldRoot");
            Complete(reverse ? "OldRoot" : "NewRoot");
            Assert.That(VisiblePanels(), Is.EqualTo(new[] { "NewRoot" }));
        }

        [Test]
        public void RootCancelsOlderPopup_ButPreservesNewerPopupRequest()
        {
            PanelPrefab("OldPopup");
            PanelPrefab("Root", true);
            PanelPrefab("NewPopup");
            ui.ShowUIAsync("OldPopup");
            ui.ShowUIAsync("Root");
            ui.ShowUIAsync("NewPopup");
            Complete("Root");
            Complete("OldPopup");
            Complete("NewPopup");
            Assert.That(VisiblePanels(), Is.EqualTo(new[] { "NewPopup", "Root" }));
        }

        [Test]
        public void CachedRoot_InvalidatesOlderDelayedPopup()
        {
            PanelPrefab("Root", true);
            PanelPrefab("Popup");
            ui.ShowUI("Root");
            ui.ShowUIAsync("Popup");
            ui.ShowUIAsync("Root");
            Complete("Popup");
            Assert.That(VisiblePanels(), Is.EqualTo(new[] { "Root" }));
        }

        [Test]
        public void SyncOpenSupersedesAsyncOpen_AndInitializesOnce()
        {
            PanelPrefab("Popup");
            int oldCalls = 0;
            UIRegressionPanel shown = null;
            ui.ShowUIAsync("Popup", onComplete: panel => oldCalls++);
            ui.ShowUI("Popup", onComplete: panel => shown = (UIRegressionPanel)panel);
            Complete("Popup");
            ui.HideAllUI();
            ui.ShowUIAsync("Popup");
            Assert.That(shown.InitCount, Is.EqualTo(1));
            Assert.That(oldCalls, Is.Zero);
            Assert.That(VisiblePanels(), Is.EqualTo(new[] { "Popup" }));
        }

        [Test]
        public void Shutdown_IsolatesExceptionsAndDisposesEachPanelOnce()
        {
            PanelPrefab("Broken");
            PanelPrefab("Healthy");
            PanelPrefab("Late");
            UIRegressionPanel broken = null, healthy = null;
            ui.ShowUI("Broken", onComplete: panel => broken = (UIRegressionPanel)panel);
            ui.ShowUI("Healthy", onComplete: panel => healthy = (UIRegressionPanel)panel);
            broken.ThrowOnHide = true;
            broken.ThrowOnDispose = true;
            int lateCalls = 0;
            ui.ShowUIAsync("Late", onComplete: panel => lateCalls++);
            LogAssert.Expect(LogType.Exception, new Regex("hide failure"));
            LogAssert.Expect(LogType.Exception, new Regex("dispose failure"));
            ui.Shutdown();
            ui.Shutdown();
            Complete("Late");
            Assert.That(broken.DisposeCount, Is.EqualTo(1));
            Assert.That(healthy.DisposeCount, Is.EqualTo(1));
            Assert.That(lateCalls, Is.Zero);
        }

        [Test]
        public void ShutdownDuringOnHide_StillDisposesAllPanels()
        {
            PanelPrefab("First");
            PanelPrefab("Second");
            UIRegressionPanel first = null, second = null;
            ui.ShowUI("First", onComplete: panel => first = (UIRegressionPanel)panel);
            ui.ShowUI("Second", onComplete: panel => second = (UIRegressionPanel)panel);
            first.WhenHidden = ui.Shutdown;
            ui.HideAllUI();
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(second.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void DestroyCallback_CallsFinalPanelCleanup()
        {
            PanelPrefab("Popup");
            UIRegressionPanel shown = null;
            ui.ShowUI("Popup", onComplete: panel => shown = (UIRegressionPanel)panel);
            // EditMode does not dispatch runtime MonoBehaviour messages reliably; invoke the destruction hook.
            typeof(UIMgr).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            Object.DestroyImmediate(ui);
            Assert.That(shown.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void FailedOnInit_DestroysThePartialPanel()
        {
            PanelPrefab("Broken").ThrowOnInit = true;
            LogAssert.Expect(LogType.Exception, new Regex("init failure"));
            ui.ShowUI("Broken");
            Assert.That(owner.GetComponentsInChildren<UIRegressionPanel>(true), Is.Empty);
        }

        [TestCase("missing")]
        [TestCase("component")]
        [TestCase("layers")]
        public void InvalidCanvas_ThrowsAndCanBeRetried(string fault)
        {
            if (fault == "missing") prefabs.Remove(UIMgr.uiPath + "UICanvas.prefab");
            else if (fault == "component") prefabs[UIMgr.uiPath + "UICanvas.prefab"] = Create("InvalidCanvas");
            else CanvasPrefab(false);
            Assert.Throws<InvalidOperationException>(() => ui.CreateUICanvas(false));
            Assert.That(ui.UICanvas, Is.Null);
            Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
            CanvasPrefab();
            ui.CreateUICanvas(false);
            Assert.That(ui.UICanvas.gameObject.activeSelf, Is.False);
            ui.ActivateUICanvas();
            Assert.That(ui.UICanvas.gameObject.activeInHierarchy, Is.True);
        }

        [Test]
        public void CanvasComponents_AreNotEnabledDuringPreparation()
        {
            var prefab = CanvasPrefab();
            prefab.AddComponent<CanvasActivationProbe>();
            prefab.SetActive(true);
            CanvasActivationProbe.EnableCount = 0;
            ui.CreateUICanvas(false);
            Assert.That(CanvasActivationProbe.EnableCount, Is.Zero);
            ui.ActivateUICanvas();
            Assert.That(CanvasActivationProbe.EnableCount, Is.EqualTo(1));
        }

        [Test]
        public void HostCleanup_DisposesPanelsBeforeClearingEvents()
        {
            PanelPrefab("Popup");
            UIRegressionPanel panel = null;
            ui.ShowUI("Popup", onComplete: value => panel = (UIRegressionPanel)value);
            var events = new EventCenter();
            var disposed = new EventId("test-disposed");
            int notifications = 0;
            events.Subscribe(disposed, () => notifications++);
            panel.WhenDisposed = () => events.Publish(disposed);
            var inactiveHost = Create("Host");
            var host = inactiveHost.AddComponent<FrameworkHost>();
            typeof(FrameworkHost).GetField("uiMgr", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, ui);
            typeof(FrameworkHost).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, events);
            typeof(FrameworkHost).GetMethod("ShutdownServices", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(host, null);
            Assert.Throws<ObjectDisposedException>(() => events.Publish(disposed));
            Assert.That(panel.DisposeCount, Is.EqualTo(1));
            Assert.That(notifications, Is.EqualTo(1));
        }
    }
}






namespace FeatherCollisionOne { public sealed class Collision : Feather.Tests.TypedTestPanel { } }
namespace FeatherCollisionTwo { public sealed class Collision : Feather.Tests.TypedTestPanel { } }
