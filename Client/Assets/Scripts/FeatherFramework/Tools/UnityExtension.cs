using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Set type of cloned child GameObject's localPosition/Scale/Rotation.
/// </summary>
public enum TransformCloneType
{
    /// <summary>Set to same as Original. This is default of Add methods.</summary>
    KeepOriginal,
    /// <summary>Set to same as Parent.</summary>
    FollowParent,
    /// <summary>Set to Position = zero, Scale = one, Rotation = identity.</summary>
    Origin,
    /// <summary>Position/Scale/Rotation as is.</summary>
    DoNothing
}

/// <summary>
/// Set type of moved child GameObject's localPosition/Scale/Rotation.
/// </summary>
public enum TransformMoveType
{
    /// <summary>Set to same as Parent.</summary>
    FollowParent,
    /// <summary>Set to Position = zero, Scale = one, Rotation = identity.</summary>
    Origin,
    /// <summary>Position/Scale/Rotation as is.</summary>
    DoNothing
}



public static class UnityExtension
{
    public static GameObject Show(this GameObject self)
    {
        self.gameObject.SetActive(true);
        return self;
    }

    public static GameObject Hide(this GameObject self)
    {
        self.gameObject.SetActive(false);
        return self;
    }

    public static T SetAlpha<T>(this T self,float a) where T : Graphic
    {
        self.color = new Color(self.color.r, self.color.g, self.color.b, a);
        return self;
    }

    public static void SetPosX<T>(this T self, float x) where T : Component
    {
        self.transform.position = new Vector3(x,self.transform.position.y, self.transform.position.z);
    }

    public static void SetPosY<T>(this T self, float y) where T : Component
    {
        self.transform.position = new Vector3(self.transform.position.x, y, self.transform.position.z);
    }

    public static void SetPosZ<T>(this T self, float z) where T : Component
    {
        self.transform.position = new Vector3(self.transform.position.x, self.transform.position.y, z);
    }

    public static void SetLocalPosX<T>(this T self, float x) where T : Component
    {
        self.transform.localPosition = new Vector3(x, self.transform.localPosition.y, self.transform.localPosition.z);
    }

    public static void SetLocalPosY<T>(this T self, float y) where T : Component
    {
        self.transform.localPosition = new Vector3(self.transform.localPosition.x, y, self.transform.localPosition.z);
    }

    public static void SetLocalPosZ<T>(this T self, float z) where T : Component
    {
        self.transform.localPosition = new Vector3(self.transform.localPosition.x, self.transform.localPosition.y, z);
    }

    public static void SetLocalScaleX<T>(this T self, float x) where T : Component
    {
        self.transform.localScale = new Vector3(x, self.transform.localScale.y, self.transform.localScale.z);
    }

    public static void SetLocalScaleY<T>(this T self, float y) where T : Component
    {
        self.transform.localScale = new Vector3(self.transform.localScale.x, y, self.transform.localScale.z);
    }

    public static void SetLocalScaleZ<T>(this T self, float z) where T : Component
    {
        self.transform.localScale = new Vector3(self.transform.localScale.x, self.transform.localScale.y, z);
    }



    //Title:GameObjectExtensions.Operate



    static UnityEngine.GameObject GetGameObject<T>(T obj)
            where T : UnityEngine.Object
    {
        var gameObject = obj as GameObject;
        if (gameObject == null)
        {
            var component = obj as Component;
            if (component == null)
            {
                return null;
            }

            gameObject = component.gameObject;
        }

        return gameObject;
    }

    #region Add

    /// <summary>
    /// <para>将目标对象的克隆体设为当前游戏对象的子物体</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginal">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T Add<T>(this GameObject parent, T childOriginal, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        if (parent == null) throw new ArgumentNullException("parent");
        if (childOriginal == null) throw new ArgumentNullException("childOriginal");

        var child = UnityEngine.Object.Instantiate(childOriginal);

        var childGameObject = GetGameObject(child);

        // for uGUI, should use SetParent(parent, false)
        var childTransform = childGameObject.transform;
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
        var rectTransform = childTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.SetParent(parent.transform, worldPositionStays: false);
        }
        else
        {
#endif
            var parentTransform = parent.transform;
            childTransform.parent = parentTransform;
            switch (cloneType)
            {
                case TransformCloneType.FollowParent:
                    childTransform.localPosition = parentTransform.localPosition;
                    childTransform.localScale = parentTransform.localScale;
                    childTransform.localRotation = parentTransform.localRotation;
                    break;
                case TransformCloneType.Origin:
                    childTransform.localPosition = Vector3.zero;
                    childTransform.localScale = Vector3.one;
                    childTransform.localRotation = Quaternion.identity;
                    break;
                case TransformCloneType.KeepOriginal:
                    var co = GetGameObject(childOriginal);
                    var childOriginalTransform = co.transform;
                    childTransform.localPosition = childOriginalTransform.localPosition;
                    childTransform.localScale = childOriginalTransform.localScale;
                    childTransform.localRotation = childOriginalTransform.localRotation;
                    break;
                case TransformCloneType.DoNothing:
                default:
                    break;
            }
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
        }
