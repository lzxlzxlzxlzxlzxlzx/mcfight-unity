using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 怪物数据库（运行时索引，从 Resources 加载所有 MonsterDefSO） </summary>
    public class MonsterDatabase
    {
        private Dictionary<string, MonsterDefSO> _byId = new();
        private List<MonsterDefSO> _sortedByPrice = new();

        /// <summary> 从 Resources/Monsters/ 加载所有 MonsterDefSO </summary>
        public void LoadAll()
        {
            var defs = Resources.LoadAll<MonsterDefSO>("Monsters");
            _byId.Clear();
            _sortedByPrice.Clear();

            foreach (var def in defs)
            {
                if (def.monsterId == null) continue;
                MonsterConfigLoader.ApplyTo(def);
                _byId[def.monsterId] = def;
                _sortedByPrice.Add(def);
            }

            _sortedByPrice.Sort((a, b) => b.price.CompareTo(a.price));
        }

        /// <summary> 手动注册（测试用） </summary>
        public void Register(MonsterDefSO def)
        {
            if (def.monsterId == null) return;
            _byId[def.monsterId] = def;
            _sortedByPrice.Add(def);
            _sortedByPrice.Sort((a, b) => b.price.CompareTo(a.price));
        }

        public MonsterDefSO GetById(string id)
        {
            if (id == null) return null;
            _byId.TryGetValue(id, out var def);
            return def;
        }

        public IReadOnlyList<MonsterDefSO> GetAllSortedByPrice() => _sortedByPrice;
        public IReadOnlyDictionary<string, MonsterDefSO> AllById => _byId;
        public int Count => _byId.Count;
    }
}
