using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaitForMatchRoomPanel : MonoBehaviour
{
    public Text text;
    public bool canLoad = false;
    public bool canDisplayFailPanel = false;
    public GameObject loading;
    public GameObject matchFail;
    public bool isActive = true;

    void OnEnable()
    {
        StartCoroutine("DisplayLoadingText");
    }

    // Update is called once per frame
    void Update()
    {
        loading.SetActive(canLoad);
        matchFail.SetActive(canDisplayFailPanel);
        if (!isActive)
        {
            isActive = true;
            this.gameObject.SetActive(false);
        }
    }

    IEnumerator DisplayLoadingText()
    {
        int times = 0;
        
        while (true)
        {
            yield return null;
            if (canLoad)
            {
                switch (times)
                {
                    case 0:
                        text.text = "匹配中";
                        times++;
                        break;
                    case 1:
                        text.text = "匹配中.";
                        times++;
                        break;
                    case 2:
                        text.text = "匹配中..";
                        times++;
                        break;
                    case 3:
                        text.text = "匹配中...";
                        times = 0;
                        break;
                    default:
                        times = 0;
                        break;
                }
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}
