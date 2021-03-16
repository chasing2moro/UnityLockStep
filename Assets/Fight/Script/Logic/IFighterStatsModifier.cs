public interface IFighterStatsModifier
{
       // float ModifyAttack(float attack);
       // float ModifyDefend(float defend);
       float ModifyDamage(float damage);
}

public class HitStatsModifier: IFighterStatsModifier{
        public HitStatsModifier(float vAttack){
                this.attack = vAttack;
        }

        public float attack;
        public float ModifyDamage(float damage){
                damage += attack;
                return damage;
        }
}