using UnityEngine;
using System;
using com.unity.mgobe;

public enum DeltaMove {
    Left = -1,
    None = 0,
    Right = 1,
}

public class FighterServerData
{
 
  
    

    public DeltaMove deltaMove;
    public bool isPlayHitAction;
    public Vector2 position;
    public ulong frameId;
    public bool isMySelf;

    public override string ToString()
    {
        return "[" + deltaMove.ToString() + "," + position.ToString() + " frameid:" + frameId + "]";
    }

    public FighterServerData(){
        position = Vector2.zero;
    }

    public SendFramePara Serialize(){
        SendFramePara para = new SendFramePara {
            Data = string.Format("{0},{1},{2:0.###},{3:0.###},{4}", (int)deltaMove, isPlayHitAction ? 1:0, position.x, position.y, frameId)
            //$"{acceleration},{steering},{game.myKartInfo.Position.x:0.###},{game.myKartInfo.Position.y:0.###},{game.myKartInfo.Position.z:0.###},{game.myKartInfo.Rotation.x:0.####},{game.myKartInfo.Rotation.y:0.####},{game.myKartInfo.Rotation.z:0.####},{game.myKartInfo.Rotation.w:0.####}"
        };
        return para;
    }

    public void Deserialize(string str){
        string[] vals = str.Split (',');
        if (vals.Length == 5) {
            int intDeltaMove;
            int intIsPlayHitAction;
            float px, py;
            if (Int32.TryParse (vals[0], out intDeltaMove) &&
                Int32.TryParse (vals[1], out intIsPlayHitAction) &&
                float.TryParse (vals[2], out px) &&
                float.TryParse (vals[3], out py) &&
                ulong.TryParse (vals[4], out frameId)) {
                    deltaMove = (DeltaMove)intDeltaMove;
                    isPlayHitAction = intIsPlayHitAction == 1;
                    position.x = px;
                    position.y = py;
            }
        }
    }
}