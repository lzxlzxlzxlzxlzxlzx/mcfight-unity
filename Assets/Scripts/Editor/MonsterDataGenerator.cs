using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace MCFight.EditorTools
{
    /// <summary>
    /// 怪物数据生成器：一键生成全部 MonsterDefSO .asset 文件
    /// 菜单：MC Fight > Generate All Monster Data
    /// </summary>
    public static class MonsterDataGenerator
    {
        const string OUTPUT_DIR = "Assets/Resources/Monsters";

        [MenuItem("MC Fight/Generate All Monster Data")]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Monsters"))
                AssetDatabase.CreateFolder("Assets/Resources", "Monsters");

            var monsters = GetAllMonsterData();
            int created = 0, updated = 0;

            foreach (var data in monsters)
            {
                string path = $"{OUTPUT_DIR}/Monster_{data.monsterId}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<MonsterDefSO>(path);
                if (existing != null)
                {
                    ApplyData(existing, data);
                    EditorUtility.SetDirty(existing);
                    updated++;
                }
                else
                {
                    var so = ScriptableObject.CreateInstance<MonsterDefSO>();
                    ApplyData(so, data);
                    AssetDatabase.CreateAsset(so, path);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MonsterDataGenerator] Done! Created: {created}, Updated: {updated}, Total: {monsters.Count}");
        }

        static void ApplyData(MonsterDefSO so, MonsterData data)
        {
            so.monsterId = data.monsterId;
            so.displayName = data.displayName;
            so.price = data.price;
            so.description = data.description;
            so.hp = data.hp;
            so.attack = data.attack;
            so.armor = data.armor;
            so.armorToughness = data.armorToughness;
            so.moveSpeed = data.moveSpeed;
            so.attackRange = data.attackRange;
            so.attackInterval = data.attackInterval;
            so.radius = data.radius;
            so.moveType = data.moveType;
            so.attackType = data.attackType;
            so.tags = data.tags;
            so.onHitEffects = data.onHitEffects;
            so.abilityComponentType = data.abilityComponentType;
        }

        // ========== 数据定义 ==========
        class MonsterData
        {
            public string monsterId, displayName, description;
            public int price;
            public float hp, attack, armor, armorToughness, moveSpeed, attackRange, attackInterval, radius;
            public MoveType moveType;
            public AttackType attackType;
            public string[] tags;
            public StatusEffectType[] onHitEffects;
            public string abilityComponentType;

            public static MonsterData Make(
                string id, string name, int price,
                float hp, float atk, float arm, float tough,
                float spd, float range, float interval, float rad,
                MoveType mv, AttackType at,
                string[] tags = null,
                StatusEffectType[] onHit = null,
                string abilityType = "")
            {
                return new MonsterData
                {
                    monsterId = id, displayName = name, price = price,
                    hp = hp, attack = atk, armor = arm, armorToughness = tough,
                    moveSpeed = spd, attackRange = range, attackInterval = interval, radius = rad,
                    moveType = mv, attackType = at,
                    tags = tags ?? System.Array.Empty<string>(),
                    onHitEffects = onHit ?? System.Array.Empty<StatusEffectType>(),
                    abilityComponentType = abilityType,
                    description = "",
                };
            }
        }

        static List<MonsterData> GetAllMonsterData()
        {
            var list = new List<MonsterData>();

            // ====== 26 Boss ======
            list.Add(MonsterData.Make("alexscaves_tremorzilla", "撼地斯拉", 1000, 500, 30, 10, 0, 48, 58, 1.0f, 56, MoveType.Ground, AttackType.Melee, new[] { "boss", "aoe_melee", "giant", "beam_skill" }, new[] { StatusEffectType.Poison }, "TremorzillaAbility"));
            list.Add(MonsterData.Make("alexscaves_luxtructosaurus", "暝煌龙", 800, 600, 12, 20, 0, 24, 55, 2.2f, 56, MoveType.Ground, AttackType.Melee, new[] { "boss", "lux_boss", "giant", "fire_immune", "meteor_passive" }, new[] { StatusEffectType.Burn }, "LuxtructosaurusAbility"));
            list.Add(MonsterData.Make("cataclysm_ancient_remnant", "远古遗魂", 700, 420, 22, 12, 0, 36, 55, 3f, 56, MoveType.Ground, AttackType.Melee, new[] { "boss", "remnant_boss", "giant" }, null, "RemnantAbility"));
            list.Add(MonsterData.Make("cataclysm_the_harbinger", "先驱者", 600, 390, 16, 12, 0, 64, 240, 2f, 28, MoveType.Fly, AttackType.Ranged, new[] { "boss", "harbinger_boss", "fly" }, null, "HarbingerAbility"));
            list.Add(MonsterData.Make("warden", "监守者", 400, 500, 30, 0, 0, 78, 48, 1.5f, 22, MoveType.Ground, AttackType.Melee, new[] { "warden_special" }, null, "WardenAbility"));
            list.Add(MonsterData.Make("cataclysm_kobolediator", "骸骨斩首者", 250, 180, 14, 10, 0, 56, 260, 3f, 20, MoveType.Ground, AttackType.Melee, new[] { "boss", "kobo_boss", "kobo_block_ranged" }, null, "KobolediatorAbility"));
            list.Add(MonsterData.Make("alexscaves_atlatitan", "擎天龙", 200, 400, 8, 0, 0, 30, 48, 2f, 24, MoveType.Ground, AttackType.Melee, new[] { "atlatitan_unit", "aoe_melee" }, null, ""));
            list.Add(MonsterData.Make("alexscaves_tremorsaurus", "撼地龙", 200, 150, 14, 8, 0, 50, 42, 0.7f, 18, MoveType.Ground, AttackType.Melee, new[] { "tremorsaurus_special" }, null, "TremorsaurusAbility"));
            list.Add(MonsterData.Make("cataclysm_ender_golem", "末影傀儡", 200, 120, 13, 12, 0, 40, 42, 2f, 20, MoveType.Ground, AttackType.Melee, new[] { "ender_golem_boss" }, null, "EnderGolemAbility"));
            list.Add(MonsterData.Make("alexsmobs_warped_mosco", "诡异蚊鬼", 180, 100, 15, 10, 0, 58, 42, 3f, 18, MoveType.Ground, AttackType.Melee, new[] { "mosco_special", "arthropod" }, null, "WarpedMoscoAbility"));
            list.Add(MonsterData.Make("cataclysm_amethyst_crab", "紫水晶巨蟹", 180, 200, 16, 10, 0, 30, 48, 1f, 22, MoveType.Ground, AttackType.Melee, new[] { "amethyst_crab_special" }, null, "AmethystCrabAbility"));
            list.Add(MonsterData.Make("cataclysm_ignited_revenant", "炽燃遗魂", 180, 80, 6, 12, 0, 42, 160, 1f, 18, MoveType.Ground, AttackType.Melee, new[] { "fire_immune", "revenant_special" }, new[] { StatusEffectType.Burn }, "RevenantAbility"));
            list.Add(MonsterData.Make("cataclysm_coralssus", "珊瑚巨兽", 150, 150, 11.5f, 5, 0, 30, 200, 3f, 22, MoveType.Ground, AttackType.Melee, new[] { "coral_leap_special" }, null, "CoralLeapAbility"));
            list.Add(MonsterData.Make("cataclysm_wadjet", "瓦吉特", 150, 150, 11, 5, 0, 52, 240, 2f, 20, MoveType.Ground, AttackType.Melee, new[] { "wadjet_boss", "aoe_melee" }, null, "WadjetAbility"));
            list.Add(MonsterData.Make("iceandfire_cyclops", "独眼巨人", 150, 150, 17, 20, 0, 48, 42, 2f, 22, MoveType.Ground, AttackType.Melee, new[] { "cyclops_special" }, null, "CyclopsAbility"));
            list.Add(MonsterData.Make("mowziesmobs_frostmaw", "霜冻巨兽", 150, 250, 8, 6, 0, 44, 220, 3f, 28, MoveType.Ground, AttackType.Melee, new[] { "boss", "frostmaw_special" }, new[] { StatusEffectType.Slow }, "FrostmawAbility"));
            list.Add(MonsterData.Make("twilightforest_alpha_yeti", "雪怪首领", 150, 200, 7, 0, 0, 40, 220, 1.5f, 24, MoveType.Ground, AttackType.Ranged, new[] { "alpha_yeti_special" }, new[] { StatusEffectType.Slow }, "AlphaYetiAbility"));
            list.Add(MonsterData.Make("cataclysm_the_prowler", "徘徊者", 140, 160, 7, 10, 0, 52, 240, 3f, 18, MoveType.Ground, AttackType.Melee, new[] { "prowler_special" }, null, "ProwlerAbility"));
            list.Add(MonsterData.Make("alexsmobs_farseer", "瞻远者", 120, 70, 6, 6, 0, 50, 200, 1.5f, 16, MoveType.Fly, AttackType.Ranged, new[] { "farseer_special", "fly" }, null, "FarseerAbility"));
            list.Add(MonsterData.Make("twilightforest_naga", "娜迦", 120, 200, 6, 0, 0, 80, 42, 0.1f, 12, MoveType.Ground, AttackType.Melee, new[] { "naga_special" }, null, "NagaAbility"));
            list.Add(MonsterData.Make("alexscaves_deep_one_mage", "深潜者法师", 100, 80, 4, 0, 0, 60, 220, 1.2f, 16, MoveType.Fly, AttackType.Ranged, new[] { "deep_one_mage_special", "fly" }, null, "DeepOneMageAbility"));
            list.Add(MonsterData.Make("alexscaves_nucleeper", "核能苦力怕", 100, 30, 500, 4, 0, 42, 200, 99f, 14, MoveType.Ground, AttackType.Melee, new[] { "nucleeper_special" }, new[] { StatusEffectType.Poison }, "NucleeperAbility"));
            list.Add(MonsterData.Make("cataclysm_deepling_warlock", "渊灵术士", 50, 45, 14, 0, 0, 30, 260, 10f, 14, MoveType.Ground, AttackType.Ranged, new[] { "warlock_special" }, null, "WarlockAbility"));
            list.Add(MonsterData.Make("iceandfire_dread_lich", "悚怖尸巫", 100, 50, 6, 2, 0, 40, 200, 1f, 16, MoveType.Ground, AttackType.Ranged, new[] { "dread_lich_special" }, null, "DreadLichAbility"));
            list.Add(MonsterData.Make("alexscaves_forsaken", "遗弃者", 150, 250, 12, 0, 0, 62, 1280, 2f, 20, MoveType.Ground, AttackType.Melee, new[] { "forsaken_special" }, null, "ForsakenAbility"));
            list.Add(MonsterData.Make("cataclysm_coral_golem", "珊瑚傀儡", 80, 110, 12.5f, 5, 0, 30, 200, 3f, 18, MoveType.Ground, AttackType.Melee, new[] { "coral_leap_special" }, null, "CoralLeapAbility"));

            // ====== 30 升级怪物 ======
            list.Add(MonsterData.Make("alexsmobs_elephant", "大象", 80, 110, 15, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, null, null, "ElephantAbility"));
            list.Add(MonsterData.Make("cataclysm_deepling_brute", "渊灵蛮兵", 60, 60, 14, 8, 0, 50, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged, null, null, "DualModeAbility"));
            list.Add(MonsterData.Make("cataclysm_ignited_berserker", "炽燃狂魂", 60, 65, 14, 8, 0, 40, 42, 3f, 18, MoveType.Ground, AttackType.Melee, new[] { "aoe_melee" }, new[] { StatusEffectType.Burn }, "BerserkerAbility"));
            list.Add(MonsterData.Make("iceandfire_if_troll", "食人妖", 60, 50, 10, 9, 0, 50, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, new[] { "knockback_immune", "troll_immune_ranged" }, null, "TrollAbility"));
            list.Add(MonsterData.Make("evoker", "唤魔者", 50, 24, 6, 0, 0, 40, 160, 1.1f, 16, MoveType.Ground, AttackType.Ranged, new[] { "summoner" }, null, "EvokerAbility"));
            list.Add(MonsterData.Make("twilightforest_minoshroom", "米诺菇", 50, 160, 13, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, null, null, "MinoshroomAbility"));
            list.Add(MonsterData.Make("alexscaves_magnetron", "磁控机兵", 40, 80, 2, 6, 0, 50, 42, 1.5f, 18, MoveType.Ground, AttackType.Melee, null, null, "MagnetronAbility"));
            list.Add(MonsterData.Make("alexscaves_deep_one_knight", "深潜者骑士", 35, 60, 10, 0, 0, 50, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged, null, null, "DualModeAbility"));
            list.Add(MonsterData.Make("cataclysm_deepling_priest", "渊灵祭司", 35, 45, 8, 0, 0, 40, 96, 6f, 18, MoveType.Ground, AttackType.Ranged, null, null, "PriestAbility"));
            list.Add(MonsterData.Make("alexsmobs_centipede_head", "洞穴蜈蚣", 30, 35, 8, 6, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, new[] { "arthropod" }, new[] { StatusEffectType.Poison }));
            list.Add(MonsterData.Make("cataclysm_deepling", "渊灵", 25, 26, 9.5f, 0, 0, 40, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged, null, null, "DualModeAbility"));
            list.Add(MonsterData.Make("alexsmobs_murmur", "轻语灵", 25, 15, 5, 0, 0, 30, 200, 1.1f, 16, MoveType.Ground, AttackType.Ranged, null, null, "MurmurAbility"));
            list.Add(MonsterData.Make("cataclysm_the_watcher", "观测者", 25, 25, 4, 5, 0, 20, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged, null, new[] { StatusEffectType.Burn }));
            list.Add(MonsterData.Make("alexsmobs_straddler", "跨座兽", 20, 28, 3, 5, 0, 30, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged, null, null, "StraddlerAbility"));
            list.Add(MonsterData.Make("iceandfire_if_cockatrice", "鸡蛇", 20, 40, 2, 2, 0, 40, 160, 0.4f, 18, MoveType.Ground, AttackType.Ranged, null, new[] { StatusEffectType.Wither }));
            list.Add(MonsterData.Make("twilightforest_blockchain_goblin", "链锤哥布林", 20, 20, 8, 0, 0, 40, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, new[] { "aoe_melee" }, null, "GoblinAbility"));
            list.Add(MonsterData.Make("witch", "女巫", 20, 26, 6, 0, 0, 40, 160, 2f, 18, MoveType.Ground, AttackType.Ranged, null, null, "WitchAbility"));
            list.Add(MonsterData.Make("twilightforest_winter_wolf", "寒冬狼", 16, 30, 4, 0, 0, 58, 64, 2f, 18, MoveType.Ground, AttackType.Ranged, null, new[] { StatusEffectType.Slow }, "ConeBreathAbility"));
            list.Add(MonsterData.Make("alexsmobs_tarantula_hawk", "沙漠蛛蜂", 15, 18, 5, 4, 0, 72, 42, 2f, 16, MoveType.Fly, AttackType.Melee, new[] { "fly", "arthropod" }, null, "TarantulaHawkAbility"));
            list.Add(MonsterData.Make("iceandfire_stymphalianbird", "铜羽泽鹗", 15, 24, 1, 4, 0, 90, 100, 1f, 16, MoveType.Fly, AttackType.Ranged, new[] { "fly", "armor_piercing" }, null, "StymphalianAbility"));
            list.Add(MonsterData.Make("twilightforest_king_spider", "国王蜘蛛", 12, 30, 6, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, new[] { "arthropod" }, null, "SpiderRiderAbility"));
            list.Add(MonsterData.Make("blaze", "烈焰人", 10, 20, 5, 0, 0, 20, 160, 5f, 16, MoveType.Fly, AttackType.Ranged, new[] { "fly" }, new[] { StatusEffectType.Burn }, "BlazeAbility"));
            list.Add(MonsterData.Make("mowziesmobs_naga", "娜迦(飞行)", 10, 30, 4, 0, 0, 72, 160, 3f, 16, MoveType.Fly, AttackType.Ranged, new[] { "fly" }, new[] { StatusEffectType.Poison }, "FlyNagaAbility"));
            list.Add(MonsterData.Make("stray", "流浪者", 9, 20, 3, 0, 0, 40, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged, null, new[] { StatusEffectType.Slow }));
            list.Add(MonsterData.Make("wither_skeleton", "凋零骷髅", 8, 20, 8, 0, 0, 40, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, null, new[] { StatusEffectType.Wither }));
            list.Add(MonsterData.Make("pillager", "掠夺者", 6, 24, 4, 0, 0, 40, 160, 2f, 18, MoveType.Ground, AttackType.Ranged));
            list.Add(MonsterData.Make("twilightforest_fire_beetle", "喷火甲虫", 6, 25, 4, 0, 0, 50, 64, 2f, 18, MoveType.Ground, AttackType.Ranged, new[] { "arthropod" }, new[] { StatusEffectType.Burn }, "ConeBreathAbility"));
            list.Add(MonsterData.Make("twilightforest_death_tome", "死灵书", 5, 30, 6, 0, 0, 30, 160, 2f, 16, MoveType.Fly, AttackType.Ranged, new[] { "fly" }));
            list.Add(MonsterData.Make("twilightforest_skeleton_druid", "骷髅德鲁伊", 5, 20, 2, 0, 0, 30, 160, 1.1f, 14, MoveType.Ground, AttackType.Ranged, null, new[] { StatusEffectType.Poison }));

            // ====== 22 普通怪物 ======
            list.Add(MonsterData.Make("twilightforest_armored_giant", "武装巨人", 120, 80, 6, 15, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("iron_golem", "铁傀儡", 100, 100, 14, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("twilightforest_giant_miner", "矿工巨人", 100, 80, 4, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("cataclysm_modern_remnant", "现世遗魂", 80, 120, 6, 5, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("ravager", "劫掠兽", 70, 100, 12, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("alexscaves_relicheirus", "遗迹恐手龙", 60, 120, 12, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("alexsmobs_rhinoceros", "犀牛", 50, 60, 8, 12, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("alexscaves_brainiac", "舐脑魔", 40, 40, 5, 8, 0, 40, 160, 1.1f, 18, MoveType.Ground, AttackType.Melee, null, new[] { StatusEffectType.Poison }, "BrainiacAbility"));
            list.Add(MonsterData.Make("alexsmobs_grizzly_bear", "灰熊", 40, 55, 8, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("alexsmobs_tiger", "老虎", 40, 50, 6, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("alexsmobs_bison", "野牛", 30, 40, 8, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("alexsmobs_tusklin", "獠牙兽", 30, 40, 9, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("twilightforest_tower_golem", "砷铅铁傀儡", 30, 40, 9, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("alexscaves_teleto", "磁流灵", 25, 18, 6, 0, 0, 72, 160, 1.1f, 16, MoveType.Fly, AttackType.Ranged, new[] { "fly" }));
            list.Add(MonsterData.Make("creeper", "苦力怕", 20, 20, 49, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee, new[] { "explosive" }));
            list.Add(MonsterData.Make("vindicator", "卫道士", 20, 24, 13, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("twilightforest_minotaur", "牛头人", 16, 30, 8, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("cataclysm_koboleton", "骷髅狗头人", 15, 25, 7, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("twilightforest_mist_wolf", "迷雾狼", 13, 30, 4, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("twilightforest_slime_beetle", "粘液甲虫", 12, 25, 8, 0, 0, 58, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged, new[] { "arthropod" }));
            list.Add(MonsterData.Make("alexscaves_deep_one", "深潜者", 10, 30, 3, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("skeleton", "骷髅", 8, 20, 3, 0, 0, 40, 160, 1.1f, 18, MoveType.Ground, AttackType.Ranged));
            list.Add(MonsterData.Make("alexscaves_vallumraptor", "阔鼻迅猛龙", 8, 30, 3, 0, 0, 58, 42, 0.85f, 18, MoveType.Ground, AttackType.Melee));

            // ====== 6 召唤物（price=0） ======
            list.Add(MonsterData.Make("vex", "恼鬼", 0, 14, 13, 0, 0, 100, 42, 5f, 12, MoveType.Fly, AttackType.Melee, new[] { "fly" }, null, "VexAbility"));
            list.Add(MonsterData.Make("stradpole", "跨座蝌蚪", 0, 4, 0, 0, 0, 58, 0, 99f, 8, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("dread_thrall", "悚怖尸奴", 0, 20, 6, 2, 0, 52, 42, 1f, 12, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("dread_beast", "悚怖尸兽", 0, 30, 4, 1, 0, 72, 42, 1f, 14, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("dread_ghoul", "悚怖食尸鬼", 0, 30, 5, 4, 0, 68, 42, 1f, 14, MoveType.Ground, AttackType.Melee));
            list.Add(MonsterData.Make("dread_spider", "悚怖劫蛛", 0, 40, 7, 10, 0, 64, 42, 1f, 16, MoveType.Ground, AttackType.Melee, new[] { "arthropod" }));

            return list;
        }
    }
}
