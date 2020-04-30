using UnityEngine;

//不能拖拽赋值东西
public class SingletonMono<T> : MonoBehaviour
    where T: SingletonMono<T>
{
    const string SingletonName = "SingletonMono";
    static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = GameObject.Find(SingletonName);
                if(go == null)
                {
                    go = new GameObject(SingletonName);
                    DontDestroyOnLoad(go);
                }
                instance = go.AddComponent<T>();
            }
            return instance;
        }
    }
}
