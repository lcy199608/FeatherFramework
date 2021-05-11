using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformCheck
{
    public static bool IsANDROID
    {
        get
        {
            bool retValue = false;
#if UNITY_ANDROID
                retValue = true;    
#endif
            return retValue;
        }
    }

    public static bool IsEDITOR
    {
        get
        {
            bool retValue = false;
#if UNITY_EDITOR
            retValue = true;
#endif
            return retValue;
        }
    }

    public static bool IsIOS
    {
        get
        {
            bool retValue = false;
#if UNITY_IOS
                retValue = true;    
#endif
            return retValue;
        }
    }

    public static bool IsWEBGL
    {
        get
        {
            bool retValue = false;
#if UNITY_WEBGL
                retValue = true;    
#endif
            return retValue;
        }
    }
}
