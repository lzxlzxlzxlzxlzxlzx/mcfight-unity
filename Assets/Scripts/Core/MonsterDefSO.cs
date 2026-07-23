using UnityEngine;

namespace MCFight
{
    /// <summary> 怪物基础数据定义（ScriptableObject，在 Inspector 中配置） </summary>
    [CreateAssetMenu(fileName = "Monster_", menuName = "MC Fight/Monster Definition")]
    public class MonsterDefSO : ScriptableObject
    {
        [Header("基本信息")]
        public string monsterId;
        public string displayName;
        public int price;
        [TextArea] public string description;

        [Header("战斗属性")]
        public float hp = 100;
        public float attack = 10;
        public float armor = 0;
        public float armorToughness = 0;
        public float moveSpeed = 58;
        public float attackRange = 42;
        public float attackInterval = 0.85f;
        public float radius = 18;
        public MoveType moveType = MoveType.Ground;
        public AttackType attackType = AttackType.Melee;

        [Header("标签")]
        public string[] tags;

        [Header("命中附带状态")]
        public StatusEffectType[] onHitEffects;

        [Header("技能配置（可选）")]
        [Tooltip("Boss 的特殊技能组件类型名，留空则使用通用攻击模式")]
        public string abilityComponentType;

        [Header("精灵图")]
        public Sprite idleSprite;
        public Sprite attackSprite;
        public Sprite deadSprite;

        /// <summary> 是否拥有指定标签 </summary>
        public bool HasTag(string tag)
        {
            if (tags == null) return false;
            for (int i = 0; i < tags.Length; i++)
                if (tags[i] == tag) return true;
            return false;
        }
    }
}
