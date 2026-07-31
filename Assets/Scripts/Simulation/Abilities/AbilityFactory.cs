using UnityEngine;

namespace MCFight
{
    /// <summary> 技能组件工厂：根据类型名创建对应 Ability </summary>
    public static class AbilityFactory
    {
        public static IAbilityComponent Create(string typeName, MonsterDefSO def)
        {
            switch (typeName)
            {
                // Batch 1: 简单升级怪物
                case "DualModeAbility": return new DualModeAbility(def);
                case "TrollAbility": return new TrollAbility(def);
                case "BerserkerAbility": return new BerserkerAbility(def);
                case "ElephantAbility": return new ChargeMeleeAbility(def, 15f, 25f, 3f);
                case "MinoshroomAbility": return new ChargeMeleeAbility(def, 13f, 23f, 3f);
                case "GoblinAbility": return new GoblinAbility(def);
                case "ConeBreathAbility": return new ConeBreathAbility(def);
                case "MagnetronAbility": return new MagnetronAbility(def);
                case "StraddlerAbility": return new StraddlerAbility(def);
                case "StymphalianAbility": return new StymphalianAbility(def);

                // Batch 2: 中等复杂度
                case "WitchAbility": return new WitchAbility(def);
                case "PriestAbility": return new PriestAbility(def);
                case "TarantulaHawkAbility": return new TarantulaHawkAbility(def);
                case "BlazeAbility": return new BlazeAbility(def);
                case "FlyNagaAbility": return new FlyNagaAbility(def);
                case "VexAbility": return new VexAbility(def);
                case "EvokerAbility": return new EvokerAbility(def);
                case "BrainiacAbility": return new BrainiacAbility(def);
                case "MurmurAbility": return new MurmurAbility(def);
                case "SpiderRiderAbility": return new SpiderRiderAbility(def);

                // Batch 3: Boss
                case "WardenAbility": return new WardenAbility(def);
                case "TremorsaurusAbility": return new TremorsaurusAbility(def);
                case "CoralLeapAbility": return new CoralLeapAbility(def);
                case "CyclopsAbility": return new CyclopsAbility(def);
                case "EnderGolemAbility": return new EnderGolemAbility(def);
                case "AmethystCrabAbility": return new AmethystCrabAbility(def);
                case "RevenantAbility": return new RevenantAbility(def);
                case "WarpedMoscoAbility": return new WarpedMoscoAbility(def);
                case "FarseerAbility": return new FarseerAbility(def);
                case "DeepOneMageAbility": return new DeepOneMageAbility(def);
                case "NucleeperAbility": return new NucleeperAbility(def);
                case "DreadLichAbility": return new DreadLichAbility(def);
                case "WadjetAbility": return new WadjetAbility(def);
                case "FrostmawAbility": return new FrostmawAbility(def);
                case "AlphaYetiAbility": return new AlphaYetiAbility(def);
                case "ProwlerAbility": return new ProwlerAbility(def);
                case "ForsakenAbility": return new ForsakenAbility(def);
                case "KobolediatorAbility": return new KobolediatorAbility(def);
                case "TremorzillaAbility": return new TremorzillaAbility(def);
                case "LuxtructosaurusAbility": return new LuxtructosaurusAbility(def);
                case "RemnantAbility": return new RemnantAbility(def);
                case "HarbingerAbility": return new HarbingerAbility(def);
                case "NagaAbility": return new NagaAbility(def);
                case "WarlockAbility": return new WarlockAbility(def);

                default:
                    Debug.LogWarning($"[AbilityFactory] Unknown ability type: {typeName}");
                    return null;
            }
        }
    }
}
