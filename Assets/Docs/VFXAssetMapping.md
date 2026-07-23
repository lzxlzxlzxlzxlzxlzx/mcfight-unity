# VFX 素材映射方案

> Cartoon FX 3 Remaster + Legacy -> MC Fight Arena 特效对应表
> 日期: 2026-07-20

---

## 一、P0 — 最影响体验（大量战斗缺少视觉）

### 1. 近战攻击命中标记（所有近战单位）

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 通用近战命中 | `CFXR3 Hit Misc A` | `CFXR Prefabs/Misc/` | 小型撞击粒子，0.5s 消散 |
| 重型近战命中 | `CFXR3 Hit Misc C` | `CFXR Prefabs/Misc/` | 更大的撞击，用于伤害>=15的近战 |
| 暴击命中(>=30伤害) | `CFXR3 Hit Light C (Air)` | `CFXR Prefabs/Light/` | 闪光爆裂 |

### 2. 骸骨斩首者三连斩 + 终结

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 三连斩 Strike 1/2 | `CFXR3 Hit Misc C` | `CFXR Prefabs/Misc/` | 刀光撞击，在锥形端点播放 |
| 终结 AOE(72px) | `CFXR3 Fire Explosion A` | `CFXR Prefabs/Explosions/` | 重击冲击波 |
| 冲锋落地 | `CFXR3 Hit Misc B` | `CFXR Prefabs/Misc/` | 落地尘土 |
| 践踏锥形 | `CFXR3 Hit Misc D` | `CFXR Prefabs/Misc/` | 锥形震荡 |

### 3. 远古遗魂石碑弹幕（7环下落）

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 石碑下落光柱 | `CFXR3 Sky Rays (Loop)` | `CFXR Prefabs/Light/` | 缩短持续时间，放在每个石碑位置 |
| 石碑落地冲击 | `CFXR3 Hit Light A (Air)` | `CFXR Prefabs/Light/` | 每环落地时播放 |
| 全屏最终爆发 | `CFXR3 LightGlow C (Loop)` | `CFXR Prefabs/Light/` | 7环全部落地后 |

### 4. 渊灵术士锁定锚点 + 激光雨

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 锁定标记 | `CFXR3 Magic Aura A (Runic)` | `CFXR Prefabs/Magic Misc/` | 跟踪目标的符文光环 |
| 激光雨每tick | `CFXR3 Hit Light B (Air)` | `CFXR Prefabs/Light/` | 7次从天而降的光柱 |
| 激光雨爆发 | `CFXR ScreenDistortion Sphere` | `CFXR Prefabs/Screen Distortion/` | 地面灼烧扭曲 |

### 5. 遗弃者弧形声波

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 弧形声波(4tick) | `CFXR ScreenDistortion Ring` | `CFXR Prefabs/Screen Distortion/` | 从中心扩散的环形波纹 |
| 声波命中 | `CFXR3 Hit Misc A` | `CFXR Prefabs/Misc/` | 被击中的小粒子 |

### 6. 深潜者法师水波扇形

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 水波扩散 | `CFX3_Vortex_Ground (Blue)` | `CFX3 Prefabs/Magic Misc/Color Variants/` | 蓝色地面漩涡 |
| 水波击退 | `CFX3_Vortex_Ground_Outward` | `CFX3 Prefabs/Magic Misc/` | 向外扩散的击退波 |

### 7. 锥形喷射（寒冬狼/喷火甲虫）

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 冰雾锥形(寒冬狼) | `CFX3_Hit_Ice_B_Air` | `CFX3 Prefabs/Ice/` | 蓝色冰雾，4tick 播放 |
| 火焰锥形(喷火甲虫) | `CFX3_Hit_Fire_B_Air` | `CFX3 Prefabs/Fire/` | 橙色火焰，4tick 播放 |

---

## 二、P1 — 提升视觉质量

### 8. 投射物拖尾美化

