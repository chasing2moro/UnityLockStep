using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FighterStats 
{
    public float attack;//攻击力
    public float attackDistance;//攻击距离
    public float attackDuration;//攻击周期second
    public float attackDurationHalf{
        get => attackDuration/2;
    }
    public float defend;//防御力
    
    public float damage;//默认为0，hp扣多少，就是靠这个字段

    public void InitDefault(){
        this.attack            = 10;
        this.attackDistance    = 10;
        this.attackDuration    = 1;//second
        this.defend            = 6;
        this.damage            = 0;
    }

     public static void GetModifiedStats (List<IFighterStatsModifier> modifiers, FighterStats startingStats,  FighterStats modifiedStats)
    {
        modifiedStats.attack            = startingStats.attack;
        modifiedStats.attackDistance    = startingStats.attackDistance;
        modifiedStats.attackDuration    = startingStats.attackDuration;
        modifiedStats.defend            = startingStats.defend;
        modifiedStats.damage            = startingStats.damage;


        for (int i = 0; i < modifiers.Count; i++)
        {
            //Modify attack、attackDistance、attackDuration、defend is implementing later
            modifiedStats.damage = modifiers[i].ModifyDamage (modifiedStats.damage);
        }
    }
    
}
