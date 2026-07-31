using System.Collections.Generic;
using System.Linq;

namespace MCFight.BalanceLab
{
    /// <summary> 从 MonsterDatabase 构建快照 + 按意图筛选 </summary>
    public static class MonsterCatalog
    {
        public static MonsterCatalogSnapshot BuildSnapshot(MonsterDatabase db)
        {
            var snap = new MonsterCatalogSnapshot();
            var all = db.GetAllSortedByPrice();
            snap.TotalCount = all.Count;

            foreach (var def in all)
            {
                var detail = new MonsterDetail
                {
                    MonsterId = def.monsterId,
                    DisplayName = def.displayName,
                    Price = def.price,
                    Hp = def.hp,
                    Attack = def.attack,
                    Armor = def.armor,
                    AttackType = def.attackType.ToString(),
                    MoveType = def.moveType.ToString(),
                    Tags = def.tags,
                    AbilityType = def.abilityComponentType
                };
                snap.Units.Add(detail);

                if (!snap.CountByPrice.ContainsKey(def.price))
                    snap.CountByPrice[def.price] = 0;
                snap.CountByPrice[def.price]++;
            }
            return snap;
        }

        public static List<MonsterDetail> FilterByIntent(MonsterCatalogSnapshot snap, IntentPreview intent)
        {
            var result = new List<MonsterDetail>();

            // 如果指定了具体单位，直接返回
            if (intent.MentionedUnitIds.Count > 0 && !intent.IsMatrix)
            {
                foreach (var u in snap.Units)
                    if (intent.MentionedUnitIds.Contains(u.MonsterId))
                        result.Add(u);
                return result;
            }

            // 按筛选条件过滤
            foreach (var u in snap.Units)
            {
                if (u.Price <= 0) continue;

                if (intent.TargetPrice.HasValue && u.Price != intent.TargetPrice.Value)
                    continue;
                if (intent.PriceMin.HasValue && u.Price < intent.PriceMin.Value)
                    continue;
                if (intent.PriceMax.HasValue && u.Price > intent.PriceMax.Value)
                    continue;
                if (intent.AttackTypeFilter != null && u.AttackType != intent.AttackTypeFilter)
                    continue;
                if (intent.MoveTypeFilter != null && u.MoveType != intent.MoveTypeFilter)
                    continue;

                result.Add(u);
            }

            return result;
        }
    }
}
