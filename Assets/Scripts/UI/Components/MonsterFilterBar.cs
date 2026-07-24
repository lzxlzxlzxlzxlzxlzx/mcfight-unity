using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary>
    /// 怪物搜索/过滤栏：支持文本搜索 + 价格排序 + 标签过滤。
    /// 商店和图鉴共用。
    /// </summary>
    public class MonsterFilterBar : MonoBehaviour
    {
        public InputField searchInput;
        public Button sortButton;
        public Text sortText;
        public Button filterButton;
        public Text filterText;

        public enum SortMode { PriceDesc, PriceAsc, Name }
        public enum FilterMode { All, Ground, Fly, Melee, Ranged }

        private SortMode _sort = SortMode.PriceDesc;
        private FilterMode _filter = FilterMode.All;
        private System.Action _onChanged;

        public void Init(System.Action onChanged)
        {
            _onChanged = onChanged;
            if (searchInput != null)
            {
                searchInput.onValueChanged.RemoveAllListeners();
                searchInput.onValueChanged.AddListener((s) => _onChanged?.Invoke());
            }
            if (sortButton != null)
            {
                sortButton.onClick.RemoveAllListeners();
                sortButton.onClick.AddListener(OnSortClick);
            }
            if (filterButton != null)
            {
                filterButton.onClick.RemoveAllListeners();
                filterButton.onClick.AddListener(OnFilterClick);
            }
            UpdateLabels();
        }

        void OnSortClick()
        {
            _sort = (SortMode)(((int)_sort + 1) % 3);
            UpdateLabels();
            _onChanged?.Invoke();
        }

        void OnFilterClick()
        {
            _filter = (FilterMode)(((int)_filter + 1) % 5);
            UpdateLabels();
            _onChanged?.Invoke();
        }

        void UpdateLabels()
        {
            if (sortText != null)
                sortText.text = _sort switch
                {
                    SortMode.PriceDesc => "价格↓",
                    SortMode.PriceAsc => "价格↑",
                    _ => "名称"
                };
            if (filterText != null)
                filterText.text = _filter switch
                {
                    FilterMode.All => "全部",
                    FilterMode.Ground => "地面",
                    FilterMode.Fly => "飞行",
                    FilterMode.Melee => "近战",
                    _ => "远程"
                };
        }

        public string GetSearchText()
        {
            return searchInput != null ? searchInput.text?.Trim() ?? "" : "";
        }

        public SortMode GetSortMode() => _sort;
        public FilterMode GetFilterMode() => _filter;

        /// <summary> 对列表进行过滤和排序 </summary>
        public List<MonsterDefSO> Apply(List<MonsterDefSO> source)
        {
            var result = new List<MonsterDefSO>();
            string search = GetSearchText().ToLower();

            foreach (var def in source)
            {
                if (def.price <= 0) continue;

                // Search filter
                if (!string.IsNullOrEmpty(search))
                {
                    bool match = (def.displayName != null && def.displayName.ToLower().Contains(search)) ||
                                 (def.monsterId != null && def.monsterId.ToLower().Contains(search));
                    if (!match) continue;
                }

                // Type filter
                switch (_filter)
                {
                    case FilterMode.Ground:
                        if (def.moveType != MoveType.Ground) continue;
                        break;
                    case FilterMode.Fly:
                        if (def.moveType != MoveType.Fly) continue;
                        break;
                    case FilterMode.Melee:
                        if (def.attackType != AttackType.Melee) continue;
                        break;
                    case FilterMode.Ranged:
                        if (def.attackType != AttackType.Ranged) continue;
                        break;
                }

                result.Add(def);
            }

            // Sort
            switch (_sort)
            {
                case SortMode.PriceDesc:
                    result.Sort((a, b) => b.price.CompareTo(a.price));
                    break;
                case SortMode.PriceAsc:
                    result.Sort((a, b) => a.price.CompareTo(b.price));
                    break;
                case SortMode.Name:
                    result.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal));
                    break;
            }

            return result;
        }
    }
}
