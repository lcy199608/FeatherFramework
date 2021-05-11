using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
}
