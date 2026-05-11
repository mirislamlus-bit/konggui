# 《缄灯：归渡》第一章“归镇”MVP

打开 `Assets/Scenes/SampleScene.unity` 后直接 Play 即可。若场景内没有手工摆放的 MVP，`ChapterOneRuntimeBootstrap` 会在运行时自动生成五个场景、玩家、UI、交互点和第一章谜题。

## 操作

- A/D 或 左右方向键：移动
- E：互动
- Q：切换灯影视角
- Tab：打开/关闭背包
- Esc：关闭第一章结尾面板

## 第一章流程

镇口 -> 石桥 -> 外婆家 -> 灵堂 -> 老井

## 谜题流程

1. 在灵堂依次与“米、酒、香”互动，顺序错误会重置。
2. 顺序正确后获得黑灯。
3. 与灵堂黑灯互动点燃。
4. 前往老井，按 Q 开启灯影视角，再与老井互动触发第一章结尾。

## 美术替换

当前工程的 `Assets` 目录没有检测到正式 PNG/PSD 素材，因此 MVP 使用运行时生成的低饱和占位 Sprite。后续可按对象名替换：

- `Player_LinZhaoying`：主角林照萤
- `TownGate_Arch`：镇口牌坊
- `Bridge` / `River`：石桥与河水
- `House`：外婆家
- `Altar` / `BlackLamp_Body` / `BlackLamp_Flame`：灵堂供桌与黑灯
- `OldWell`：老井

也可以在 Unity 菜单执行 `JianDeng/Build Chapter 1 MVP Scene`，生成可编辑场景 `Assets/Scenes/Chapter1_GuiZhen_MVP.unity` 和 `Assets/Art/Placeholders` 占位图资产。
