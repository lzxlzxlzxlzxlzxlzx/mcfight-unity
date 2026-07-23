using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary>
    /// 怪物配置加载器：从 Resources/monster_config.json 读取数值覆盖 ScriptableObject。
    /// 修改 JSON 后重启游戏即生效，无需重新编译。
    /// 
    /// JSON 格式：
    /// {
    ///   "monsters": [
    ///     {
    ///       "monsterId": "alexscaves_tremorzilla",
    ///       "_comment": "撼地斯拉",
    ///       "_ability_comment": "激光5秒, 15tick×20=300总伤害",
    ///       "hp": 500,
    ///       "attack": 30,
    ///       "armor": 10,
    ///       "armorToughness": 0,
    ///       "moveSpeed": 48,
    ///       "attackRange": 58,
    ///       "attackInterval": 1.0,
    ///       "radius": 56,
    ///       "price": 1000,
    ///       "abilityParams": [
    ///         {"key": "beamCooldown", "value": 20},
    ///         {"key": "beamDuration", "value": 5}
    ///       ]
    ///     }
    ///   ]
    /// }
    /// </summary>
    public static class MonsterConfigLoader
    {
        private static Dictionary<string, ConfigEntry> _config;
        private static bool _loaded = false;

        static void LoadConfig()
        {
            if (_loaded) return;
            _loaded = true;
            _config = new Dictionary<string, ConfigEntry>();

            var json = Resources.Load<TextAsset>("monster_config");
            if (json == null)
            {
                Debug.LogWarning("[MonsterConfig] monster_config.json not found, using SO defaults");
                return;
            }

            var root = JsonUtility.FromJson<ConfigRoot>(json.text);
            if (root?.monsters == null) return;

            foreach (var entry in root.monsters)
            {
                if (!string.IsNullOrEmpty(entry.monsterId))
                    _config[entry.monsterId] = entry;
            }

            Debug.Log($"[MonsterConfig] Loaded {_config.Count} monster configs");
        }

        /// <summary> 将配置值覆盖到 MonsterDefSO </summary>
        public static void ApplyTo(MonsterDefSO def)
        {
            LoadConfig();
            if (_config == null || !_config.TryGetValue(def.monsterId, out var entry)) return;

            def.hp = entry.hp;
            def.attack = entry.attack;
            def.armor = entry.armor;
            def.armorToughness = entry.armorToughness;
            def.moveSpeed = entry.moveSpeed;
            def.attackRange = entry.attackRange;
            def.attackInterval = entry.attackInterval;
            def.radius = entry.radius;
            def.price = entry.price;
        }

        /// <summary> 获取技能参数。如果 JSON 中未定义则报错。 </summary>
        public static float GetAbilityParam(string monsterId, string key)
        {
            LoadConfig();
            if (_config == null || !_config.TryGetValue(monsterId, out var entry))
            {
                Debug.LogError($"[MonsterConfig] 怪物 '{monsterId}' 不在配置文件中！");
                return 0f;
            }
            if (entry.abilityParams == null)
            {
                Debug.LogError($"[MonsterConfig] 怪物 '{monsterId}' 没有技能参数定义！");
                return 0f;
            }
            foreach (var p in entry.abilityParams)
                if (p.key == key) return p.value;
            Debug.LogError($"[MonsterConfig] 怪物 '{monsterId}' 缺少参数 '{key}'！");
            return 0f;
        }

        /// <summary> 获取技能参数（整数）。如果 JSON 中未定义则报错。 </summary>
        public static int GetAbilityParamInt(string monsterId, string key)
        {
            return Mathf.RoundToInt(GetAbilityParam(monsterId, key));
        }

        [System.Serializable]
        public class ConfigRoot
        {
            public List<ConfigEntry> monsters;
        }

        [System.Serializable]
        public class ConfigEntry
        {
            public string monsterId;
            public string _comment;
            public string _ability_comment;
            public float hp;
            public float attack;
            public float armor;
            public float armorToughness;
            public float moveSpeed;
            public float attackRange;
            public float attackInterval;
            public float radius;
            public int price;
            public List<AbilityParam> abilityParams;
        }

        [System.Serializable]
        public class AbilityParam
        {
            public string key;
            public float value;
            public string _comment;
        }
    }
}
