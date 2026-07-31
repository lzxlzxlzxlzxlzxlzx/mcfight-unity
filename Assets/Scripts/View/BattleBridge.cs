using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary>
    /// 战斗桥接器：驱动 BattleSimulator，每帧从模拟器状态同步到渲染层。
    /// 挂在场景中的一个 GameObject 上。
    /// </summary>
    public class BattleBridge : MonoBehaviour
    {
        public static BattleBridge Instance { get; private set; }

        [Header("摄像机")]
        public Camera battleCamera;

        [Header("预制体")]
        public GameObject unitPrefab;
        public GameObject projectilePrefab;

        /// <summary> 模拟器实例 </summary>
        public BattleSimulator Simulator { get; private set; }

        /// <summary> 怪物数据库 </summary>
        private MonsterDatabase _database;

        /// <summary> 单位视图缓存 </summary>
        private readonly Dictionary<int, UnitView> _unitViews = new();

        /// <summary> 累积时间（用于固定步长模拟） </summary>
        private float _accumulatedTime;

        /// <summary> 用于生成临时 SO </summary>
        private readonly List<MonsterDefSO> _tempDefs = new();

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            SetupCamera();
            // 不再自动启动测试战斗，由 GameManager 控制
        }

        /// <summary> 由 GameManager 调用：启动正式战斗 </summary>
        public void StartBattle(List<DeployedUnit> deployments, MonsterDatabase database)
        {
            // 清理上一场残留
            StopBattle();

            _database = database;
            _unitViews.Clear();

            Simulator = new BattleSimulator();
            Simulator.Initialize(deployments, _database, seed: 42);
            _accumulatedTime = 0;

            // 注册伤害事件 → 生成伤害数字
            DamageEvents.OnDamage -= OnDamageNumber;
            DamageEvents.OnDamage += OnDamageNumber;

            Debug.Log($"[BattleBridge] Battle started! Units: {Simulator.State.Units.Count}");
        }

        void OnDamageNumber(DamageEvent evt)
        {
            if (evt.Damage <= 0) return;
            SpawnDamageNumber(evt.Damage, evt.Category, evt.X, evt.Y);

            // VFX: 按伤害类型播放粒子特效
            var vfxPos = new Vector3(evt.X, evt.Y, 0);
            switch (evt.Category)
            {
                case DamageCategory.Melee:
                    VFXSpriteView.Play("hitmark", vfxPos.x, vfxPos.y, 25f, 0.4f);
                    break;
                case DamageCategory.Ranged:
                    VFXSpriteView.Play("hitmark", vfxPos.x, vfxPos.y, 20f, 0.3f);
                    break;
                case DamageCategory.Beam:
                    VFXSpriteView.Play("hitmark", vfxPos.x, vfxPos.y, 30f, 0.4f);
                    break;
                case DamageCategory.Explosion:
                    VFXSpriteView.Play("smallexplosion", vfxPos.x, vfxPos.y, 60f, 0.8f);
                    break;
                case DamageCategory.True:
                    VFXSpriteView.Play("hitmark", vfxPos.x, vfxPos.y, 28f, 0.4f);
                    break;
            }
        }

        /// <summary> 停止战斗，清理所有渲染对象 </summary>
        public void StopBattle()
        {
            // 取消注册事件
            DamageEvents.OnDamage -= OnDamageNumber;

            Simulator = null;
            _accumulatedTime = 0;

            // 清理所有单位视图
            foreach (var kv in _unitViews)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _unitViews.Clear();

            // 清理投射物视图
            foreach (var kv in _projViews)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _projViews.Clear();

            // 清理效果视图
            foreach (var kv in _effectViews)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _effectViews.Clear();

            // 清理光束视图
            foreach (var kv in _beamViews)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _beamViews.Clear();

            // 清理残留
            var dmgViews = FindObjectsByType<DamageNumberView>(FindObjectsSortMode.None);
            foreach (var dv in dmgViews)
                if (dv != null) Destroy(dv.gameObject);

            // 清理所有 VFX 粒子
            VFXSpawner.ClearAll();

            // 清理所有 VFXSpriteView 和 SlashView 特效
            var vfxViews = FindObjectsByType<VFXSpriteView>(FindObjectsSortMode.None);
            foreach (var v in vfxViews)
                if (v != null) Destroy(v.gameObject);
            var slashViews = FindObjectsByType<SlashView>(FindObjectsSortMode.None);
            foreach (var s in slashViews)
                if (s != null) Destroy(s.gameObject);
        }

        /// <summary> 战斗速度倍率（1x/2x/4x） </summary>
        public float SpeedMultiplier = 1f;

        void Update()
        {
            if (Simulator == null || Simulator.IsFinished) return;

            // 固定步长模拟：累积真实时间，每 TICK_DT 跑一次
            _accumulatedTime += Time.deltaTime * SpeedMultiplier;
            int maxSteps = SpeedMultiplier >= 4f ? 8 : SpeedMultiplier >= 2f ? 6 : 3;

            while (_accumulatedTime >= BattleConstants.TICK_DT && maxSteps > 0)
            {
                Simulator.Tick(BattleConstants.TICK_DT);
                _accumulatedTime -= BattleConstants.TICK_DT;
                maxSteps--;
                if (Simulator.IsFinished) break;
            }

            SyncViews();
        }

        void SyncViews()
        {
            var state = Simulator.State;
            var units = state.Units;

            // 同步单位
            for (int i = 0; i < units.Count; i++)
            {
                ref var u = ref units[i];
                if (u.State == UnitStateEnum.Dead)
                {
                    if (_unitViews.TryGetValue(u.Id, out var view) && view != null)
                    {
                        view.PlayDeath();
                        _unitViews.Remove(u.Id);
                    }
                    continue;
                }

                if (!_unitViews.TryGetValue(u.Id, out var unitView) || unitView == null)
                {
                    // 创建新单位视图
                    var def = _database.GetById(u.MonsterId);
                    var go = CreateUnitGameObject(u, def);
                    unitView = go.GetComponent<UnitView>();
                    if (unitView == null)
                        unitView = go.AddComponent<UnitView>();
                    unitView.Initialize(u, def);
                    _unitViews[u.Id] = unitView;
                }

                unitView.SyncFromState(ref u);
            }

            // 清理已死亡的视图
            var toRemove = new List<int>();
            foreach (var kv in _unitViews)
            {
                bool found = false;
                for (int i = 0; i < units.Count; i++)
                    if (units[i].Id == kv.Key && units[i].State != UnitStateEnum.Dead)
                    { found = true; break; }
                if (!found)
                {
                    if (kv.Value != null) Destroy(kv.Value.gameObject);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var id in toRemove)
                _unitViews.Remove(id);

            // 同步投射物
            SyncProjectiles(state);

            // 同步区域效果
            SyncAreaEffects(state);

            // 同步光束
            SyncBeams(state);

            // 同步 VFX 事件
            SyncVFXEvents(state);
        }

        // ===== 投射物渲染 =====
        private readonly Dictionary<int, ProjectileView> _projViews = new();
        void SyncProjectiles(BattleState state)
        {
            var projs = state.Projectiles;
            var alive = new HashSet<int>();

            for (int i = 0; i < projs.Count; i++)
            {
                var p = projs[i];
                alive.Add(p.Id);

                if (!_projViews.TryGetValue(p.Id, out var pv) || pv == null)
                {
                    var go = new GameObject($"Proj_{p.Id}");
                    pv = go.AddComponent<ProjectileView>();
                    pv.Init(p);
                    _projViews[p.Id] = pv;
                }
                pv.Sync(p);
            }

            // 清理消失的
            var toRemoveP = new List<int>();
            foreach (var kv in _projViews)
                if (!alive.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value.gameObject);
                    toRemoveP.Add(kv.Key);
                }
            foreach (var id in toRemoveP) _projViews.Remove(id);
        }

        // ===== VFX 事件渲染 =====
        void SyncVFXEvents(BattleState state)
        {
            if (state.VFXEvents.Count == 0) return;
            foreach (var vfx in state.VFXEvents)
                VFXSpawner.Spawn(vfx.Path, new Vector3(vfx.X, vfx.Y, 0), vfx.Scale, vfx.Lifetime);
            state.VFXEvents.Clear();
        }

        // ===== 区域效果渲染 =====
        private readonly Dictionary<int, EffectView> _effectViews = new();
        void SyncAreaEffects(BattleState state)
        {
            var effects = state.AreaEffects;
            var alive = new HashSet<int>();

            for (int i = 0; i < effects.Count; i++)
            {
                var eff = effects[i];
                alive.Add(eff.Id);

                if (!_effectViews.TryGetValue(eff.Id, out var ev) || ev == null)
                {
                    var go = new GameObject($"Effect_{eff.Id}");
                    ev = go.AddComponent<EffectView>();
                    ev.Init(eff);
                    _effectViews[eff.Id] = ev;

                    // 冲击波创建时播放冲击波环特效
                    if (eff.Type == AreaEffectType.Shockwave)
                    {
                        VFXSpriteView.Play("shockwave", eff.X, eff.Y, eff.Radius * 2f, 0.5f);
                    }
                }
                ev.Sync(eff);
            }

            var toRemoveE = new List<int>();
            foreach (var kv in _effectViews)
                if (!alive.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value.gameObject);
                    toRemoveE.Add(kv.Key);
                }
            foreach (var id in toRemoveE) _effectViews.Remove(id);
        }

        // ===== 光束渲染 =====
        private readonly Dictionary<int, BeamView> _beamViews = new();
        void SyncBeams(BattleState state)
        {
            var beams = state.ActiveBeams;
            var alive = new HashSet<int>();

            for (int i = 0; i < beams.Count; i++)
            {
                var b = beams[i];
                alive.Add(b.Id);

                if (!_beamViews.TryGetValue(b.Id, out var bv) || bv == null)
                {
                    var go = new GameObject($"Beam_{b.Id}");
                    bv = go.AddComponent<BeamView>();
                    bv.Init(b);
                    _beamViews[b.Id] = bv;
                }
                bv.Sync(b);
            }

            var toRemoveB = new List<int>();
            foreach (var kv in _beamViews)
                if (!alive.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value.gameObject);
                    toRemoveB.Add(kv.Key);
                }
            foreach (var id in toRemoveB) _beamViews.Remove(id);
        }

        /// <summary> 生成伤害数字 </summary>
        public void SpawnDamageNumber(float damage, DamageCategory category, float x, float y)
        {
            var go = new GameObject("DmgNum");
            go.transform.position = new Vector3(x, y, 0);
            var dv = go.AddComponent<DamageNumberView>();
            dv.Init(damage, category, new Vector3(x, y, 0));
            Destroy(go, 1f);
        }

        GameObject CreateUnitGameObject(UnitState u, MonsterDefSO def)
        {
            var go = new GameObject($"Unit_{u.Id}_{u.MonsterId}");
            var sr = go.AddComponent<SpriteRenderer>();
            if (def != null && def.idleSprite != null)
            {
                sr.sprite = def.idleSprite;
                sr.sortingOrder = 100;
                // 缩放贴图到目标尺寸
                float spriteH = def.idleSprite.rect.height;
                float targetSize = u.HasTag("giant") ? BattleConstants.SIZE_GIANT :
                                    u.HasTag("boss") ? BattleConstants.SIZE_BOSS :
                                    u.MoveType == MoveType.Fly ? BattleConstants.SIZE_FLY :
                                    BattleConstants.SIZE_NORMAL;
                float scale = targetSize / spriteH;
                go.transform.localScale = new Vector3(scale, scale, 1);
            }
            else
            {
                // 无精灵图：用颜色方块
                var tex = CreateColorTexture(u.Team == 0 ? Color.blue : Color.red);
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 1);
                sr.sortingOrder = 100;
            }

            // HP 条
            var hpGo = new GameObject("HPBar");
            hpGo.transform.SetParent(go.transform, false);
            hpGo.transform.localPosition = new Vector3(0, 2, 0);
            var hpSr = hpGo.AddComponent<SpriteRenderer>();
            hpSr.sortingOrder = 200;

            var unitView = go.AddComponent<UnitView>();
            unitView.hpBarRenderer = hpSr;

            return go;
        }

        Texture2D CreateColorTexture(Color color)
        {
            var tex = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        void SetupCamera()
        {
            if (battleCamera == null)
                battleCamera = Camera.main;

            if (battleCamera != null)
            {
                battleCamera.orthographic = true;
                battleCamera.clearFlags = CameraClearFlags.SolidColor;
                battleCamera.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
                battleCamera.orthographicSize = 360f;
                battleCamera.rect = new Rect(0, 0, 1, 1);
                battleCamera.transform.position = new Vector3(640f, 360f, -10);
            }
        }
    }
}
