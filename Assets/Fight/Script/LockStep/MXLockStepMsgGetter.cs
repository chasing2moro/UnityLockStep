using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MXLockStepMsgGetter
{
    public static UInt64 cacheFrame;


    public static UInt64 stepFrameCount = 5;
    public static UInt64 interleaveFrameCount = 4;
    public static UInt64 periodFrameCount
    {
        get=>MXLockStepMsgGetter.stepFrameCount + MXLockStepMsgGetter.interleaveFrameCount;
    }

    public static void Init()
    {
        cacheFrame = 0;
    }


    public static void GetNetMsg()
    {
        //System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //sb.AppendLine("myself:");
        //自己的网络消息
        List<FighterServerData> serverDatas = GetNetMsgWithDic(Client.Ins.serverParser.serverDatasMySelf);
        KartGame.UI.MetaGameController.Ins.myFighter.AddNetMsg(serverDatas);
        //foreach (var item in serverDatas)
        //{
        //    sb.Append(item.ToString());
        //}

        //别人的网络消息
        serverDatas = GetNetMsgWithDic(Client.Ins.serverParser.serverDatasOther);
        KartGame.UI.MetaGameController.Ins.otherFighter.AddNetMsg(serverDatas);
        //sb.AppendLine();
        //sb.AppendLine("other:");
        //foreach (var item in serverDatas)
        //{
        //    sb.Append(item.ToString());
        //}
        //Debug.Log(sb.ToString());
    }

     static List<FighterServerData> GetNetMsgWithDic(Dictionary<UInt64, FighterServerData>  frameId2ServerData)
    {
        Dictionary<UInt64, bool> frameid2bool = new Dictionary<ulong, bool>();
        for (UInt64 i = cacheFrame + 1; i <= cacheFrame + stepFrameCount; i++)
        {
            frameid2bool[i] = false;
        }
        bool isMyself = false;

        List<FighterServerData> serverDatas = new List<FighterServerData>();
        if (frameId2ServerData.Count == 0)
            return serverDatas;
        foreach (var frameid in frameId2ServerData.Keys)
        {
            if (frameid > cacheFrame && frameid <= cacheFrame + stepFrameCount)
            {
                serverDatas.Add(frameId2ServerData[frameid]);
                frameid2bool[frameid] = true;
            }
            isMyself = frameId2ServerData[frameid].isMySelf;
        }

        foreach (var k in frameid2bool.Keys)
        {
            if (frameid2bool[k] == false)
            {
                Debug.LogError((isMyself ? "自己":"别人") + "丢失frameid:" + k);
            }
        }

        serverDatas.Sort((a, b) => (int)(a.frameId - b.frameId));
        return serverDatas;
    }
}