#endif
        if (setLayer)
        {
            childGameObject.layer = parent.layer;
        }

        if (setActive != null)
        {
            childGameObject.SetActive(setActive.Value);
        }
        if (specifiedName != null)
        {
            child.name = specifiedName;
        }

        return child;
    }

    /// <summary>
    /// <para>将目标对象的克隆体设为当前游戏对象的子物体（批量）</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginals">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    public static T[] AddRange<T>(this GameObject parent, IEnumerable<T> childOriginals, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        if (parent == null) throw new ArgumentNullException("parent");
        if (childOriginals == null) throw new ArgumentNullException("childOriginals");

        // iteration optimize
        {
            var array = childOriginals as T[];
            if (array != null)
            {
                var result = new T[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    var child = Add(parent, array[i], cloneType, setActive, specifiedName, setLayer);
                    result[i] = child;
                }
                return result;
            }
        }

        {
            var iterList = childOriginals as IList<T>;
            if (iterList != null)
            {
                var result = new T[iterList.Count];
                for (int i = 0; i < iterList.Count; i++)
                {
                    var child = Add(parent, iterList[i], cloneType, setActive, specifiedName, setLayer);
                    result[i] = child;
                }
                return result;
            }
        }

        {
            var result = new List<T>();
            foreach (var childOriginal in childOriginals)
            {
                var child = Add(parent, childOriginal, cloneType, setActive, specifiedName, setLayer);
                result.Add(child);
            }

            return result.ToArray();
        }
    }

    /// <summary>
    /// <para>将目标对象的克隆体设为当前游戏对象的第一个子物体</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginal">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>      
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T AddFirst<T>(this GameObject parent, T childOriginal, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var child = Add(parent, childOriginal, cloneType, setActive, specifiedName, setLayer);
        var go = GetGameObject(child);
        if (go == null) return child;

        go.transform.SetAsFirstSibling();
        return child;
    }

    /// <summary>
    /// <para>将目标对象的克隆体设为当前游戏对象的第一个子物体（批量）</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginals">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>       
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T[] AddFirstRange<T>(this GameObject parent, IEnumerable<T> childOriginals, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var child = AddRange(parent, childOriginals, cloneType, setActive, specifiedName, setLayer);
        for (int i = child.Length - 1; i >= 0; i--)
        {
            var go = GetGameObject(child[i]);
            if (go == null) continue;
            go.transform.SetAsFirstSibling();
        }
        return child;
    }

    /// <summary>
    /// <para>将目标对象的克隆体创建在当前游戏对象之前</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginal">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>     
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T AddBeforeSelf<T>(this GameObject parent, T childOriginal, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex();

        var child = Add(root, childOriginal, cloneType, setActive, specifiedName, setLayer);

        var go = GetGameObject(child);
        if (go == null) return child;

        go.transform.SetSiblingIndex(sibilingIndex);
        return child;
    }

    /// <summary>
    /// <para>将目标对象的克隆体创建在当前游戏对象之前（批量）</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginals">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>     
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T[] AddBeforeSelfRange<T>(this GameObject parent, IEnumerable<T> childOriginals, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex();
        var child = AddRange(root, childOriginals, cloneType, setActive, specifiedName, setLayer);
        for (int i = child.Length - 1; i >= 0; i--)
        {
            var go = GetGameObject(child[i]);
            if (go == null) continue;
            go.transform.SetSiblingIndex(sibilingIndex);
        }

        return child;
    }

    /// <summary>
    /// <para>将目标对象的克隆体创建在当前游戏对象之后</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginal">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>     
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T AddAfterSelf<T>(this GameObject parent, T childOriginal, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex() + 1;
        var child = Add(root, childOriginal, cloneType, setActive, specifiedName, setLayer);
        var go = GetGameObject(child);
        if (go == null) return child;

        go.transform.SetSiblingIndex(sibilingIndex);
        return child;
    }

    /// <summary>
    /// <para>将目标对象的克隆体创建在当前游戏对象之后（批量）</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childOriginals">Clone Target.</param>
    /// <param name="cloneType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>     
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="specifiedName">Set name of child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T[] AddAfterSelfRange<T>(this GameObject parent, IEnumerable<T> childOriginals, TransformCloneType cloneType = TransformCloneType.KeepOriginal, bool? setActive = null, string specifiedName = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex() + 1;
        var child = AddRange(root, childOriginals, cloneType, setActive, specifiedName, setLayer);
        for (int i = child.Length - 1; i >= 0; i--)
        {
            var go = GetGameObject(child[i]);
            if (go == null) continue;
            go.transform.SetSiblingIndex(sibilingIndex);
        }

        return child;
    }

    #endregion

    #region Move

    /// <summary>
    /// <para>将目标对象设为当前游戏对象的最后一个子物体（其他的参考add）</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="child">Target.</param>
    /// <param name="moveType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>      
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T MoveToLast<T>(this GameObject parent, T child, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        if (parent == null) throw new ArgumentNullException("parent");
        if (child == null) throw new ArgumentNullException("child");

        var childGameObject = GetGameObject(child);
        if (child == null) return child;

        // for uGUI, should use SetParent(parent, false)
        var childTransform = childGameObject.transform;
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
        var rectTransform = childTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.SetParent(parent.transform, worldPositionStays: false);
        }
        else
        {
#endif
            var parentTransform = parent.transform;
            childTransform.parent = parentTransform;
            switch (moveType)
            {
                case TransformMoveType.FollowParent:
                    childTransform.localPosition = parentTransform.localPosition;
                    childTransform.localScale = parentTransform.localScale;
                    childTransform.localRotation = parentTransform.localRotation;
                    break;
                case TransformMoveType.Origin:
                    childTransform.localPosition = Vector3.zero;
                    childTransform.localScale = Vector3.one;
                    childTransform.localRotation = Quaternion.identity;
                    break;
                case TransformMoveType.DoNothing:
                default:
                    break;
            }
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
        }