| 投射物类型 | 源 Prefab | 路径 | 说明 |
|------------|-----------|------|------|
| 通用远程(蓝/红) | `CFXR3 Lightball A + Trail` | `CFXR Prefabs/Light/` | 轻量光球+拖尾 |
| 烈焰人火球 | `CFXR3 Fireball A + Fire Trail` | `CFXR Prefabs/Fire/` | 火球+火焰拖尾 |
| 冰球(雪怪首领) | `CFXR3 Iceball A + Ice Trail` | `CFXR Prefabs/Ice/` | 冰球+冰雾拖尾 |
| 先驱者追踪导弹 | `CFXR3 Lightball B + Trail` | `CFXR Prefabs/Light/` | 紫色光球+螺旋拖尾 |
| 徘徊者导弹 | `CFXR3 Lightball B + Trail` | `CFXR Prefabs/Light/` | 紫红色变体 |
| 炽燃遗魂骨弹 | `CFXR3 Hit Misc E Skull` | `CFXR Prefabs/Misc/` | 骷髅形投射+白色拖尾 |
| 遗弃者声波弹 | `CFXR3 LightGlow A (Loop)` | `CFXR Prefabs/Light/` | 青色光团(穿透不消) |
| 深潜者法师水弹 | `CFX3_IceBall_A (Blue)` | `CFX3 Prefabs/Ice/Color Variants/` | 蓝色水球 |
| 唤魔者尖牙 | `CFX3_Hit_Misc_B (Yellow)` | `CFX3 Prefabs/Misc/Color Variants/` | 黄色地面裂开 |

### 9. 光束附加粒子（撼地斯拉/先驱者/徘徊者）

| 激光 | 附加粒子 | 路径 | 说明 |
|------|----------|------|------|
| 撼地斯拉(橙) | `CFXR3 Flying Ember` | `CFXR Prefabs/Fire/` | 沿光束散落火花 |
| 先驱者死亡射线(红) | `CFXR3 LightGlow B (Loop, Red)` | `CFXR Prefabs/Light/Variants/` | 沿光束的红色光晕 |
| 徘徊者射线(紫) | `CFXR3 LightGlow A (Loop, Blue)` | `CFXR Prefabs/Light/Variants/` | 改为紫色，沿光束光晕 |

### 10. AOE 冲击波美化

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 通用冲击波 | `CFXR3 Hit Misc B` | `CFXR Prefabs/Misc/` | 替代当前的纯色圆 |
| 大型冲击波 | `CFXR ScreenDistortion Ring` | `CFXR Prefabs/Screen Distortion/` | 带屏幕扭曲的扩散环 |
| 撼地斯拉践踏 | `CFXR3 Hit Misc D` | `CFXR Prefabs/Misc/` | 大范围地面震荡 |

### 11. 陨石下落（暝煌龙被动）

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 陨石本体 | `CFXR3 Fireball A` | `CFXR Prefabs/Fire/` | 下落的火球 |
| 落地爆炸 | `CFXR3 Fire Explosion B` | `CFXR Prefabs/Explosions/` | 爆炸+烟雾 |
| 熔岩残留 | `CFXR3 Fire (No Smoke)` | `CFXR Prefabs/Fire/Variants/` | 持续燃烧 |

### 12. 沙暴龙卷风（远古遗魂）

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 沙暴龙卷 | `CFX3_VortexTornado` | `CFX3 Prefabs/Magic Misc/` | 旋转龙卷风 |
| 3个环绕 | 同上(缩小) | 同上 | 3个实例，半径96px环绕 |

### 13. 瓦吉特龙卷穿透

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 穿透龙卷 | `CFX3_VortexTornado (Orange)` | `CFX3 Prefabs/Magic Misc/Color Variants/` | 沙色龙卷风，线性移动 |

---

## 三、P2 — 细节增强

### 14. 状态效果粒子（单位身上持续）

| 状态 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 中毒(Poison) | `CFX3_Hit_Misc_A (Green)` | `CFX3 Prefabs/Misc/Color Variants/` | 绿色气泡，循环 |
| 燃烧(Burn) | `CFX3_Flying_Ember (Blue)` | `CFX3 Prefabs/Fire/Color Variants/` | 橙色火焰粒子 |
| 凋零(Wither) | `CFX3_DarkMagicAura_A` | `CFX3 Prefabs/Magic Dark/` | 紫色暗影光环 |
| 减速(Slow) | `CFX3_Hit_Ice_B_Air` | `CFX3 Prefabs/Ice/` | 蓝色冰晶(缩小循环) |
| 恐惧(Fear) | `CFX3_Hit_Misc_E_Skull` | `CFX3 Prefabs/Misc/` | 骷髅标记 |
| 冰冻(Freeze) | `CFX3_Ice_Shield` | `CFX3 Prefabs/Ice/` | 冰块覆盖 |
| 蛰晕(Stun) | `CFX3_Hit_Electric_A_Air (Yellow)` | `CFX3 Prefabs/Electric/Color Variants/` | 黄色闪电 |

