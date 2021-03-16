using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : MonoBehaviour
{
    public Text logText;

    public string[] msgs;
    const int MsgCap = 20;
    int curIndex = 0;

    private void Start()
    {
        msgs = new string[20];
        curIndex = 0;
    }

    // Start is called before the first frame update
    public void LogMsg(string msg)
    {
        if (!isActiveAndEnabled)
            return;

        msgs[curIndex] = msg;

        string str = "";
        for (int i = 0; i < MsgCap; ++i)
        {
            int idx = curIndex >= i ? curIndex - i : curIndex + MsgCap - i;
            if (msgs[idx] != null)
                str += msgs[idx];
        }

        logText.text = str;
        logText.GetComponent<RectTransform>().sizeDelta = new Vector2(logText.preferredWidth, logText.preferredHeight);

        curIndex = (curIndex+1) % MsgCap;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
