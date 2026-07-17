using System.Collections;
using System.Collections.Generic;
using UI.Framework.Base;
using TMPro;
using UnityEngine;

public class CharactorInfoItem : UIWidget
{
    [SerializeField]
    private TextMeshProUGUI content;

    /// <summary>
    /// 设置内容文本
    /// </summary>
    public void SetContent(string text)
    {
        if (content != null)
            content.text = text;
    }
}