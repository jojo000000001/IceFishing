# 冰上钓鱼

| 项 | 内容 |
| --- | --- |
| 版本 | v1.6 |
| 工程 | IceFishing / Unity 2022.3.62f3c1 |
| 作业引擎 | 2022.3 LTS（要求写的是 52f1，提交前用指定版本打开保存即可） |
| 画面 | 竖屏 9:18，1080×2160 |

复刻《无尽冬日》冰钓小游戏，不做整款 SLG。

## 怎么改

- **规则**（饵料、碰撞、渔具数值、做不做宝藏模式）以本文「已确认规则」为准。改规则只改这一节。
- **界面、预制体、镜头、手感**以 Play 里看到的为准，直接改代码和预制体，不必先改文档。
- 不再「先改文档再改代码」。

## 现在做到哪

营地 ↔ 钓鱼能跑通：点开始后镜头沉进冰洞，钩子停在屏幕中上部，世界往下卷，拖拽左右移动；保护用完或到钓绳最大深度后上浮，到 0 m 回营地。水里按深度带刷 16 种鱼，只水平游；下潜撞头扣保护，上浮撞头捕获，承重满返程。暂停可回营地。图鉴、升级、存档、结算界面还没有。

预制体：

| 资源 | 路径 |
| --- | --- |
| 营地 UI | `Assets/Prefabs/UI/HubView.prefab` |
| 钓鱼 HUD | `Assets/Prefabs/UI/FishingHudView.prefab` |
| 水下 | `Assets/Prefabs/World/UnderwaterField.prefab` |
| 鱼 | `Assets/Prefabs/World/Fish/`（16 个） |
| 鱼种 SO | `Assets/Data/Fish/` |

菜单重生成：`IceFishing / Rebuild Hub Prefab`、`Rebuild Fishing HUD Prefab`、`Rebuild Underwater Prefab`、`Rebuild Fish Prefabs And Definitions`。整场景重铺：`Rebuild M1 Scene`。

## 范围

做：普通冰钓、营地冰洞入口、渔具三件套、图鉴、积分、暂停撤退。

不做：主城、充值、排行榜、宝藏冰钓入口、150 种鱼、一键代钓。

## 已确认规则

| 项 | 结论 |
| --- | --- |
| 饵料 | 上限 10，开局 8；营地每 15 秒 +1；撤退不扣；设置可开无限饵 |
| 冰洞 | 营地画面上的洞，钩子从这里下去 |
| 宝藏模式 | 首版不做 |
| 碰撞 | 只算鱼头。下潜撞头扣保护；上浮撞头捕获；身体/尾穿过 |
| 上浮 | 保护用尽，或到达钓绳最大深度；拉回到 0 m 后返回营地（结算未做，先直接回） |
| 保护次数 | 选 World，Inspector 里改 `hookProtectionHits`，默认 2 |
| 钓绳长度 | 选 World，Inspector 里改 `lineLengthMeters`（米），默认 180 |
| 鱼密度 | 选 World，Inspector 里改 `fishEveryMeters`（至少每隔多少米一条）和 `fishMaxAlive` |
| 撤退 | 本局作废，饵料和道具回到开局前 |
| 渔具 Lv.1→10 | 线 180→550 m；钩 5→20 条；坠 0→165 m |
| 计分 | 1～5 星：10 / 20 / 40 / 70 / 150 |
| 鱼类 | 16 种；只水平游；深度带全部在 100 m 以内；沿深度至少每 3 m 一条（World 上 `fishEveryMeters` / `fishMaxAlive`） |

手感：下潜、镜头卷动在 World 的 Inspector 里改 `descentMetersPerSecond`（默认 8 m/s）和 `worldUnitsPerMeter`（默认 0.42）。上浮 10 m/s，钩子水平 14，钩子屏幕 Y = 0.55。

## 一局流程

```text
营地 → 点开始（镜头下潜）→ 下潜躲鱼头 → 上浮钩鱼头 → 承重满或回洞 → 营地
                              ↑____________暂停撤退回营地___________|
```

阶段互斥：`Ready / Descending / Ascending / Returning / Settle / Paused`。同一套鱼头碰撞由当前阶段决定语义。

## 架构

MVC：Model 不算场景，View 不算规则，Controller 不算绘制。

| 层 | 命名空间 | 现在有什么 |
| --- | --- | --- |
| Model | `IceFishing.Model` | `PlayerProfile`、`CastSession`、`GearTables`、`BaitRegen`、`FishDefinition`、`FishCatalog` |
| View | `IceFishing.View` | `HubView`、`FishingHudView`、`WorldView`、`UnderwaterField`、`FishField`、弹窗 |
| Controller | `IceFishing.Controller` | `AppController`、`HubController`、`FishingController` |
| Core | `IceFishing.Core` | `CastPhase`、`AppScreen`、`GameEventBus`、`ServiceLocator` |

作业还要用到、尚未落地的模式：图鉴、升级、存档。输入已按拖拽抽象，重力感应后做。

单场景 `Assets/Scenes/IceFishing.unity`。

## 还没做

1. 积分、升级、图鉴、存档
2. 稳定器 / 灯 / 感应器
3. 光晕、暗角、音效、结算界面
4. APK

鱼类 16 种，按深度带刷，不做四季。贴图在 `Assets/Art/Sprites/Fish/`。
