using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Newtonsoft.Json;

public static class SaveHandler
{
    const int KEY_SIZE = 256;
    const string SYS_DATA_FILE_NAME = "sys"; 
    const string TEMP_DATA_FILE_NAME = "data";
    const string EXIST_TEMP_SLOT_TITLE = "ExistSlots";

    public static int? CurrentTempSlotId { get; private set; } //当前的SlotId

    static ES3Settings es3Setting; //ES3的配置
    static Dictionary<string, string> systemDataDic; //系统数据
    static Dictionary<int, Dictionary<string, string>> tempDataDic; // 当前加载的临时数据
    static HashSet<int> existTempDataSlotSet; //当前的存档位
    static string initVector;

    //初始化
    public static void Initialize()
    {
        initVector = GetMd5Str(Application.productName + SystemInfo.deviceModel);
        es3Setting = new ES3Settings(ES3.EncryptionType.AES, Application.productName);

        try
        {
            systemDataDic = ES3.Load(SYS_DATA_FILE_NAME, new Dictionary<string, string>(), es3Setting);
        }
        catch
        {
            ES3.DeleteFile();
            systemDataDic = ES3.Load(SYS_DATA_FILE_NAME, new Dictionary<string, string>(), es3Setting);
        }

        existTempDataSlotSet = GetSystemData(EXIST_TEMP_SLOT_TITLE, new HashSet<int>());

        Debug.Log("Init Success");
    }

    // 加载数据
    public static void LoadTempData(int SlotId)
    {
        if (CurrentTempSlotId == null || CurrentTempSlotId != SlotId)
        {
            if (CurrentTempSlotId != null)
            {
                tempDataDic.Clear();
            }
            CurrentTempSlotId = SlotId;

            tempDataDic = ES3.Load(string.Join("_", TEMP_DATA_FILE_NAME, SlotId), new Dictionary<int, Dictionary<string, string>>(), es3Setting);

            if (!existTempDataSlotSet.Contains(SlotId))
            {
                existTempDataSlotSet.Add(SlotId);
                SetSystemData(EXIST_TEMP_SLOT_TITLE, existTempDataSlotSet, true);
            }
        }
    }

    // 存储系统数据
    public static void SetSystemData<T>(string ID, T value, bool SaveImmediately = false)
    {
        if (systemDataDic == null)
        {
            throw new Exception("未加载系统数据");
        }

        string s = EncryptString(JsonConvert.SerializeObject(value), ID);

        if (systemDataDic.ContainsKey(ID))
        {
            if (systemDataDic[ID] == s)
            {
                return;
            }
            systemDataDic[ID] = s;
        }
        else
        {
            systemDataDic.Add(ID, s);
        }

        if (SaveImmediately)
        {
            ES3.Save<Dictionary<string, string>>(SYS_DATA_FILE_NAME, systemDataDic, es3Setting);
        }
    }

    // 获取系统数据
    public static T GetSystemData<T>(string ID, T defaultValue)
    {
        if (systemDataDic == null)
        {
            throw new Exception("未加载系统数据");
        }

        if (systemDataDic.ContainsKey(ID))
        {
            return JsonConvert.DeserializeObject<T>(DecryptString(systemDataDic[ID], ID));
        }
        return defaultValue;
    }

    // 存储普通数据
    public static void SetTempData<T>(string ID, T value, bool SaveImmediately = true,int GroupID = 0)
    {
        if (CurrentTempSlotId == null)
        {
            throw new Exception("并未加载临时数据");
        }

        string s = EncryptString(JsonConvert.SerializeObject(value), ID);
        if (!tempDataDic.ContainsKey(GroupID))
        {
            tempDataDic.Add(GroupID, new Dictionary<string, string>());
        }

        if (tempDataDic[GroupID].ContainsKey(ID))
        {
            if (tempDataDic[GroupID][ID] == s)
            {
                return;
            }
            tempDataDic[GroupID][ID] = s;
        }
        else
        {
            tempDataDic[GroupID].Add(ID, s);
        }

        if (SaveImmediately)
        {
            ES3.Save<Dictionary<int, Dictionary<string, string>>>(string.Join("_", TEMP_DATA_FILE_NAME, CurrentTempSlotId), tempDataDic, es3Setting);
        }
    }

