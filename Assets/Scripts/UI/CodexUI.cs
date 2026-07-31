using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    public class CodexUI : MonoBehaviour
    {
        public Transform cardGridParent;
        public ScrollRect cardGridScroll;
        public GameObject cardPrefab;
        public Button backButton;

        public GameObject detailPanel;
        public Text detailText;
        public Button detailBackButton;

        [Header("Filter")]
        public MonsterFilterBar filterBar;

        private GameManager _gm;
        private List<MonsterDefSO> _allMonsters;

        void Start()
        {
            _gm = GameManager.Instance;
            if (backButton) backButton.onClick.AddListener(OnBack);
            if (detailBackButton) detailBackButton.onClick.AddListener(CloseDetail);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm == null) return;
            _allMonsters = new List<MonsterDefSO>(_gm.Database.GetAllSortedByPrice());
            if (cardGridParent == null) return;
            if (detailPanel != null) detailPanel.SetActive(false);

            if (filterBar != null)
                filterBar.Init(PopulateCards);

            PopulateCards();
        }

        public void Hide() { gameObject.SetActive(false); }

        void PopulateCards()
        {
            if (cardGridParent == null) return;

            for (int i = cardGridParent.childCount - 1; i >= 0; i--)
                Destroy(cardGridParent.GetChild(i).gameObject);

            var filtered = (filterBar != null && _allMonsters != null)
                ? filterBar.Apply(_allMonsters)
                : _allMonsters;

            if (filtered == null) return;

            foreach (var def in filtered)
            {
                if (def.price <= 0) continue;
                var card = Instantiate(cardPrefab, cardGridParent);
                card.SetActive(true);
                SetupCard(card, def);
            }
        }

        void SetupCard(GameObject card, MonsterDefSO def)
        {
            var cardView = card.GetComponent<MonsterCardView>();
            if (cardView != null)
            {
                cardView.Bind(def, MonsterCardView.Mode.Codex, _gm);
                var cardBtn = card.GetComponent<Button>();
                if (cardBtn == null) cardBtn = card.AddComponent<Button>();
                cardBtn.onClick.RemoveAllListeners();
                cardBtn.onClick.AddListener(() => ShowDetail(def));
                return;
            }

            var bg = card.GetComponent<Image>();
            if (bg != null) bg.color = MonsterCardView.GetRarityColor(def.price);

            var art = card.transform.Find("Art")?.GetComponent<Image>();
            if (art != null && def.idleSprite != null) { art.sprite = def.idleSprite; art.preserveAspect = true; }

            var nameTxt = card.transform.Find("Name/NameText")?.GetComponent<Text>();
            if (nameTxt != null) nameTxt.text = def.displayName;

            var costTxt = card.transform.Find("Cost/Value")?.GetComponent<Text>();
            if (costTxt != null) costTxt.text = def.price.ToString();

            var statsTxt = card.transform.Find("Stats")?.GetComponent<Text>();
            if (statsTxt != null)
                statsTxt.text = $"HP {def.hp:F0}  ATK {def.attack:F0}" + (def.armor > 0 ? $"  ARM {def.armor:F0}" : "");

            var btn = card.GetComponent<Button>();
            if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => ShowDetail(def)); }
        }

        void ShowDetail(MonsterDefSO def)
        {
            if (detailPanel == null || detailText == null) return;
            detailPanel.SetActive(true);

            // 隐藏搜索栏和卡片列表
            if (filterBar != null) filterBar.gameObject.SetActive(false);
            if (cardGridScroll != null) cardGridScroll.gameObject.SetActive(false);
            if (backButton != null) backButton.gameObject.SetActive(false);

            var sb = new StringBuilder();
            // 标题 - 亮白色
            sb.AppendLine($"<size=36><b><color=#FFFFFF>{def.displayName}</color></b></size>");
            sb.AppendLine();

            // 价格 + 稀有度
            string rarity = def.price >= 600 ? "传说" : def.price >= 120 ? "史诗" : def.price >= 50 ? "罕见" : "普通";
            string rarityColor = def.price >= 600 ? "#FFD700" : def.price >= 120 ? "#BB66FF" : def.price >= 50 ? "#FF8800" : "#CCCCCC";
            sb.AppendLine($"<size=22><color=#FFD700>价格 {def.price}G</color>  <color={rarityColor}>[{rarity}]</color></size>");
            sb.AppendLine();

            // 基础属性 - 青色标题
            sb.AppendLine("<size=22><b><color=#5AAAFF>基础属性</color></b></size>");
            sb.AppendLine($"<color=#CCCCCC>  生命值: {def.hp:F0}</color>");
            sb.AppendLine($"<color=#CCCCCC>  攻击力: {def.attack:F0}</color>");
            if (def.armor > 0)
                sb.AppendLine($"<color=#CCCCCC>  护甲: {def.armor:F0}" + (def.armorToughness > 0 ? $" (韧性: {def.armorToughness:F0})" : "") + "</color>");
            sb.AppendLine($"<color=#CCCCCC>  移动速度: {def.moveSpeed:F0}</color>");
            sb.AppendLine($"<color=#CCCCCC>  攻击范围: {def.attackRange:F0}</color>");
            sb.AppendLine($"<color=#CCCCCC>  攻击间隔: {def.attackInterval:F1}秒</color>");
            sb.AppendLine($"<color=#CCCCCC>  碰撞半径: {def.radius:F0}</color>");
            string moveType = def.moveType == MoveType.Fly ? "飞行" : "地面";
            string attackType = def.attackType == AttackType.Ranged ? "远程" : "近战";
            sb.AppendLine($"<color=#CCCCCC>  类型: {moveType} / {attackType}</color>");
            sb.AppendLine();

            // 标签
            if (def.tags != null && def.tags.Length > 0)
            {
                sb.AppendLine("<size=22><b><color=#88FF88>特性标签</color></b></size>");
                var tagNames = new List<string>();
                foreach (var t in def.tags)
                {
                    string cn = t switch
                    {
                        "boss" => "Boss", "giant" => "巨型", "fly" => "飞行",
                        "aoe_melee" => "范围近战", "explosive" => "自爆",
                        "guard" => "近战", "beam_skill" => "光束技能",
                        "fire_immune" => "免疫火焰", "kobo_block_ranged" => "格挡远程50%",
                        "troll_immune_ranged" => "免疫远程/光束",
                        "knockback_immune" => "免疫击退", "armor_piercing" => "穿透护甲",
                        "spider_rider" => "骑乘", "arthropod" => "节肢动物",
                        _ => t
                    };
                    tagNames.Add($"<color=#CCCCCC>[{cn}]</color>");
                }
                sb.AppendLine(string.Join(" ", tagNames));
                sb.AppendLine();
            }

            // 命中效果
            if (def.onHitEffects != null && def.onHitEffects.Length > 0)
            {
                sb.AppendLine("<size=22><b><color=#FF8844>命中效果</color></b></size>");
                var effectNames = new List<string>();
                foreach (var e in def.onHitEffects)
                {
                    string cn = e switch
                    {
                        StatusEffectType.Poison => "中毒", StatusEffectType.Burn => "燃烧",
                        StatusEffectType.Wither => "凋零", StatusEffectType.Slow => "减速",
                        StatusEffectType.Fear => "恐惧", StatusEffectType.Freeze => "冰冻",
                        StatusEffectType.Stun => "蛰晕",
                        _ => e.ToString()
                    };
                    effectNames.Add($"<color=#CCCCCC>[{cn}]</color>");
                }
                sb.AppendLine(string.Join(" ", effectNames));
                sb.AppendLine();
            }

            // 技能
            sb.AppendLine("<size=22><b><color=#BB66FF>技能</color></b></size>");
            string abilityName = string.IsNullOrEmpty(def.abilityComponentType) ? "标准攻击" : GetAbilityChineseName(def.abilityComponentType);
            sb.AppendLine($"<color=#CCCCCC>  类型: {abilityName}</color>");
            sb.AppendLine();

            // 技能详细描述
            string skillDesc = GetSkillDescription(def.monsterId);
            sb.AppendLine($"<size=16><color=#AAAAAA>{skillDesc}</color></size>");

            detailText.text = sb.ToString();
        }

        string GetAbilityChineseName(string type)
        {
            return type switch
            {
                "DualModeAbility" => "远近双模式",
                "TrollAbility" => "食人妖重击",
                "BerserkerAbility" => "狂战士旋风",
                "ElephantAbility" => "蓄力冲锋",
                "MinoshroomAbility" => "蓄力冲锋",
                "GoblinAbility" => "链锤横扫",
                "ConeBreathAbility" => "锥形喷射",
                "MagnetronAbility" => "磁力场",
                "StraddlerAbility" => "远程投射",
                "StymphalianAbility" => "双发射击",
                "WitchAbility" => "药水投掷",
                "PriestAbility" => "神圣范围",
                "TarantulaHawkAbility" => "蛰晕节肢",
                "BlazeAbility" => "三连火球",
                "FlyNagaAbility" => "剧毒盘旋",
                "VexAbility" => "飞行突袭",
                "EvokerAbility" => "尖牙召唤",
                "BrainiacAbility" => "远近切换+废料桶",
                "MurmurAbility" => "轻语灵头部",
                "SpiderRiderAbility" => "骑乘系统",
                "WardenAbility" => "监守者声波",
                "TremorsaurusAbility" => "恐吓怒吼",
                "CoralLeapAbility" => "珊瑚跃击",
                "CyclopsAbility" => "独眼巨人吞噬",
                "EnderGolemAbility" => "末影符文",
                "AmethystCrabAbility" => "紫水晶巨蟹钻地",
                "RevenantAbility" => "炽燃遗魂防御",
                "WarpedMoscoAbility" => "诡异蚊鬼变身",
                "FarseerAbility" => "瞻远者射线",
                "DeepOneMageAbility" => "深潜者水波",
                "NucleeperAbility" => "核能自爆",
                "DreadLichAbility" => "尸巫召唤",
                "WadjetAbility" => "瓦吉特龙卷石碑",
                "FrostmawAbility" => "霜冻巨兽重击",
                "AlphaYetiAbility" => "雪怪首领狂暴",
                "ProwlerAbility" => "徘徊者四技能",
                "ForsakenAbility" => "遗弃者跃击",
                "KobolediatorAbility" => "骸骨斩首者三连斩",
                "TremorzillaAbility" => "撼地斯拉超能射线",
                "LuxtructosaurusAbility" => "暝煌龙陨石践踏",
                "RemnantAbility" => "远古遗魂五技能",
                "HarbingerAbility" => "先驱者模式切换",
                "NagaAbility" => "娜迦蛇形体节",
                "WarlockAbility" => "渊灵术士激光雨",
                _ => type
            };
        }

        string GetSkillDescription(string monsterId)
        {
            return monsterId switch
            {
                // ===== Boss =====
                "alexscaves_tremorzilla" => "践踏: 92px范围, 30伤害, 1.0秒间隔\n超能射线: 5秒持续光束, 每0.33秒造成20伤害, 共15次=300总伤害\n射程500, 冷却20秒",
                "alexscaves_luxtructosaurus" => "跃击: 跳向目标, 48px范围, 12伤害, 冷却10秒\n甩尾/践踏交替: 96-112px范围, 12伤害\n陨石被动: 每3秒随机落石, 20伤害+熔岩区域\n火焰免疫",
                "cataclysm_ancient_remnant" => "5个随机技能:\n撕咬: 34+5%最大生命值伤害\n甩尾: 26+5%最大生命值, 80px范围\n沙暴: 3个环绕龙卷风, 持续15秒\n践踏: 23+3.5%最大生命值, 100px范围\n石碑弹幕: 7环方尖碑从天而降, 18+5%最大生命值, 冷却20秒\n所有技能施法3秒",
                "cataclysm_the_harbinger" => "模式切换: 每15秒在凋零导弹/激光之间切换\n4技能循环(每5秒): 追踪导弹(6发×3伤害)/手雷雨(8发×20伤害)/冲撞(11+6%最大生命值)/死亡射线(5秒, 10+5%最大生命值/秒)\n被动: 每秒回2HP, 击杀回5HP",
                "cataclysm_kobolediator" => "冲锋: 冲向目标, 18伤害, 72px范围\n三连斩: 2次14伤害斩击+18伤害终结, 72px范围\n大斩: 100px范围, 14伤害\n格挡远程50%, 无法攻击飞行单位",
                "warden" => "近战: 30伤害, 1.5秒间隔\n声波: 穿透投射物, 10伤害, 射程220, 冷却10秒\n速度78 (最快地面单位)",
                "alexscaves_tremorsaurus" => "恐吓怒吼: 对所有非Boss施加恐惧, 140px范围, 冷却10-15秒\n极速近战: 14伤害, 0.7秒间隔",
                "cataclysm_ender_golem" => "3技能随机循环:\n拳击: 10-16伤害\n猛击: 72px范围AOE\n虚空符文: 240px远程, 48px范围\n1秒定身效果",
                "alexsmobs_warped_mosco" => "地面阶段: 3技能随机(15+11伤害AOE/吸血10HP)\n变身: 血量低于25%变飞行远程\n变身后: 128速度, 7伤害远程, 180射程",
                "cataclysm_amethyst_crab" => "钻地循环:\n埋地5秒(无敌)→破土2秒(48px, 16伤害AOE)→横扫3秒(20px, 16伤害)",
                "cataclysm_ignited_revenant" => "随机技能:\n旋转: 3次脉冲, 20px, 6伤害\n声波: 4次脉冲, 64px, 4伤害\n骨弹: 直线投射, 6伤害\n防御姿态: 近战伤害×0.1, 免疫非近战",
                "cataclysm_coralssus" => "跃击: 1.6秒施法, 跳向目标, 28px范围, 11.5伤害\n珊瑚巨兽为大版本(28px/11.5伤害), 珊瑚傀儡为小版本(20px/12.5伤害)",
                "cataclysm_wadjet" => "扇形斩击/龙卷交替:\n扇形: 80px, 11伤害×2次\n龙卷: 直线穿透, 15伤害\n石碑弹幕: 5环, 18伤害, 冷却15秒",
                "iceandfire_cyclops" => "吞噬: 目标血量≤50时秒杀\n重击: 48pxAOE, 17伤害\n3秒恢复时间",
                "mowziesmobs_frostmaw" => "冰球: 投射物+冰冻, 12伤害, 220射程\n冰雾: 10秒持续, 140px, 1伤害/tick, 减速\n猛砸: 90px, 40伤害, 冷却20秒",
                "twilightforest_alpha_yeti" => "狂暴: 3次脉冲, 80px, 5伤害, 冷却10秒\n冰炸弹: 60px爆炸+冰冻区域(50px, 5秒)\n冰冻区域2DPS",
                "cataclysm_the_prowler" => "4技能循环:\n横扫: 72px, 11伤害\n旋转: 80px, 7伤害×4次\n追踪导弹: 3发, 3伤害, 250速度\n死亡射线: 400px, 5+5%最大生命值×4次",
                "alexscaves_forsaken" => "跃击: 跳向目标, 冷却10秒\n撕咬: 12伤害×2次\n锤击: 8伤害, 24px范围+冲击波\n弧形声波: 4次脉冲, 每次3伤害, 64px范围\n被动: 每秒回1HP",
                "cataclysm_coral_golem" => "跃击: 1.5秒施法, 跳向目标, 20px范围, 12.5伤害",
                "cataclysm_deepling_warlock" => "标记→延迟→激光雨:\n标记目标位置, 2秒延迟后降下激光雨\n7次命中, 每次14伤害=98总伤害\n40px范围AOE, 射程260\n冷却10秒",
                "iceandfire_dread_lich" => "远程: 12伤害(对空×2)\n召唤随从: 冷却10秒, 召唤4种亡灵(30HP/5攻击)\n击杀转化: 击杀敌方单位时转化为亡灵随从",

                // ===== 升级怪物 =====
                "alexsmobs_elephant" => "蓄力冲锋: 3秒蓄力后25伤害\n普通攻击: 15伤害\n20%概率长牙",
                "cataclysm_deepling_brute" => "远近双模式: 距离≤100近战(攻击力×1.4), >100远程\n近战0.85秒冷却, 远程1.1秒冷却",
                "cataclysm_ignited_berserker" => "挥砍/旋转交替:\n挥砍: 14伤害×2次\n旋转: 3秒, 64pxAOE, 11伤害×2次\n附带燃烧",
                "iceandfire_if_troll" => "重击: 27伤害(15秒冷却)\n普攻: 10伤害\n免疫击退, 免疫远程/光束",
                "evoker" => "召唤恼鬼: 2只, 15秒冷却\n尖牙: 近距64px/远距160px, 6伤害\n恼鬼: 14HP, 13攻击, 100速度",
                "twilightforest_minoshroom" => "蓄力冲锋: 3秒蓄力后23伤害\n普通攻击: 13伤害",
                "alexscaves_magnetron" => "磁力攻击: 伤害=2+周围敌军数量\n20px击退\n100px探测范围",
                "cataclysm_deepling_priest" => "持续范围: 3秒施法, 96px范围, 每秒8伤害\n冷却3秒",
                "alexsmobs_murmur" => "头部投射体: 从目标附近发射, 5伤害\n头部独立攻击, 受伤×0.5\n共享血量",
                "alexsmobs_straddler" => "远程投射: 3伤害, 160射程\n命中生成蝌蚪\n10px击退",
                "iceandfire_if_cockatrice" => "极速远程: 0.4秒间隔, 2伤害\n附带凋零效果",
                "twilightforest_blockchain_goblin" => "横扫: 48px范围, 8伤害\n10px击退",
                "witch" => "4种药水随机:\n伤害药水(6伤害, 48pxAOE)\n剧毒药水(中毒)\n迟缓药水(减速)\n治疗药水(回10HP, 无敌人时优先)",
                "twilightforest_winter_wolf" => "冰雾锥形: 4次×4伤害, 64px范围, 60°锥形\n附带减速",
                "alexsmobs_tarantula_hawk" => "蛰晕: 对节肢动物施加蛰晕(30秒定身)\n5伤害近战\n飞行单位",
                "iceandfire_stymphalianbird" => "双发射击: 2发×1伤害\n无视护甲\n移速90\n350速度投射物",
                "twilightforest_king_spider" => "骑乘系统: 蜘蛛近战6伤害+德鲁伊远程2伤害\n德鲁伊: 20HP, 160射程, 毒箭\n蜘蛛死亡后德鲁伊下马移动",
                "blaze" => "三连火球: 3发×5伤害, 5秒间隔\n无限射程, 随机偏移\n附带燃烧",
                "mowziesmobs_naga" => "剧毒射击/俯冲交替\n盘旋飞行: 100px半径绕目标\n4伤害毒弹, 8伤害俯冲\n3秒冷却",
                "alexscaves_deep_one_mage" => "近距离: 60pxAOE, 2伤害\n水弹: 10伤害, 220射程\n水波: 3发扇形, 4伤害, 160px, 80px击退",
                "alexscaves_brainiac" => "远近切换攻击\n废料桶: 血量低于30时投掷, 48px范围, 10伤害, 生成污染区域(30秒, 5DPS, 中毒+减速)",
                "alexscaves_nucleeper" => "10秒引信后自爆\n中心500伤害, 边缘100伤害, 200px范围\n不分敌我",
                "cataclysm_deepling" => "远近双模式: 距离≤100近战(攻击力×1.4), >100远程\n近战0.85秒冷却, 远程1.1秒冷却",
                "alexscaves_deep_one_knight" => "远近双模式: 距离≤100近战(攻击力×1.4), >100远程\n近战0.85秒冷却, 远程1.1秒冷却",

                // ===== 通用怪物(有特殊标签但无技能组件) =====
                "creeper" => "自爆: 接近目标后爆炸\n中心49伤害, 边缘递减\n爆炸范围60px",
                "alexscaves_atlatitan" => "范围近战: 92pxAOE(巨人), 8伤害\n2秒冷却",
                "cataclysm_modern_remnant" => "近战攻击: 6伤害\n附带范围效果",
                "alexsmobs_centipede_head" => "近战攻击: 8伤害\n附带中毒",
                "cataclysm_the_watcher" => "远程攻击: 4伤害\n附带燃烧",
                "alexscaves_teleto" => "远程攻击: 6伤害\n飞行单位",
                "stray" => "远程攻击: 3伤害\n附带减速",
                "wither_skeleton" => "近战攻击: 8伤害\n附带凋零",
                "twilightforest_skeleton_druid" => "远程攻击: 2伤害\n附带中毒",
                "twilightforest_fire_beetle" => "锥形喷射: 4次×4伤害, 64px范围\n附带燃烧",
                "twilightforest_slime_beetle" => "远程攻击: 8伤害\n节肢动物",
                "alexscaves_relicheirus" => "范围近战: 标准AOE攻击模式",
                _ => "标准攻击模式"
            };
        }

        void CloseDetail()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            // 恢复搜索栏和卡片列表
            if (filterBar != null) filterBar.gameObject.SetActive(true);
            if (cardGridScroll != null) cardGridScroll.gameObject.SetActive(true);
            if (backButton != null) backButton.gameObject.SetActive(true);
        }

        void OnBack() { if (_gm != null) _gm.ExitCodex(); }
    }
}
