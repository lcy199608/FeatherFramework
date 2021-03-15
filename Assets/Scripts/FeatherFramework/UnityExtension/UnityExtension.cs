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
}
