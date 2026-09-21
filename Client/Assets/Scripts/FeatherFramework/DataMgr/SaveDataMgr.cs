using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Newtonsoft.Json;

public sealed class SaveDataMgr
{
    const int KEY_SIZE = 256;
    const string SYS_DATA_FILE_NAME = "sys"; 
    const string TEMP_DATA_FILE_NAME = "data";
    const string EXIST_TEMP_SLOT_TITLE = "ExistSlots";

    public int? CurrentTempSlotId { get; private set; } //当前的SlotId

    ES3Settings es3Setting; //ES3的配置
    Dictionary<string, string> systemDataDic; //系统数据
    Dictionary<int, Dictionary<string, string>> tempDataDic; // 当前加载的临时数据
    HashSet<int> existTempDataSlotSet; //当前的存档位
    string initVector;
    bool systemDataDirty;
    bool tempDataDirty;
    private bool stopped;
    public Exception ReadError { get; private set; }
    public bool IsReadOnly => ReadError != null;

    private void EnsureWritable()
    {
        if (stopped) throw new ObjectDisposedException(nameof(SaveDataMgr));
        if (ReadError != null) throw new InvalidOperationException("Save data could not be read. Restore the original file and retry before writing.", ReadError);
    }

    // Retrying only a failed read preserves normal dirty data and never deletes the source file.
    public bool RetryRead()
    {
        if (stopped) throw new ObjectDisposedException(nameof(SaveDataMgr));
        if (ReadError == null) return true;
        int? slot = CurrentTempSlotId;
        CurrentTempSlotId = null;
        ReadError = null;
        Initialize(es3Setting);
        if (slot.HasValue && ReadError == null) LoadData(slot.Value);
        return ReadError == null;
    }

    internal void Shutdown()
    {
        if (stopped) return;
        try { if (!IsReadOnly) ApplyChangesToDatabase(); }
        finally { stopped = true; }
    }

    internal SaveDataMgr() { }

    //初始化
    public void Initialize()
    {
        Initialize(new ES3Settings(ES3.EncryptionType.AES, Application.productName));
    }

    internal void Initialize(ES3Settings settings)
    {
        if (stopped) throw new ObjectDisposedException(nameof(SaveDataMgr));
        if (systemDataDirty || tempDataDirty) throw new InvalidOperationException("Flush pending changes before reinitializing save data.");
        initVector = GetMd5Str(Application.productName + SystemInfo.deviceModel);
        es3Setting = settings ?? throw new ArgumentNullException(nameof(settings));

        try
        {
            systemDataDic = ES3.Load(SYS_DATA_FILE_NAME, new Dictionary<string, string>(), es3Setting);
        }
        catch (Exception exception)
        {
            ReadError = exception;
            Debug.LogError($"Failed to load system save data. An empty in-memory save will be used and the original file will be preserved.\n{exception}");
            systemDataDic = new Dictionary<string, string>();
        }

        systemDataDic ??= new Dictionary<string, string>();
        existTempDataSlotSet = GetSystemData(EXIST_TEMP_SLOT_TITLE, new HashSet<int>());
        existTempDataSlotSet ??= new HashSet<int>();

        Debug.Log("Game Data Init Success");
    }

    // 加载数据
    public void LoadData(int SlotId)
    {
        EnsureWritable();
        if (CurrentTempSlotId == null || CurrentTempSlotId != SlotId)
        {
            if (CurrentTempSlotId != null)
            {
                ApplyChangesToDatabase();
            }
            CurrentTempSlotId = SlotId;

            string slotFileName = string.Join("_", TEMP_DATA_FILE_NAME, SlotId);
            try
            {
                tempDataDic = ES3.Load(slotFileName, new Dictionary<int, Dictionary<string, string>>(), es3Setting);
            }
            catch (Exception exception)
            {
                ReadError = exception;
                Debug.LogError($"Failed to load save slot {SlotId}. Empty in-memory data will be used and {slotFileName} will be preserved.\n{exception}");
                tempDataDic = new Dictionary<int, Dictionary<string, string>>();
            }
            tempDataDic ??= new Dictionary<int, Dictionary<string, string>>();
            tempDataDirty = false;

            if (ReadError == null && !existTempDataSlotSet.Contains(SlotId))
            {
                existTempDataSlotSet.Add(SlotId);
                SetSystemData(EXIST_TEMP_SLOT_TITLE, existTempDataSlotSet, true);
            }
        }
    }

    // 存储系统数据
    public void SetSystemData<T>(string ID, T value, bool SaveImmediately = false)
    {
        EnsureWritable();
        ValidateKey(ID);
        if (systemDataDic == null)
        {
            throw new Exception("未加载系统数据");
        }

        string s = EncryptString(JsonConvert.SerializeObject(value), ID);

        if (systemDataDic.ContainsKey(ID))
        {
            if (systemDataDic[ID] == s)
            {
                if (SaveImmediately && systemDataDirty)
                {
                    SaveSystemData();
                }
                return;
            }
            systemDataDic[ID] = s;
        }
        else
        {
            systemDataDic.Add(ID, s);
        }

        systemDataDirty = true;
        if (SaveImmediately)
        {
            SaveSystemData();
        }
    }