### 15. 死亡特效

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 普通死亡 | `CFX3_SmokePuff` + `CFX3_Hit_Misc_F_Smoke` | `CFX3 Prefabs/Misc/` | 烟雾消散 |
| 核能苦力怕自爆 | `CFXR3 Fire Explosion A` | `CFXR Prefabs/Explosions/` | 大爆炸 |
| 独眼巨人吞噬 | `CFX3_Hit_Misc_B_Gravity` | `CFX3 Prefabs/Misc/` | 目标缩小下沉 |
| 尸巫转化 | `CFXR3 Resurrection Light (Circle)` | `CFXR Prefabs/Light/` | 灵魂飞出+召唤光 |

### 16. 特殊状态视觉

| 状态 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 紫水晶巨蟹埋地 | `CFX3_SmokeColumn_Green` | `CFX3 Prefabs/Environment/` | 地面冒泡 |
| 炽燃遗魂防御 | `CFX3_Shield_Rays (White)` | `CFX3 Prefabs/Light/Color Variants/` | 骨骼护盾光环 |
| 诡异蚊鬼变身 | `CFXR3 Magic Aura B (Runic)` | `CFXR Prefabs/Magic Misc/` | 变身爆发 |
| 先驱者回血 | `CFXR3 Resurrection Light (Circle, Loop)` | `CFXR Prefabs/Light/Variants/` | 绿色恢复光环 |
| 遗弃者回血 | `CFXR3 LightGlow A (Loop, Green)` | `CFXR Prefabs/Light/Variants/` | 暗色恢复粒子 |

### 17. 击退方向动画

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 通用击退 | `CFX3_Vortex_Ground_Outward` | `CFX3 Prefabs/Magic Misc/` | 向击退方向扩散 |
| 磁控机兵击退 | 同上 | 同上 | 磁力线视觉 |
| 链锤哥布林击退 | 同上 | 同上 | 旋转击退 |

### 18. 召唤生成动画

| 特效 | 源 Prefab | 路径 | 说明 |
|------|-----------|------|------|
| 唤魔者召唤恼鬼 | `CFXR3 Resurrection Light (Oval)` | `CFXR Prefabs/Light/Variants/` | 椭圆召唤光 |
| 尸巫召唤随从 | `CFXR3 Resurrection Light (Circle)` | `CFXR Prefabs/Light/` | 圆形召唤光 |

---

## 四、技术实现方案

### 4.1 VFXSpawner 工具类

创建一个 `VFXSpawner.cs` 静态工具类，负责：
- 从 `Resources/VFX/` 加载预制体
- 在指定位置实例化并自动销毁
- 支持队伍颜色 tint
- 支持缩放（匹配游戏世界尺寸）

### 4.2 预制体整理

将需要的 prefab 复制到 `Assets/Resources/VFX/` 下，按功能分类：
```
Resources/VFX/
├── Hit/          # 命中效果
├── Explosion/    # 爆炸效果
├── Projectile/   # 投射物拖尾
├── Beam/         # 光束附加
├── Area/         # 区域效果
├── Status/       # 状态粒子
├── Death/        # 死亡效果
└── Summon/       # 召唤效果
```

### 4.3 坐标系适配

Cartoon FX 预制体默认在 3D 空间中，我们的游戏是 2D 俯视角。
- ParticleSystem 的 `Simulation Space` 设为 `World`
- Z 轴固定为 0
- 缩放因子：粒子系统默认尺寸偏大，需统一缩小到 0.1-0.3 倍

### 4.4 集成方式

在 `BattleBridge.cs` 和 `BattleEffectViews.cs` 中：
- `OnDamageNumber` 事件中额外播放命中特效
- `SyncAreaEffects` 中用粒子替换纯色圆
- `SyncProjectiles` 中用拖尾预制体替换方块
- `SyncBeams` 中沿光束方向散布粒子