    // 获取普通数据
    public static T GetTempData<T>(string ID, T defaultValue, int GroupId = 0)
    {
        if (CurrentTempSlotId == null)
        {
            throw new Exception("并未加载临时数据");
        }

        if (tempDataDic.ContainsKey(GroupId) && tempDataDic[GroupId].ContainsKey(ID))
        {
            return JsonConvert.DeserializeObject<T>(DecryptString(tempDataDic[GroupId][ID], ID));
        }
        else
        {
            return defaultValue;
        }
    }

    // 取值
    public static T GetValue<T>(SQLIdHolder key, T defaultValue)
    {
        if (key.IsSystemData)
        {
            return GetSystemData(key.ID, defaultValue);
        }
        else
        {
            return GetTempData(key.ID, defaultValue, key.GroupID);
        }
    }

    // 存值
    public static void SetValue<T>(SQLIdHolder key, T value, bool SaveImmediately = true)
    {
        if (key.IsSystemData)
        {
            SetSystemData(key.ID, value, SaveImmediately);
        }
        else
        {
            SetTempData(key.ID, value, SaveImmediately, key.GroupID);
        }
    }

    public static void ApplyChangesToDatabase()
    {
        if (systemDataDic == null)
        {
            throw new Exception("未加载系统数据");
        }

        lock (systemDataDic)
        {
            ES3.Save<Dictionary<string, string>>(SYS_DATA_FILE_NAME, systemDataDic, es3Setting);
        }
        if (CurrentTempSlotId != null)
        {
            lock (tempDataDic)
            {
                ES3.Save<Dictionary<int, Dictionary<string, string>>>(string.Join("_", TEMP_DATA_FILE_NAME, CurrentTempSlotId), tempDataDic, es3Setting);
            }
        }
    }

    //删除某个GroupId
    public static void DeleteTempDataGroup(int GroupId = 0)
    {
        if (CurrentTempSlotId == null)
        {
            throw new Exception("并未加载临时数据的SQL");
        }
        if (tempDataDic.ContainsKey(GroupId))
        {
            tempDataDic.Remove(GroupId);
        }
    }

    //清除某个slot档位
    public static void DeleteTempDataTable(int slotID = 0)
    {
        if (CurrentTempSlotId == slotID)
        {
            CurrentTempSlotId = null;
            tempDataDic?.Clear();
        }
        if (existTempDataSlotSet.Contains(slotID))
        {
            existTempDataSlotSet.Remove(slotID);
            SetSystemData(EXIST_TEMP_SLOT_TITLE, existTempDataSlotSet, true);
            ES3.DeleteKey(string.Join("_", TEMP_DATA_FILE_NAME, slotID));
        }
    }

    //清除全部数据
    public static void DeleteAllTempDataTable()
    {
        foreach (var item in existTempDataSlotSet.ToList())
        {
            DeleteTempDataTable(item);
        }
    }

    // 获取MD5值
    private static string GetMd5Str(string ConvertString)
    {
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        string t2 = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(ConvertString)), 4, 8);
        t2 = t2.Replace("-", "");
        return t2;
    }

    //加密
    private static string EncryptString(string plainText, string passPhrase)
    {
#if UNITY_EDITOR
        //return plainText;
#endif
        byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
        byte[] keyBytes = password.GetBytes(KEY_SIZE / 8);
        RijndaelManaged symmetricKey = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
        MemoryStream memoryStream = new MemoryStream();
        CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
        cryptoStream.FlushFinalBlock();
        byte[] cipherTextBytes = memoryStream.ToArray();
        memoryStream.Close();
        cryptoStream.Close();
        return Convert.ToBase64String(cipherTextBytes);
    }

    //解密
    private static string DecryptString(string cipherText, string passPhrase)
    {
#if UNITY_EDITOR
        //return cipherText;
#endif
        byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
        byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
        PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
        byte[] keyBytes = password.GetBytes(KEY_SIZE / 8);
        RijndaelManaged symmetricKey = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
        MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
        CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        byte[] plainTextBytes = new byte[cipherTextBytes.Length];
        int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
        memoryStream.Close();
        cryptoStream.Close();
        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
    }
}