    // 获取系统数据
    public T GetSystemData<T>(string ID, T defaultValue)
    {
        ValidateKey(ID);
        if (systemDataDic == null)
        {
            throw new Exception("未加载系统数据");
        }

        if (systemDataDic.ContainsKey(ID))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(DecryptString(systemDataDic[ID], ID));
            }
            catch (Exception exception)
            {
                ReadError = exception;
                Debug.LogError($"Failed to decode system save key '{ID}'. The default value will be used.\n{exception}");
            }
        }
        return defaultValue;
    }

    // 存储普通数据
    public void SetData<T>(string ID, T value, bool SaveImmediately = true,int GroupID = 0)
    {
        EnsureWritable();
        ValidateKey(ID);
        if (CurrentTempSlotId == null)
        {
            throw new Exception("并未加载临时数据");
        }

        string s = EncryptString(JsonConvert.SerializeObject(value), ID);
        if (!tempDataDic.ContainsKey(GroupID))
        {
            tempDataDic.Add(GroupID, new Dictionary<string, string>());
        }
        else if (tempDataDic[GroupID] == null)
        {
            tempDataDic[GroupID] = new Dictionary<string, string>();
        }

        if (tempDataDic[GroupID].ContainsKey(ID))
        {
            if (tempDataDic[GroupID][ID] == s)
            {
                if (SaveImmediately && tempDataDirty)
                {
                    SaveTempData();
                }
                return;
            }
            tempDataDic[GroupID][ID] = s;
        }
        else
        {
            tempDataDic[GroupID].Add(ID, s);
        }

        tempDataDirty = true;
        if (SaveImmediately)
        {
            SaveTempData();
        }
    }

    // 获取普通数据
    public T GetData<T>(string ID, T defaultValue, int GroupId = 0)
    {
        ValidateKey(ID);
        if (CurrentTempSlotId == null)
        {
            throw new Exception("并未加载临时数据");
        }

        if (tempDataDic != null && tempDataDic.ContainsKey(GroupId)
            && tempDataDic[GroupId] != null && tempDataDic[GroupId].ContainsKey(ID))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(DecryptString(tempDataDic[GroupId][ID], ID));
            }
            catch (Exception exception)
            {
                ReadError = exception;
                Debug.LogError($"Failed to decode save key '{ID}' in group {GroupId}. The default value will be used.\n{exception}");
            }
        }
        return defaultValue;
    }

    public void ApplyChangesToDatabase()
    {
        EnsureWritable();
        if (systemDataDic == null)
        {
            throw new Exception("未加载系统数据");
        }

        if (systemDataDirty)
        {
            SaveSystemData();
        }
        if (CurrentTempSlotId != null && tempDataDirty)
        {
            SaveTempData();
        }
    }

    //删除某个GroupId
    public void DeleteTempDataGroup(int GroupId = 0)
    {
        EnsureWritable();
        if (CurrentTempSlotId == null)
        {
            throw new Exception("并未加载临时数据的SQL");
        }
        if (tempDataDic.ContainsKey(GroupId))
        {
            tempDataDic.Remove(GroupId);
            tempDataDirty = true;
            SaveTempData();
        }
    }

    //清除某个slot档位
    public void DeleteTempDataTable(int slotID = 0)
    {
        EnsureWritable();
        // Slots are keys inside the shared ES3 file, not separate files.
        // Delete first: a failed storage operation must retain the current in-memory slot.
        ES3.DeleteKey(string.Join("_", TEMP_DATA_FILE_NAME, slotID), es3Setting);
        if (CurrentTempSlotId == slotID)
        {
            CurrentTempSlotId = null;
            tempDataDic?.Clear();
            tempDataDirty = false;
        }
        // A failed index write leaves systemDataDirty set and can be retried safely.
        existTempDataSlotSet.Remove(slotID);
        SetSystemData(EXIST_TEMP_SLOT_TITLE, existTempDataSlotSet, true);
    }

    //清除全部数据
    public void DeleteAllTempDataTable()
    {
        foreach (var item in existTempDataSlotSet.ToList())
        {
            DeleteTempDataTable(item);
        }
    }

    private void SaveSystemData()
    {
        ES3.Save<Dictionary<string, string>>(SYS_DATA_FILE_NAME, systemDataDic, es3Setting);
        systemDataDirty = false;
    }

    private void SaveTempData()
    {
        if (CurrentTempSlotId == null || tempDataDic == null)
        {
            return;
        }
        ES3.Save<Dictionary<int, Dictionary<string, string>>>(
            string.Join("_", TEMP_DATA_FILE_NAME, CurrentTempSlotId), tempDataDic, es3Setting);
        tempDataDirty = false;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Save key cannot be empty.", nameof(key));
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
    private string EncryptString(string plainText, string passPhrase)
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
    private string DecryptString(string cipherText, string passPhrase)
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
