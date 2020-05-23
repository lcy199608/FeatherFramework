using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SaveTest : MonoBehaviour
{
    public List<SQLIdHolder> sqls;
    public InputField id;
    public InputField value;
    public InputField slot;
    private int Slot => Convert.ToInt32(slot.text);
    public InputField groupId;
    private int GroupId => Convert.ToInt32(groupId.text);
    public Toggle saveNow;
    public Text showText;

    public void InitData()
    {
        SaveHandler.Initialize();
    }

    public void LoadData()
    {
        SaveHandler.LoadTempData(Slot);
        Debug.Log("Load");
    }

    public void SetSysData(/*string id,T value,bool saveNow = true*/)
    {
        SaveHandler.SetSystemData(id.text, value.text, saveNow.isOn);
        Debug.Log("SetSysData");
    }

    public void GetSysData(/*string id,T def*/)
    {
         showText.text = SaveHandler.GetSystemData(id.text ,value.text);
        Debug.Log("GetSysData");
    }

    public void SetTempData(/*string id,T value,bool saveNow = true,int groupId = 0*/)
    {
        SaveHandler.SetTempData(id.text,value.text,saveNow.isOn, GroupId);
        Debug.Log("SetTempData");
    }

    public void GetTempData(/*string id,T def,int groupId*/)
    {
        showText.text = SaveHandler.GetTempData(id.text, value.text, GroupId);
        Debug.Log("GetTempData");
    }

    public void GetValue(/*SQLIdHolder key, T defaultValue*/)
    {
        showText.text = SaveHandler.GetValue(sqls[Convert.ToInt32(id.text)], value.text);
        Debug.Log("GetValue");
    }

    public void SetValue(/*SQLIdHolder key, T value, bool SaveImmediately = true*/)
    {
        SaveHandler.SetValue(sqls[Convert.ToInt32(id.text)], value.text, saveNow.isOn);
        Debug.Log("SetValue");
    }

    public void ApplyChangesToDatabase()
    {
        SaveHandler.ApplyChangesToDatabase();
        Debug.Log("ApplyChangesToDatabase");
    }

    public void DeleteTempDataGroup(/*int GroupId = 0*/)
    {
        SaveHandler.DeleteTempDataGroup(GroupId);
        Debug.Log("DeleteTempDataGroup");
    }

    public void DeleteTempDataTable(/*int slotID = 0*/)
    {
        SaveHandler.DeleteTempDataTable(Slot);
        Debug.Log("DeleteTempDataTable");
    }

    public void DeleteAllTempDataTable()
    {
        SaveHandler.DeleteAllTempDataTable();
        Debug.Log("DeleteAllTempDataTable");
    }
}
