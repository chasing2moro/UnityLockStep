using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MXLockStepMsgGetter
{
    public static UInt64 cacheFrame;


    public static UInt64 stepFrameCount = 5;
    public static UInt64 interleaveFrameCount = 3;
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
        List<FighterServerData> serverDatas = GetNetMsgWithDic(Client.Ins.serverParser.serverDatasMySelf);
        //foreach (var item in serverDatas)
        //{
        //    sb.Append(item.ToString());
        //}
        KartGame.UI.MetaGameController.Ins.myFighter.AddNetMsg(serverDatas);

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
        List<FighterServerData> serverDatas = new List<FighterServerData>();
        foreach (var frameid in frameId2ServerData.Keys)
        {
            if (frameid > cacheFrame && frameid <= cacheFrame + stepFrameCount)
            {
                serverDatas.Add(frameId2ServerData[frameid]);
            }
        }
        serverDatas.Sort((a, b) => (int)(a.frameId - b.frameId));
        return serverDatas;
    }
}
