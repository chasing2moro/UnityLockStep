using UnityEngine;
using System.Collections.Generic;

public class  FighterLogic : MonoBehaviour, IFighterInfo
{
    Vector2 _postion;//横版格斗游戏，没有z轴方向
    float _hp;

    public Vector2 postion{get=>_postion;}
    public float hp{get=>_hp;}

    public FighterStats defaultStats;

    public List<IFighterStatsModifier> currentModifiers;
    public List<IFighterStatsModifier> tempModifiers;

    public FighterStats modifiedStats;
   

    public bool isHitting{
        get{
            return timeStartHit > 0;
        }
    }
    float timeStartHit = -1;
    bool isDamageOther = false;//别人是否因为我的攻击，受到伤害了

    float accumilateTime = 0;

    DeltaMove deltaMove = DeltaMove.None;
    
    public void HandleData(FighterServerData data){
       if(!this.isHitting){
           if(data.isPlayHitAction){
                this.timeStartHit = this.accumilateTime;
                this.isDamageOther = false;
           }
       }
       deltaMove = data.deltaMove;
        Debug.Log("deltaMove:" + deltaMove);
    }
    public FighterLogic otherFighter;

    void Awake()
    {
        currentModifiers = new List<IFighterStatsModifier>();
        tempModifiers = new List<IFighterStatsModifier>();
        defaultStats = new FighterStats();
        modifiedStats = new FighterStats();
    }

    //外部调用
    public void OnUpdate(ref float dt){
        this.accumilateTime += dt;


        this.UpdateModifier();
        this.UpdateMove();
        this.UpdateHit();
        this.UpdateHP();
    }

    void UpdateModifier(){
        FighterStats.GetModifiedStats(currentModifiers, defaultStats, modifiedStats);
        ClearTempModifiers();
    }

    void UpdateMove()
    {
        switch (deltaMove)
        {
            case DeltaMove.Left:
                this.transform.Translate(Vector3.left * 0.1f, Space.Self);
                break;
            case DeltaMove.None:
                break;
            case DeltaMove.Right:
                this.transform.Translate(Vector3.right * 0.1f, Space.Self);
                break;
            default:
                break;
        }
        deltaMove = DeltaMove.None;
    }

    void UpdateHit(){
        if(this.isHitting){
            float hitDuration = this.accumilateTime - this.timeStartHit;
            if(!this.isDamageOther && hitDuration <= modifiedStats.attackDurationHalf){
                //出拳

                //两个人之间的距离
                float distanceEachOther = Vector2.Distance(this.postion, this.otherFighter.postion);

                //拳头伸出的距离
                float distanceStretchedOut = modifiedStats.attackDistance * hitDuration/modifiedStats.attackDurationHalf;

                if(distanceStretchedOut >= distanceEachOther){
                    this.isDamageOther = true;
                    otherFighter.ApplyHittedModifier(new HitStatsModifier(modifiedStats.attack));
                }
            }else if(hitDuration <= modifiedStats.attackDuration){
                //收拳，目前没用
            }else{
                //攻击完成
                this.timeStartHit = -1;
                this.isDamageOther = false;
            }
        }
    }

    void UpdateHP(){
        if(this.modifiedStats.damage > 0){
            float takeDamage = this.modifiedStats.defend - this.modifiedStats.damage;
            if(takeDamage < 0){
                //伤害 破甲了，要扣血(加负数，就是减血)
                this._hp += takeDamage;
            }
        }
    }

 

    void ClearTempModifiers () {
        for (int i = 0; i < tempModifiers.Count; i++) {
            currentModifiers.Remove (tempModifiers[i]);
        }

        tempModifiers.Clear ();
    }

        /// <summary>
        /// Checks and applies the modifier to the kart's stats if the kart is not grounded.
        /// </summary>
        void ApplyHittedModifier (IFighterStatsModifier modifier) {
             currentModifiers.Add (modifier);
             tempModifiers.Add(modifier);
        }
    
}