using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Text))]
public class MLT_TextApplier : MonoBehaviour
{
    public MultiLanguageText MLT;
    public MLT_Asset MLT_Asset;

    Text t;
    private void Awake()
    {
        t = GetComponent<Text>();
        if (MLT_Asset != null)
        {
            MLT = MLT_Asset.MLT;
        }

        t.text = MLT.Text;

        EventCenter.Instance.AddEventListener("语言切换", Switch);
    }

    void Switch()
    {
        t.text = MLT.Text;
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener("语言切换", Switch);
    }
}
