using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class MXLockStep : MonoBehaviour
//{

//    //累计经过的时间
//    float accumilateTime;

//    //下一次事件帧时间
//    float nextKeyTime;

//    //事件帧间隔
//    public float keyLen = 0.36f;

//    //下一次逻辑帧时间
//    float nextLogicTime;

//    //当前逻辑帧数
//    float logicNum;

//    //逻辑帧间隔
//    public float logicLen = 0.12f;

//    float renderLerpValue;

//    bool isRequestData;
//    bool isResponseData(){
//        throw new System.Exception("isResponseData not implement");
//    }

//    void LogicUpdate(ref float dt){
//        throw new System.Exception("LogicUpdate not implement");
//    }

//    void RenderUpdate(ref float dt){
//        throw new System.Exception("RenderUpdate not implement");
//    }
 
//    //dt即为渲染帧间隔(可以和unity保持一致即60帧/s)
//    void FixedUpdate(){

//        //计算累计经过的时间
//        accumilateTime += Time.fixedDeltaTime;

//        //累积时间大于关键帧的时候  
//        if (accumilateTime >= nextKeyTime) {
//            //*2*.取得缓存的用户操作并处理
//            if (isRequestData){
//                if(isResponseData())
//                    isRequestData = false;
//                else
//                    return;//如果缓存没数据了则锁定客户端
//            }

//            //更新下一个事件帧时间
//            nextKeyTime = nextKeyTime + keyLen;
//        }

//        //处理逻辑帧(这里while循环是防止某一帧间隔过大跨度了多个逻辑帧的情况)
//        while(accumilateTime > nextLogicTime) {
//            //*3*.执行逻辑处理
//            this.LogicUpdate(ref logicLen);

//            //下一个事件帧到达前
//            //if (nextLogicTime == nextKeyTime - logicLen ){
//            if (nextLogicTime > (nextKeyTime - 2*logicLen)  && nextLogicTime <= (nextKeyTime - logicLen)){
//                isRequestData = true;
//                //*1*.向服务器请求当前用户的所有操作并缓存
//                throw new System.Exception("向服务器请求当前用户的所有操作并缓存 not implement");
//                MXLockStepMsgGetter.StartReq();
//            }

//            //更新当前逻辑帧数和下一次逻辑帧时间
//            logicNum = logicNum + 1;
//            nextLogicTime = nextLogicTime + logicLen;
//        }

//        //设置渲染参数(结果为当前渲染帧在两个逻辑帧之间比值)
//        renderLerpValue = (accumilateTime + logicLen - nextLogicTime) / logicLen;
//        //*4*.调用渲染处理将计算的插值参数传进去
//        this.RenderUpdate(ref renderLerpValue);
//    }
//}

public class MXLockStep : MonoBehaviour
{

}