#endif
        if (setLayer)
        {
            childGameObject.layer = parent.layer;
        }

        if (setActive != null)
        {
            childGameObject.SetActive(setActive.Value);
        }

        return child;
    }

    /// <summary>
    /// <para>Move the GameObject/Component as children of this GameObject.</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childs">Target.</param>
    /// <param name="moveType">Choose set type of moved child GameObject's localPosition/Scale/Rotation.</param>
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T[] MoveToLastRange<T>(this GameObject parent, IEnumerable<T> childs, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        if (parent == null) throw new ArgumentNullException("parent");
        if (childs == null) throw new ArgumentNullException("childs");

        // iteration optimize
        {
            var array = childs as T[];
            if (array != null)
            {
                var result = new T[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    var child = MoveToLast(parent, array[i], moveType, setActive, setLayer);
                    result[i] = child;
                }
                return result;
            }
        }

        {
            var iterList = childs as IList<T>;
            if (iterList != null)
            {
                var result = new T[iterList.Count];
                for (int i = 0; i < iterList.Count; i++)
                {
                    var child = MoveToLast(parent, iterList[i], moveType, setActive, setLayer);
                    result[i] = child;
                }
                return result;
            }
        }
        {
            var result = new List<T>();
            foreach (var childOriginal in childs)
            {
                var child = MoveToLast(parent, childOriginal, moveType, setActive, setLayer);
                result.Add(child);
            }

            return result.ToArray();
        }
    }

    /// <summary>
    /// <para>Move the GameObject/Component as the first children of this GameObject.</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="child">Target.</param>
    /// <param name="moveType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>      
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T MoveToFirst<T>(this GameObject parent, T child, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        MoveToLast(parent, child, moveType, setActive, setLayer);
        var go = GetGameObject(child);
        if (go == null) return child;

        go.transform.SetAsFirstSibling();
        return child;
    }

    /// <summary>
    /// <para>Move the GameObject/Component as the first children of this GameObject.</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childs">Target.</param>
    /// <param name="moveType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>       
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T[] MoveToFirstRange<T>(this GameObject parent, IEnumerable<T> childs, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var child = MoveToLastRange(parent, childs, moveType, setActive, setLayer);
        for (int i = child.Length - 1; i >= 0; i--)
        {
            var go = GetGameObject(child[i]);
            if (go == null) continue;

            go.transform.SetAsFirstSibling();
        }
        return child;
    }

    /// <summary>
    /// <para>Move the GameObject/Component before this GameObject.</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="child">Target.</param>
    /// <param name="moveType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>      
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T MoveToBeforeSelf<T>(this GameObject parent, T child, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex();

        MoveToLast(root, child, moveType, setActive, setLayer);
        var go = GetGameObject(child);
        if (go == null) return child;

        go.transform.SetSiblingIndex(sibilingIndex);
        return child;
    }

    /// <summary>
    /// <para>Move the GameObject/Component before GameObject.</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childs">Target.</param>
    /// <param name="moveType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>       
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T[] MoveToBeforeSelfRange<T>(this GameObject parent, IEnumerable<T> childs, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex();
        var child = MoveToLastRange(root, childs, moveType, setActive, setLayer);
        for (int i = child.Length - 1; i >= 0; i--)
        {
            var go = GetGameObject(child[i]);
            if (go == null) continue;

            go.transform.SetSiblingIndex(sibilingIndex);
        }

        return child;
    }

    /// <summary>
    /// <para>Move the GameObject/Component after this GameObject.</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="child">Target.</param>
    /// <param name="moveType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>      
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T MoveToAfterSelf<T>(this GameObject parent, T child, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex() + 1;
        MoveToLast(root, child, moveType, setActive, setLayer);
        var go = GetGameObject(child);
        if (go == null) return child;

        go.transform.SetSiblingIndex(sibilingIndex);
        return child;
    }

    /// <summary>
    /// <para>Move the GameObject/Component after this GameObject.</para>
    /// </summary>
    /// <param name="parent">Parent GameObject.</param>
    /// <param name="childs">Target.</param>
    /// <param name="moveType">Choose set type of cloned child GameObject's localPosition/Scale/Rotation.</param>       
    /// <param name="setActive">Set activates/deactivates child GameObject. If null, doesn't set specified value.</param>
    /// <param name="setLayer">Set layer of child GameObject same with parent.</param>
    public static T[] MoveToAfterSelfRange<T>(this GameObject parent, IEnumerable<T> childs, TransformMoveType moveType = TransformMoveType.DoNothing, bool? setActive = null, bool setLayer = false)
        where T : UnityEngine.Object
    {
        var root = parent.Parent();
        if (root == null) throw new InvalidOperationException("The parent root is null");

        var sibilingIndex = parent.transform.GetSiblingIndex() + 1;
        var child = MoveToLastRange(root, childs, moveType, setActive, setLayer);
        for (int i = child.Length - 1; i >= 0; i--)
        {
            var go = GetGameObject(child[i]);
            if (go == null) continue;

            go.transform.SetSiblingIndex(sibilingIndex);
        }

        return child;
    }

    #endregion

    /// <summary>安全的销毁游戏对象（有防空处理，可批量）</summary>
    /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
    /// <param name="detachParent">set to parent = null.</param>
    public static void Destroy(this GameObject self, bool useDestroyImmediate = false, bool detachParent = false)
    {
        if (self == null) return;

        if (detachParent)
        {
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
            self.transform.SetParent(null);
#else
                self.transform.parent = null;
#endif
        }

        if (useDestroyImmediate)
        {
            GameObject.DestroyImmediate(self);
        }
        else
        {
            GameObject.Destroy(self);
        }
    }



    //Title:GameObjectExtensions.Enumerable



    /// <summary>Returns a collection of GameObjects that contains the ancestors of every GameObject in the source collection.</summary>
    public static IEnumerable<GameObject> Ancestors(this IEnumerable<GameObject> source)
    {
        foreach (var item in source)
        {
            var e = item.Ancestors().GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }

    /// <summary>Returns a collection of GameObjects that contains every GameObject in the source collection, and the ancestors of every GameObject in the source collection.</summary>
    public static IEnumerable<GameObject> AncestorsAndSelf(this IEnumerable<GameObject> source)
    {
        foreach (var item in source)
        {
            var e = item.AncestorsAndSelf().GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }

    /// <summary>Returns a collection of GameObjects that contains the descendant GameObjects of every GameObject in the source collection.</summary>
    public static IEnumerable<GameObject> Descendants(this IEnumerable<GameObject> source, Func<Transform, bool> descendIntoChildren = null)
    {
        foreach (var item in source)
        {
            var e = item.Descendants(descendIntoChildren).GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }

    /// <summary>Returns a collection of GameObjects that contains every GameObject in the source collection, and the descendent GameObjects of every GameObject in the source collection.</summary>
    public static IEnumerable<GameObject> DescendantsAndSelf(this IEnumerable<GameObject> source, Func<Transform, bool> descendIntoChildren = null)
    {
        foreach (var item in source)
        {
            var e = item.DescendantsAndSelf(descendIntoChildren).GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }

    /// <summary>返回当前游戏对象的所有子物体（只包含下一层级，不是所有）</summary>
    public static IEnumerable<GameObject> Children(this IEnumerable<GameObject> source)
    {
        foreach (var item in source)
        {
            var e = item.Children().GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }

    /// <summary>返回当前游戏对象的所有子物体和本身（只包含下一层级，不是所有）</summary>
    public static IEnumerable<GameObject> ChildrenAndSelf(this IEnumerable<GameObject> source)
    {
        foreach (var item in source)
        {
            var e = item.ChildrenAndSelf().GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }

    /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
    /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
    /// <param name="detachParent">set to parent = null.</param>
    public static void Destroy(this IEnumerable<GameObject> source, bool useDestroyImmediate = false, bool detachParent = false)
    {
        if (detachParent)
        {
            var l = new List<GameObject>(source); // avoid halloween problem
            var e = l.GetEnumerator(); // get struct enumerator for avoid unity's compiler bug(avoid boxing)
            while (e.MoveNext())
            {
                e.Current.Destroy(useDestroyImmediate, true);
            }
        }
        else
        {
            foreach (var item in source)
            {
                item.Destroy(useDestroyImmediate, false); // doesn't detach.
            }
        }
    }

    /// <summary>Returns a collection of specified component in the source collection.</summary>
    public static IEnumerable<T> OfComponent<T>(this IEnumerable<GameObject> source)
        where T : UnityEngine.Component
    {
        foreach (var item in source)
        {
#if UNITY_EDITOR
            var cache = ComponentCache<T>.Instance;
            item.GetComponents<T>(cache);
            if (cache.Count != 0)
            {
                var component = cache[0];
                cache.Clear();
                yield return component;
            }
#else
                        
                var component = item.GetComponent<T>();
                if (component != null)
                {
                    yield return component;
                }
#endif
        }
    }


#if UNITY_EDITOR
    class ComponentCache<T>
    {
        public static readonly List<T> Instance = new List<T>(); // for no allocate on UNITY_EDITOR
    }
#endif

    /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
    public static int ToArrayNonAlloc<T>(this IEnumerable<T> source, ref T[] array)
    {
        var index = 0;
        foreach (var item in source)
        {
            if (array.Length == index)
            {
                var newSize = (index == 0) ? 4 : index * 2;
                Array.Resize(ref array, newSize);
            }
            array[index++] = item;
        }

        return index;
    }



    //Title:GameObjectExtensions.Traverse
    //Traverse Game Objects, based on Axis(Parent, Child, Children, Ancestors/Descendants, BeforeSelf/BeforeAfter)



    /// <summary>获取当前游戏对象的父物体，如果没有则返回null</summary>
    public static GameObject Parent(this GameObject origin)
    {
        if (origin == null) return null;

        var parentTransform = origin.transform.parent;
        if (parentTransform == null) return null;

        return parentTransform.gameObject;
    }

    /// <summary>返回当前游戏对象第一个符合指定名称的子物体，如果没有则返回null</summary>
    public static GameObject Child(this GameObject origin, string name)
    {
        if (origin == null) return null;

        var child = origin.transform.Find(name); // transform.find can get inactive object
        if (child == null) return null;
        return child.gameObject;
    }

    /// <summary>Returns a collection of the child GameObjects.</summary>
    public static ChildrenEnumerable Children(this GameObject origin)
    {
        return new ChildrenEnumerable(origin, false);
    }

    /// <summary>Returns a collection of GameObjects that contain this GameObject, and the child GameObjects.</summary>
    public static ChildrenEnumerable ChildrenAndSelf(this GameObject origin)
    {
        return new ChildrenEnumerable(origin, true);
    }

    /// <summary>返回当前游戏对象的所有父物体</summary>
    public static AncestorsEnumerable Ancestors(this GameObject origin)
    {
        return new AncestorsEnumerable(origin, false);
    }

    /// <summary>返回当前游戏对象的所有父物体还有本身</summary>
    public static AncestorsEnumerable AncestorsAndSelf(this GameObject origin)
    {
        return new AncestorsEnumerable(origin, true);
    }

    /// <summary>返回该游戏对象的所有子物体（所有层级）</summary>
    public static DescendantsEnumerable Descendants(this GameObject origin, Func<Transform, bool> descendIntoChildren = null)
    {
        return new DescendantsEnumerable(origin, false, descendIntoChildren);
    }

    /// <summary>返回该游戏对象的所有子物体和本身（所有层级）</summary>
    public static DescendantsEnumerable DescendantsAndSelf(this GameObject origin, Func<Transform, bool> descendIntoChildren = null)
    {
        return new DescendantsEnumerable(origin, true, descendIntoChildren);
    }

    /// <summary>返回所有sibling在该游戏对象之前的对象</summary>
    public static BeforeSelfEnumerable BeforeSelf(this GameObject origin)
    {
        return new BeforeSelfEnumerable(origin, false);
    }

    /// <summary>返回该游戏对象和所有sibling在该游戏对象之前的对象</summary>
    public static BeforeSelfEnumerable BeforeSelfAndSelf(this GameObject origin)
    {
        return new BeforeSelfEnumerable(origin, true);
    }

    /// <summary>返回所有sibling在该游戏对象之后的对象</summary>
    public static AfterSelfEnumerable AfterSelf(this GameObject origin)
    {
        return new AfterSelfEnumerable(origin, false);
    }

    /// <summary>返回该游戏对象和所有sibling在该游戏对象之后的对象</summary>
    public static AfterSelfEnumerable AfterSelfAndSelf(this GameObject origin)
    {
        return new AfterSelfEnumerable(origin, true);
    }

    // Implements hand struct enumerator.

    public struct ChildrenEnumerable : IEnumerable<GameObject>
    {
        readonly GameObject origin;
        readonly bool withSelf;

        public ChildrenEnumerable(GameObject origin, bool withSelf)
        {
            this.origin = origin;
            this.withSelf = withSelf;
        }

        /// <summary>返回包含指定组件的集合</summary>
        public OfComponentEnumerable<T> OfComponent<T>()
            where T : Component
        {
            return new OfComponentEnumerable<T>(ref this);
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        /// <param name="detachParent">set to parent = null.</param>
        public void Destroy(bool useDestroyImmediate = false, bool detachParent = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.Destroy(useDestroyImmediate, false);
            }
            if (detachParent)
            {
                origin.transform.DetachChildren();
                if (withSelf)
                {
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
                    origin.transform.SetParent(null);
#else
                        origin.transform.parent = null;
#endif
                }
            }
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(Func<GameObject, bool> predicate, bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current;
                if (predicate(item))
                {
                    item.Destroy(useDestroyImmediate, false);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            // check GameObject is destroyed only on GetEnumerator timing
            return (origin == null)
                ? new Enumerator(null, withSelf, false)
                : new Enumerator(origin.transform, withSelf, true);
        }

        IEnumerator<GameObject> IEnumerable<GameObject>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region LINQ

        int GetChildrenSize()
        {
            return origin.transform.childCount + (withSelf ? 1 : 0);
        }

        public void ForEach(Action<GameObject> action)
        {
            var e = this.GetEnumerator();
            while (e.MoveNext())
            {
                action(e.Current);
            }
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(ref GameObject[] array)
        {
            var index = 0;

            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? GetChildrenSize() : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(Func<GameObject, bool> filter, ref GameObject[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? GetChildrenSize() : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? GetChildrenSize() : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? GetChildrenSize() : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                var state = let(item);

                if (!filter(state)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? GetChildrenSize() : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(state);
            }

            return index;
        }

        public GameObject[] ToArray()
        {
            var array = new GameObject[GetChildrenSize()];
            var len = ToArrayNonAlloc(ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject[] ToArray(Func<GameObject, bool> filter)
        {
            var array = new GameObject[GetChildrenSize()];
            var len = ToArrayNonAlloc(filter, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, T> selector)
        {
            var array = new T[GetChildrenSize()];
            var len = ToArrayNonAlloc<T>(selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector)
        {
            var array = new T[GetChildrenSize()];
            var len = ToArrayNonAlloc(filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector)
        {
            var array = new T[GetChildrenSize()];
            var len = ToArrayNonAlloc(let, filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject First()
        {
            var e = this.GetEnumerator();
            if (e.MoveNext())
            {
                return e.Current;
            }
            else
            {
                throw new InvalidOperationException("sequence is empty.");
            }
        }

        public GameObject FirstOrDefault()
        {
            var e = this.GetEnumerator();
            return (e.MoveNext())
                ? e.Current
                : null;
        }

        #endregion

        public struct Enumerator : IEnumerator<GameObject>
        {
            readonly int childCount; // childCount is fixed when GetEnumerator is called.

            readonly Transform originTransform;
            readonly bool canRun;

            bool withSelf;
            int currentIndex;
            GameObject current;

            internal Enumerator(Transform originTransform, bool withSelf, bool canRun)
            {
                this.originTransform = originTransform;
                this.withSelf = withSelf;
                this.childCount = canRun ? originTransform.childCount : 0;
                this.currentIndex = -1;
                this.canRun = canRun;
                this.current = null;
            }

            public bool MoveNext()
            {
                if (!canRun) return false;

                if (withSelf)
                {
                    current = originTransform.gameObject;
                    withSelf = false;
                    return true;
                }

                currentIndex++;
                if (currentIndex < childCount)
                {
                    var child = originTransform.GetChild(currentIndex);
                    current = child.gameObject;
                    return true;
                }

                return false;
            }

            public GameObject Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }

        public struct OfComponentEnumerable<T> : IEnumerable<T>
            where T : Component
        {
            ChildrenEnumerable parent;

            public OfComponentEnumerable(ref ChildrenEnumerable parent)
            {
                this.parent = parent;
            }

            public OfComponentEnumerator<T> GetEnumerator()
            {
                return new OfComponentEnumerator<T>(ref this.parent);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region LINQ

            public void ForEach(Action<T> action)
            {
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    action(e.Current);
                }
            }

            public T First()
            {
                var e = this.GetEnumerator();
                if (e.MoveNext())
                {
                    return e.Current;
                }
                else
                {
                    throw new InvalidOperationException("sequence is empty.");
                }
            }

            public T FirstOrDefault()
            {
                var e = this.GetEnumerator();
                return (e.MoveNext())
                    ? e.Current
                    : null;
            }

            public T[] ToArray()
            {
                var array = new T[parent.GetChildrenSize()];
                var len = ToArrayNonAlloc(ref array);
                if (array.Length != len)
                {
                    Array.Resize(ref array, len);
                }
                return array;
            }

            /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
            public int ToArrayNonAlloc(ref T[] array)
            {
                var index = 0;
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    if (array.Length == index)
                    {
                        var newSize = (index == 0) ? parent.GetChildrenSize() : index * 2;
                        Array.Resize(ref array, newSize);
                    }
                    array[index++] = e.Current;
                }

                return index;
            }

            #endregion
        }

        public struct OfComponentEnumerator<T> : IEnumerator<T>
            where T : Component
        {
            Enumerator enumerator; // enumerator is mutable
            T current;

#if UNITY_EDITOR
            static List<T> componentCache = new List<T>(); // for no allocate on UNITY_EDITOR
#endif

            public OfComponentEnumerator(ref ChildrenEnumerable parent)
            {
                this.enumerator = parent.GetEnumerator();
                this.current = default(T);
            }

            public bool MoveNext()
            {
                while (enumerator.MoveNext())
                {
#if UNITY_EDITOR
                    enumerator.Current.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        current = componentCache[0];
                        componentCache.Clear();
                        return true;
                    }
#else
                        
                        var component = enumerator.Current.GetComponent<T>();
                        if (component != null)
                        {
                            current = component;
                            return true;
                        }
#endif
                }

                return false;
            }

            public T Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }
    }

    public struct AncestorsEnumerable : IEnumerable<GameObject>
    {
        readonly GameObject origin;
        readonly bool withSelf;

        public AncestorsEnumerable(GameObject origin, bool withSelf)
        {
            this.origin = origin;
            this.withSelf = withSelf;
        }

        /// <summary>Returns a collection of specified component in the source collection.</summary>
        public OfComponentEnumerable<T> OfComponent<T>()
            where T : Component
        {
            return new OfComponentEnumerable<T>(ref this);
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.Destroy(useDestroyImmediate, false);
            }
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(Func<GameObject, bool> predicate, bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current;
                if (predicate(item))
                {
                    item.Destroy(useDestroyImmediate, false);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            // check GameObject is destroyed only on GetEnumerator timing
            return (origin == null)
                ? new Enumerator(null, null, withSelf, false)
                : new Enumerator(origin, origin.transform, withSelf, true);
        }

        IEnumerator<GameObject> IEnumerable<GameObject>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region LINQ

        public void ForEach(Action<GameObject> action)
        {
            var e = this.GetEnumerator();
            while (e.MoveNext())
            {
                action(e.Current);
            }
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(ref GameObject[] array)
        {
            var index = 0;

            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(Func<GameObject, bool> filter, ref GameObject[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                var state = let(item);

                if (!filter(state)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(state);
            }

            return index;
        }

        public GameObject[] ToArray()
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject[] ToArray(Func<GameObject, bool> filter)
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(filter, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc<T>(selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(let, filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject First()
        {
            var e = this.GetEnumerator();
            if (e.MoveNext())
            {
                return e.Current;
            }
            else
            {
                throw new InvalidOperationException("sequence is empty.");
            }
        }

        public GameObject FirstOrDefault()
        {
            var e = this.GetEnumerator();
            return (e.MoveNext())
                ? e.Current
                : null;
        }

        #endregion

        public struct Enumerator : IEnumerator<GameObject>
        {
            readonly bool canRun;

            GameObject current;
            Transform currentTransform;
            bool withSelf;

            internal Enumerator(GameObject origin, Transform originTransform, bool withSelf, bool canRun)
            {
                this.current = origin;
                this.currentTransform = originTransform;
                this.withSelf = withSelf;
                this.canRun = canRun;
            }

            public bool MoveNext()
            {
                if (!canRun) return false;

                if (withSelf)
                {
                    // withSelf, use origin and originTransform
                    withSelf = false;
                    return true;
                }

                var parentTransform = currentTransform.parent;
                if (parentTransform != null)
                {
                    current = parentTransform.gameObject;
                    currentTransform = parentTransform;
                    return true;
                }

                return false;
            }

            public GameObject Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }

        public struct OfComponentEnumerable<T> : IEnumerable<T>
            where T : Component
        {
            AncestorsEnumerable parent;

            public OfComponentEnumerable(ref AncestorsEnumerable parent)
            {
                this.parent = parent;
            }

            public OfComponentEnumerator<T> GetEnumerator()
            {
                return new OfComponentEnumerator<T>(ref parent);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region LINQ

            public void ForEach(Action<T> action)
            {
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    action(e.Current);
                }
            }

            public T First()
            {
                var e = this.GetEnumerator();
                if (e.MoveNext())
                {
                    return e.Current;
                }
                else
                {
                    throw new InvalidOperationException("sequence is empty.");
                }
            }

            public T FirstOrDefault()
            {
                var e = this.GetEnumerator();
                return (e.MoveNext())
                    ? e.Current
                    : null;
            }

            public T[] ToArray()
            {
                var array = new T[4];
                var len = ToArrayNonAlloc(ref array);
                if (array.Length != len)
                {
                    Array.Resize(ref array, len);
                }
                return array;
            }

            /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
            public int ToArrayNonAlloc(ref T[] array)
            {
                var index = 0;
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    if (array.Length == index)
                    {
                        var newSize = (index == 0) ? 4 : index * 2;
                        Array.Resize(ref array, newSize);
                    }
                    array[index++] = e.Current;
                }

                return index;
            }

            #endregion
        }

        public struct OfComponentEnumerator<T> : IEnumerator<T>
            where T : Component
        {
            Enumerator enumerator; // enumerator is mutable
            T current;

#if UNITY_EDITOR
            static List<T> componentCache = new List<T>(); // for no allocate on UNITY_EDITOR
#endif

            public OfComponentEnumerator(ref AncestorsEnumerable parent)
            {
                this.enumerator = parent.GetEnumerator();
                this.current = default(T);
            }

            public bool MoveNext()
            {
                while (enumerator.MoveNext())
                {
#if UNITY_EDITOR
                    enumerator.Current.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        current = componentCache[0];
                        componentCache.Clear();
                        return true;
                    }
#else
                        
                        var component = enumerator.Current.GetComponent<T>();
                        if (component != null)
                        {
                            current = component;
                            return true;
                        }
#endif
                }

                return false;
            }

            public T Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }
    }

    public struct DescendantsEnumerable : IEnumerable<GameObject>
    {
        static readonly Func<Transform, bool> alwaysTrue = _ => true;

        readonly GameObject origin;
        readonly bool withSelf;
        readonly Func<Transform, bool> descendIntoChildren;

        public DescendantsEnumerable(GameObject origin, bool withSelf, Func<Transform, bool> descendIntoChildren)
        {
            this.origin = origin;
            this.withSelf = withSelf;
            this.descendIntoChildren = descendIntoChildren ?? alwaysTrue;
        }

        /// <summary>Returns a collection of specified component in the source collection.</summary>
        public OfComponentEnumerable<T> OfComponent<T>()
            where T : Component
        {
            return new OfComponentEnumerable<T>(ref this);
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.Destroy(useDestroyImmediate, false);
            }
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(Func<GameObject, bool> predicate, bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current;
                if (predicate(item))
                {
                    item.Destroy(useDestroyImmediate, false);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            // check GameObject is destroyed only on GetEnumerator timing
            if (origin == null)
            {
                return new Enumerator(null, withSelf, false, null, descendIntoChildren);
            }

            InternalUnsafeRefStack refStack;
            if (InternalUnsafeRefStack.RefStackPool.Count != 0)
            {
                refStack = InternalUnsafeRefStack.RefStackPool.Dequeue();
                refStack.Reset();
            }
            else
            {
                refStack = new InternalUnsafeRefStack(6);
            }

            return new Enumerator(origin.transform, withSelf, true, refStack, descendIntoChildren);
        }

        IEnumerator<GameObject> IEnumerable<GameObject>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region LINQ

        void ResizeArray<T>(ref int index, ref T[] array)
        {
            if (array.Length == index)
            {
                var newSize = (index == 0) ? 4 : index * 2;
                Array.Resize(ref array, newSize);
            }
        }

        void DescendantsCore(ref Transform transform, ref Action<GameObject> action)
        {
            if (!descendIntoChildren(transform)) return;

            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                action(child.gameObject);
                DescendantsCore(ref child, ref action);
            }
        }

        void DescendantsCore(ref Transform transform, ref int index, ref GameObject[] array)
        {
            if (!descendIntoChildren(transform)) return;

            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                ResizeArray(ref index, ref array);
                array[index++] = child.gameObject;
                DescendantsCore(ref child, ref index, ref array);
            }
        }

        void DescendantsCore(ref Func<GameObject, bool> filter, ref Transform transform, ref int index, ref GameObject[] array)
        {
            if (!descendIntoChildren(transform)) return;

            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                var childGameObject = child.gameObject;
                if (filter(childGameObject))
                {
                    ResizeArray(ref index, ref array);
                    array[index++] = childGameObject;
                }
                DescendantsCore(ref filter, ref child, ref index, ref array);
            }
        }

        void DescendantsCore<T>(ref Func<GameObject, T> selector, ref Transform transform, ref int index, ref T[] array)
        {
            if (!descendIntoChildren(transform)) return;

            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                ResizeArray(ref index, ref array);
                array[index++] = selector(child.gameObject);
                DescendantsCore(ref selector, ref child, ref index, ref array);
            }
        }

        void DescendantsCore<T>(ref Func<GameObject, bool> filter, ref Func<GameObject, T> selector, ref Transform transform, ref int index, ref T[] array)
        {
            if (!descendIntoChildren(transform)) return;

            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                var childGameObject = child.gameObject;
                if (filter(childGameObject))
                {
                    ResizeArray(ref index, ref array);
                    array[index++] = selector(childGameObject);
                }
                DescendantsCore(ref filter, ref selector, ref child, ref index, ref array);
            }
        }

        void DescendantsCore<TState, T>(ref Func<GameObject, TState> let, ref Func<TState, bool> filter, ref Func<TState, T> selector, ref Transform transform, ref int index, ref T[] array)
        {
            if (!descendIntoChildren(transform)) return;

            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                var state = let(child.gameObject);
                if (filter(state))
                {
                    ResizeArray(ref index, ref array);
                    array[index++] = selector(state);
                }
                DescendantsCore(ref let, ref filter, ref selector, ref child, ref index, ref array);
            }
        }

        /// <summary>Use internal iterator for performance optimization.</summary>
        /// <param name="action"></param>
        public void ForEach(Action<GameObject> action)
        {
            if (withSelf)
            {
                action(origin);
            }
            var originTransform = origin.transform;
            DescendantsCore(ref originTransform, ref action);
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(ref GameObject[] array)
        {
            var index = 0;
            if (withSelf)
            {
                ResizeArray(ref index, ref array);
                array[index++] = origin;
            }

            var originTransform = origin.transform;
            DescendantsCore(ref originTransform, ref index, ref array);

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(Func<GameObject, bool> filter, ref GameObject[] array)
        {
            var index = 0;
            if (withSelf && filter(origin))
            {
                ResizeArray(ref index, ref array);
                array[index++] = origin;
            }
            var originTransform = origin.transform;
            DescendantsCore(ref filter, ref originTransform, ref index, ref array);

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            if (withSelf)
            {
                ResizeArray(ref index, ref array);
                array[index++] = selector(origin);
            }
            var originTransform = origin.transform;
            DescendantsCore(ref selector, ref originTransform, ref index, ref array);

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            if (withSelf && filter(origin))
            {
                ResizeArray(ref index, ref array);
                array[index++] = selector(origin);
            }
            var originTransform = origin.transform;
            DescendantsCore(ref filter, ref selector, ref originTransform, ref index, ref array);

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector, ref T[] array)
        {
            var index = 0;
            if (withSelf)
            {
                var state = let(origin);
                if (filter(state))
                {
                    ResizeArray(ref index, ref array);
                    array[index++] = selector(state);
                }
            }

            var originTransform = origin.transform;
            DescendantsCore(ref let, ref filter, ref selector, ref originTransform, ref index, ref array);

            return index;
        }

        public GameObject[] ToArray()
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject[] ToArray(Func<GameObject, bool> filter)
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(filter, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc<T>(selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(let, filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject First()
        {
            var e = this.GetEnumerator();
            try
            {
                if (e.MoveNext())
                {
                    return e.Current;
                }
                else
                {
                    throw new InvalidOperationException("sequence is empty.");
                }
            }
            finally
            {
                e.Dispose();
            }
        }

        public GameObject FirstOrDefault()
        {
            var e = this.GetEnumerator();
            try
            {
                return (e.MoveNext())
                    ? e.Current
                    : null;
            }
            finally
            {
                e.Dispose();
            }
        }

        #endregion

        internal class InternalUnsafeRefStack
        {
            public static Queue<InternalUnsafeRefStack> RefStackPool = new Queue<InternalUnsafeRefStack>();

            public int size = 0;
            public Enumerator[] array; // Pop = this.array[--size];

            public InternalUnsafeRefStack(int initialStackDepth)
            {
                array = new DescendantsEnumerable.Enumerator[initialStackDepth];
            }

            public void Push(ref Enumerator e)
            {
                if (size == array.Length)
                {
                    Array.Resize(ref array, array.Length * 2);
                }
                array[size++] = e;
            }

            public void Reset()
            {
                size = 0;
            }
        }

        public struct Enumerator : IEnumerator<GameObject>
        {
            readonly int childCount; // childCount is fixed when GetEnumerator is called.

            readonly Transform originTransform;
            bool canRun;

            bool withSelf;
            int currentIndex;
            GameObject current;
            InternalUnsafeRefStack sharedStack;
            Func<Transform, bool> descendIntoChildren;

            internal Enumerator(Transform originTransform, bool withSelf, bool canRun, InternalUnsafeRefStack sharedStack, Func<Transform, bool> descendIntoChildren)
            {
                this.originTransform = originTransform;
                this.withSelf = withSelf;
                this.childCount = canRun ? originTransform.childCount : 0;
                this.currentIndex = -1;
                this.canRun = canRun;
                this.current = null;
                this.sharedStack = sharedStack;
                this.descendIntoChildren = descendIntoChildren;
            }

            public bool MoveNext()
            {
                if (!canRun) return false;

                while (sharedStack.size != 0)
                {
                    if (sharedStack.array[sharedStack.size - 1].MoveNextCore(true, out current))
                    {
                        return true;
                    }
                }

                if (!withSelf && !descendIntoChildren(originTransform))
                {
                    // reuse
                    canRun = false;
                    InternalUnsafeRefStack.RefStackPool.Enqueue(sharedStack);
                    return false;
                }

                if (MoveNextCore(false, out current))
                {
                    return true;
                }
                else
                {
                    // reuse
                    canRun = false;
                    InternalUnsafeRefStack.RefStackPool.Enqueue(sharedStack);
                    return false;
                }
            }

            bool MoveNextCore(bool peek, out GameObject current)
            {
                if (withSelf)
                {
                    current = originTransform.gameObject;
                    withSelf = false;
                    return true;
                }

                ++currentIndex;
                if (currentIndex < childCount)
                {
                    var item = originTransform.GetChild(currentIndex);
                    if (descendIntoChildren(item))
                    {
                        var childEnumerator = new Enumerator(item, true, true, sharedStack, descendIntoChildren);
                        sharedStack.Push(ref childEnumerator);
                        return sharedStack.array[sharedStack.size - 1].MoveNextCore(true, out current);
                    }
                    else
                    {
                        current = item.gameObject;
                        return true;
                    }
                }

                if (peek)
                {
                    sharedStack.size--; // Pop
                }

                current = null;
                return false;
            }

            public GameObject Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }

            public void Dispose()
            {
                if (canRun)
                {
                    canRun = false;
                    InternalUnsafeRefStack.RefStackPool.Enqueue(sharedStack);
                }
            }

            public void Reset() { throw new NotSupportedException(); }
        }

        public struct OfComponentEnumerable<T> : IEnumerable<T>
            where T : Component
        {
            DescendantsEnumerable parent;

            public OfComponentEnumerable(ref DescendantsEnumerable parent)
            {
                this.parent = parent;
            }

            public OfComponentEnumerator<T> GetEnumerator()
            {
                return new OfComponentEnumerator<T>(ref parent);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region LINQ

            public T First()
            {
                var e = this.GetEnumerator();
                try
                {
                    if (e.MoveNext())
                    {
                        return e.Current;
                    }
                    else
                    {
                        throw new InvalidOperationException("sequence is empty.");
                    }
                }
                finally
                {
                    e.Dispose();
                }
            }

            public T FirstOrDefault()
            {
                var e = this.GetEnumerator();
                try
                {
                    return (e.MoveNext())
                        ? e.Current
                        : null;
                }
                finally
                {
                    e.Dispose();
                }
            }

            /// <summary>Use internal iterator for performance optimization.</summary>
            public void ForEach(Action<T> action)
            {
                if (parent.withSelf)
                {
                    T component = default(T);
#if UNITY_EDITOR
                    parent.origin.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        component = componentCache[0];
                        componentCache.Clear();
                    }
#else
                        component = parent.origin.GetComponent<T>();
#endif

                    if (component != null)
                    {
                        action(component);
                    }
                }

                var originTransform = parent.origin.transform;
                OfComponentDescendantsCore(ref originTransform, ref action);
            }


            public T[] ToArray()
            {
                var array = new T[4];
                var len = ToArrayNonAlloc(ref array);
                if (array.Length != len)
                {
                    Array.Resize(ref array, len);
                }
                return array;
            }

#if UNITY_EDITOR
            static List<T> componentCache = new List<T>(); // for no allocate on UNITY_EDITOR
#endif

            void OfComponentDescendantsCore(ref Transform transform, ref Action<T> action)
            {
                if (!parent.descendIntoChildren(transform)) return;

                var childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = transform.GetChild(i);

                    T component = default(T);
#if UNITY_EDITOR
                    child.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        component = componentCache[0];
                        componentCache.Clear();
                    }
#else
                        component = child.GetComponent<T>();
#endif

                    if (component != null)
                    {
                        action(component);
                    }
                    OfComponentDescendantsCore(ref child, ref action);
                }
            }

            void OfComponentDescendantsCore(ref Transform transform, ref int index, ref T[] array)
            {
                if (!parent.descendIntoChildren(transform)) return;

                var childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = transform.GetChild(i);
                    T component = default(T);
#if UNITY_EDITOR
                    child.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        component = componentCache[0];
                        componentCache.Clear();
                    }
#else
                        component = child.GetComponent<T>();
#endif

                    if (component != null)
                    {
                        if (array.Length == index)
                        {
                            var newSize = (index == 0) ? 4 : index * 2;
                            Array.Resize(ref array, newSize);
                        }

                        array[index++] = component;
                    }
                    OfComponentDescendantsCore(ref child, ref index, ref array);
                }
            }

            /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
            public int ToArrayNonAlloc(ref T[] array)
            {
                var index = 0;
                if (parent.withSelf)
                {
                    T component = default(T);
#if UNITY_EDITOR
                    parent.origin.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        component = componentCache[0];
                        componentCache.Clear();
                    }
#else
                        component = parent.origin.GetComponent<T>();
#endif

                    if (component != null)
                    {
                        if (array.Length == index)
                        {
                            var newSize = (index == 0) ? 4 : index * 2;
                            Array.Resize(ref array, newSize);
                        }

                        array[index++] = component;
                    }
                }

                var originTransform = parent.origin.transform;
                OfComponentDescendantsCore(ref originTransform, ref index, ref array);

                return index;
            }

            #endregion
        }

        public struct OfComponentEnumerator<T> : IEnumerator<T>
            where T : Component
        {
            Enumerator enumerator; // enumerator is mutable
            T current;

#if UNITY_EDITOR
            static List<T> componentCache = new List<T>(); // for no allocate on UNITY_EDITOR
#endif

            public OfComponentEnumerator(ref DescendantsEnumerable parent)
            {
                this.enumerator = parent.GetEnumerator();
                this.current = default(T);
            }

            public bool MoveNext()
            {
                while (enumerator.MoveNext())
                {
#if UNITY_EDITOR
                    enumerator.Current.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        current = componentCache[0];
                        componentCache.Clear();
                        return true;
                    }
#else
                        
                        var component = enumerator.Current.GetComponent<T>();
                        if (component != null)
                        {
                            current = component;
                            return true;
                        }
#endif
                }

                return false;
            }

            public T Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }

            public void Dispose()
            {
                enumerator.Dispose();
            }

            public void Reset() { throw new NotSupportedException(); }
        }
    }

    public struct BeforeSelfEnumerable : IEnumerable<GameObject>
    {
        readonly GameObject origin;
        readonly bool withSelf;

        public BeforeSelfEnumerable(GameObject origin, bool withSelf)
        {
            this.origin = origin;
            this.withSelf = withSelf;
        }

        /// <summary>Returns a collection of specified component in the source collection.</summary>
        public OfComponentEnumerable<T> OfComponent<T>()
            where T : Component
        {
            return new OfComponentEnumerable<T>(ref this);
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.Destroy(useDestroyImmediate, false);
            }
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(Func<GameObject, bool> predicate, bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current;
                if (predicate(item))
                {
                    item.Destroy(useDestroyImmediate, false);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            // check GameObject is destroyed only on GetEnumerator timing
            return (origin == null)
                ? new Enumerator(null, withSelf, false)
                : new Enumerator(origin.transform, withSelf, true);
        }

        IEnumerator<GameObject> IEnumerable<GameObject>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region LINQ

        public void ForEach(Action<GameObject> action)
        {
            var e = this.GetEnumerator();
            while (e.MoveNext())
            {
                action(e.Current);
            }
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(ref GameObject[] array)
        {
            var index = 0;

            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(Func<GameObject, bool> filter, ref GameObject[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                var state = let(item);

                if (!filter(state)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(state);
            }

            return index;
        }

        public GameObject[] ToArray()
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject[] ToArray(Func<GameObject, bool> filter)
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(filter, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc<T>(selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(let, filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject First()
        {
            var e = this.GetEnumerator();
            if (e.MoveNext())
            {
                return e.Current;
            }
            else
            {
                throw new InvalidOperationException("sequence is empty.");
            }
        }

        public GameObject FirstOrDefault()
        {
            var e = this.GetEnumerator();
            return (e.MoveNext())
                ? e.Current
                : null;
        }

        #endregion

        public struct Enumerator : IEnumerator<GameObject>
        {
            readonly int childCount; // childCount is fixed when GetEnumerator is called.
            readonly Transform originTransform;
            bool canRun;

            bool withSelf;
            int currentIndex;
            GameObject current;
            Transform parent;

            internal Enumerator(Transform originTransform, bool withSelf, bool canRun)
            {
                this.originTransform = originTransform;
                this.withSelf = withSelf;
                this.currentIndex = -1;
                this.canRun = canRun;
                this.current = null;
                this.parent = originTransform.parent;
                this.childCount = (parent != null) ? parent.childCount : 0;
            }

            public bool MoveNext()
            {
                if (!canRun) return false;

                if (parent == null) goto RETURN_SELF;

                currentIndex++;
                if (currentIndex < childCount)
                {
                    var item = parent.GetChild(currentIndex);

                    if (item == originTransform)
                    {
                        goto RETURN_SELF;
                    }

                    current = item.gameObject;
                    return true;
                }

            RETURN_SELF:
                if (withSelf)
                {
                    current = originTransform.gameObject;
                    withSelf = false;
                    canRun = false; // reached self, run complete.
                    return true;
                }

                return false;
            }

            public GameObject Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }

        public struct OfComponentEnumerable<T> : IEnumerable<T>
            where T : Component
        {
            BeforeSelfEnumerable parent;

            public OfComponentEnumerable(ref BeforeSelfEnumerable parent)
            {
                this.parent = parent;
            }

            public OfComponentEnumerator<T> GetEnumerator()
            {
                return new OfComponentEnumerator<T>(ref parent);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region LINQ

            public void ForEach(Action<T> action)
            {
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    action(e.Current);
                }
            }

            public T First()
            {
                var e = this.GetEnumerator();
                if (e.MoveNext())
                {
                    return e.Current;
                }
                else
                {
                    throw new InvalidOperationException("sequence is empty.");
                }
            }

            public T FirstOrDefault()
            {
                var e = this.GetEnumerator();
                return (e.MoveNext())
                    ? e.Current
                    : null;
            }

            public T[] ToArray()
            {
                var array = new T[4];
                var len = ToArrayNonAlloc(ref array);
                if (array.Length != len)
                {
                    Array.Resize(ref array, len);
                }
                return array;
            }

            /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
            public int ToArrayNonAlloc(ref T[] array)
            {
                var index = 0;
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    if (array.Length == index)
                    {
                        var newSize = (index == 0) ? 4 : index * 2;
                        Array.Resize(ref array, newSize);
                    }
                    array[index++] = e.Current;
                }

                return index;
            }

            #endregion
        }

        public struct OfComponentEnumerator<T> : IEnumerator<T>
            where T : Component
        {
            Enumerator enumerator; // enumerator is mutable
            T current;

#if UNITY_EDITOR
            static List<T> componentCache = new List<T>(); // for no allocate on UNITY_EDITOR
#endif

            public OfComponentEnumerator(ref BeforeSelfEnumerable parent)
            {
                this.enumerator = parent.GetEnumerator();
                this.current = default(T);
            }

            public bool MoveNext()
            {
                while (enumerator.MoveNext())
                {
#if UNITY_EDITOR
                    enumerator.Current.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        current = componentCache[0];
                        componentCache.Clear();
                        return true;
                    }
#else
                        
                        var component = enumerator.Current.GetComponent<T>();
                        if (component != null)
                        {
                            current = component;
                            return true;
                        }
#endif
                }

                return false;
            }

            public T Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }
    }

    public struct AfterSelfEnumerable : IEnumerable<GameObject>
    {
        readonly GameObject origin;
        readonly bool withSelf;

        public AfterSelfEnumerable(GameObject origin, bool withSelf)
        {
            this.origin = origin;
            this.withSelf = withSelf;
        }

        /// <summary>Returns a collection of specified component in the source collection.</summary>
        public OfComponentEnumerable<T> OfComponent<T>()
            where T : Component
        {
            return new OfComponentEnumerable<T>(ref this);
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.Destroy(useDestroyImmediate, false);
            }
        }

        /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
        /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
        public void Destroy(Func<GameObject, bool> predicate, bool useDestroyImmediate = false)
        {
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current;
                if (predicate(item))
                {
                    item.Destroy(useDestroyImmediate, false);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            // check GameObject is destroyed only on GetEnumerator timing
            return (origin == null)
                ? new Enumerator(null, withSelf, false)
                : new Enumerator(origin.transform, withSelf, true);
        }

        IEnumerator<GameObject> IEnumerable<GameObject>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region LINQ

        public void ForEach(Action<GameObject> action)
        {
            var e = this.GetEnumerator();
            while (e.MoveNext())
            {
                action(e.Current);
            }
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(ref GameObject[] array)
        {
            var index = 0;

            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc(Func<GameObject, bool> filter, ref GameObject[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = item;
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                if (!filter(item)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(item);
            }

            return index;
        }

        /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
        public int ToArrayNonAlloc<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector, ref T[] array)
        {
            var index = 0;
            var e = this.GetEnumerator(); // does not need to call Dispose.
            while (e.MoveNext())
            {
                var item = e.Current;
                var state = let(item);

                if (!filter(state)) continue;

                if (array.Length == index)
                {
                    var newSize = (index == 0) ? 4 : index * 2;
                    Array.Resize(ref array, newSize);
                }
                array[index++] = selector(state);
            }

            return index;
        }

        public GameObject[] ToArray()
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject[] ToArray(Func<GameObject, bool> filter)
        {
            var array = new GameObject[4];
            var len = ToArrayNonAlloc(filter, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc<T>(selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<T>(Func<GameObject, bool> filter, Func<GameObject, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public T[] ToArray<TState, T>(Func<GameObject, TState> let, Func<TState, bool> filter, Func<TState, T> selector)
        {
            var array = new T[4];
            var len = ToArrayNonAlloc(let, filter, selector, ref array);
            if (array.Length != len)
            {
                Array.Resize(ref array, len);
            }
            return array;
        }

        public GameObject First()
        {
            var e = this.GetEnumerator();
            if (e.MoveNext())
            {
                return e.Current;
            }
            else
            {
                throw new InvalidOperationException("sequence is empty.");
            }
        }

        public GameObject FirstOrDefault()
        {
            var e = this.GetEnumerator();
            return (e.MoveNext())
                ? e.Current
                : null;
        }

        #endregion

        public struct Enumerator : IEnumerator<GameObject>
        {
            readonly int childCount; // childCount is fixed when GetEnumerator is called.
            readonly Transform originTransform;
            readonly bool canRun;

            bool withSelf;
            int currentIndex;
            GameObject current;
            Transform parent;

            internal Enumerator(Transform originTransform, bool withSelf, bool canRun)
            {
                this.originTransform = originTransform;
                this.withSelf = withSelf;
                this.currentIndex = (originTransform != null) ? originTransform.GetSiblingIndex() + 1 : 0;
                this.canRun = canRun;
                this.current = null;
                this.parent = originTransform.parent;
                this.childCount = (parent != null) ? parent.childCount : 0;
            }

            public bool MoveNext()
            {
                if (!canRun) return false;

                if (withSelf)
                {
                    current = originTransform.gameObject;
                    withSelf = false;
                    return true;
                }

                if (currentIndex < childCount)
                {
                    current = parent.GetChild(currentIndex).gameObject;
                    currentIndex++;
                    return true;
                }

                return false;
            }

            public GameObject Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }

        public struct OfComponentEnumerable<T> : IEnumerable<T>
            where T : Component
        {
            AfterSelfEnumerable parent;

            public OfComponentEnumerable(ref AfterSelfEnumerable parent)
            {
                this.parent = parent;
            }

            public OfComponentEnumerator<T> GetEnumerator()
            {
                return new OfComponentEnumerator<T>(ref this.parent);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region LINQ

            public void ForEach(Action<T> action)
            {
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    action(e.Current);
                }
            }

            public T First()
            {
                var e = this.GetEnumerator();
                if (e.MoveNext())
                {
                    return e.Current;
                }
                else
                {
                    throw new InvalidOperationException("sequence is empty.");
                }
            }

            public T FirstOrDefault()
            {
                var e = this.GetEnumerator();
                return (e.MoveNext())
                    ? e.Current
                    : null;
            }

            public T[] ToArray()
            {
                var array = new T[4];
                var len = ToArrayNonAlloc(ref array);
                if (array.Length != len)
                {
                    Array.Resize(ref array, len);
                }
                return array;
            }

            /// <summary>Store element into the buffer, return number is size. array is automaticaly expanded.</summary>
            public int ToArrayNonAlloc(ref T[] array)
            {
                var index = 0;
                var e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    if (array.Length == index)
                    {
                        var newSize = (index == 0) ? 4 : index * 2;
                        Array.Resize(ref array, newSize);
                    }
                    array[index++] = e.Current;
                }

                return index;
            }

            #endregion
        }

        public struct OfComponentEnumerator<T> : IEnumerator<T>
            where T : Component
        {
            Enumerator enumerator; // enumerator is mutable
            T current;

#if UNITY_EDITOR
            static List<T> componentCache = new List<T>(); // for no allocate on UNITY_EDITOR
#endif

            public OfComponentEnumerator(ref AfterSelfEnumerable parent)
            {
                this.enumerator = parent.GetEnumerator();
                this.current = default(T);
            }

            public bool MoveNext()
            {
                while (enumerator.MoveNext())
                {
#if UNITY_EDITOR
                    enumerator.Current.GetComponents<T>(componentCache);
                    if (componentCache.Count != 0)
                    {
                        current = componentCache[0];
                        componentCache.Clear();
                        return true;
                    }
#else
                        
                        var component = enumerator.Current.GetComponent<T>();
                        if (component != null)
                        {
                            current = component;
                            return true;
                        }
#endif
                }

                return false;
            }

            public T Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }
            public void Dispose() { }
            public void Reset() { throw new NotSupportedException(); }
        }
    }
}
