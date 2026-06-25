---
version: 1.0
name: Tenlux Promo Site
description: >
  深色沉浸式产品网页，以 Linear/Raycast 的近黑画布为基底，
  融入 Stripe 的大气渐变 mesh 做品牌辉光叙事，
  用 Tenlux 品牌绿 (#0d6b5e / #7fe0cb) 作为唯一彩色强调。
  网页本身支持深浅主题切换，让浏览体验成为产品能力的直接演示。

design_inspiration:
  dark_canvas: "Linear (#010102) + Raycast (#07080a) — surface ladder + hairline elevation"
  atmospheric_gradient: "Stripe — hero mesh gradient, but scoped to brand green"
  typography: "Linear — aggressive negative tracking on display; Inter + Noto Serif SC"
  light_mode: "Vercel — stark contrast, geometric clarity"
  product_focus: "Raycast + Linear — product screenshots as the primary decoration"

colors:
  # ── Brand ──
  brand: "#0d6b5e"
  brand-glow: "#7fe0cb"
  brand-glow-soft: "#4dbfa8"
  brand-soft: "rgba(127, 224, 203, 0.12)"
  accent: "#efb18b"
  accent-soft: "rgba(239, 177, 139, 0.15)"

  # ── Dark Surface ──
  canvas: "#0a0a0c"
  surface-1: "#0f1012"
  surface-2: "#141518"
  surface-3: "#191a1e"
  surface-card: "#1c1d22"

  # ── Dark Hairline ──
  hairline: "rgba(255, 255, 255, 0.08)"
  hairline-strong: "rgba(255, 255, 255, 0.14)"

  # ── Dark Text ──
  ink: "#f0f0f2"
  body: "#b8b8be"
  body-strong: "#d4d4da"
  mute: "#78787e"
  faint: "#505058"

  # ── CTA ──
  cta-fg: "#0a0a0c"
  cta-bg: "#7fe0cb"

  # ── Light Surface (theme toggle) ──
  light-canvas: "#f8f8fa"
  light-surface-1: "#ffffff"
  light-surface-2: "#f2f2f5"
  light-hairline: "rgba(0, 0, 0, 0.08)"
  light-hairline-strong: "rgba(0, 0, 0, 0.14)"
  light-ink: "#111114"
  light-body: "#4a4a52"
  light-mute: "#8a8a92"
  light-brand: "#0d6b5e"
  light-cta-bg: "#0d6b5e"
  light-cta-fg: "#ffffff"

  # ── Hero Gradient Mesh ──
  mesh-start: "#0d6b5e"
  mesh-mid: "#1a4a42"
  mesh-end: "#0a0a0c"
  mesh-accent: "rgba(127, 224, 203, 0.08)"

  # ── Semantic ──
  success: "#4dbfa8"
  info: "#5b9cf5"

typography:
  # Display — hero headlines
  display-xl:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 80px
    fontWeight: 600
    lineHeight: 1.02
    letterSpacing: -3.2px

  display-lg:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 56px
    fontWeight: 600
    lineHeight: 1.06
    letterSpacing: -2.0px

  display-md:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 40px
    fontWeight: 600
    lineHeight: 1.10
    letterSpacing: -1.2px

  # Heading — section titles
  heading-lg:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 28px
    fontWeight: 600
    lineHeight: 1.15
    letterSpacing: -0.5px

  heading-md:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 22px
    fontWeight: 500
    lineHeight: 1.25
    letterSpacing: -0.3px

  # Body
  body-lg:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 18px
    fontWeight: 400
    lineHeight: 1.6
    letterSpacing: -0.05px

  body-md:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 16px
    fontWeight: 400
    lineHeight: 1.6
    letterSpacing: 0

  body-sm:
    fontFamily: "Inter, 'Noto Serif SC', system-ui, sans-serif"
    fontSize: 14px
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: 0

  # Caption & Label
  caption:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: 12px
    fontWeight: 500
    lineHeight: 1.4
    letterSpacing: 0.6px
    textTransform: uppercase

  eyebrow:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: 13px
    fontWeight: 500
    lineHeight: 1.3
    letterSpacing: 0.4px
    textTransform: uppercase

  # Chinese display (brand title)
  cn-display:
    fontFamily: "'Noto Serif SC', serif"
    fontSize: 96px
    fontWeight: 700
    lineHeight: 1.0
    letterSpacing: -2px

  # Button
  button:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: 15px
    fontWeight: 500
    lineHeight: 1.0
    letterSpacing: 0

  # Mono (for data/stats)
  mono:
    fontFamily: "'JetBrains Mono', 'SF Mono', ui-monospace, monospace"
    fontSize: 14px
    fontWeight: 400
    lineHeight: 1.4
    letterSpacing: 0

rounded:
  none: 0px
  xs: 4px
  sm: 6px
  md: 8px
  lg: 12px
  xl: 16px
  xxl: 24px
  pill: 9999px
  full: 9999px

spacing:
  xxs: 4px
  xs: 8px
  sm: 12px
  md: 16px
  lg: 24px
  xl: 32px
  xxl: 48px
  3xl: 64px
  section: 96px

components:
  # ── Buttons ──
  button-primary:
    backgroundColor: "{colors.cta-bg}"
    textColor: "{colors.cta-fg}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    padding: "12px 24px"
    height: 48px
    fontWeight: 600
    description: "品牌绿 CTA 按钮，深色模式下的主要行动按钮"

  button-primary-light:
    backgroundColor: "{colors.light-cta-bg}"
    textColor: "{colors.light-cta-fg}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    padding: "12px 24px"
    height: 48px
    description: "亮色模式下的品牌绿 CTA"

  button-secondary:
    backgroundColor: "{colors.surface-2}"
    textColor: "{colors.ink}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    padding: "12px 24px"
    height: 48px
    border: "1px solid {colors.hairline-strong}"
    description: "深色次要按钮"

  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.body}"
    typography: "{typography.button}"
    rounded: "{rounded.md}"
    padding: "8px 16px"
    description: "幽灵文字按钮"

  # ── Navigation ──
  top-nav:
    backgroundColor: "color-mix(in srgb, {colors.canvas} 80%, transparent)"
    backdropFilter: "blur(20px)"
    textColor: "{colors.ink}"
    typography: "{typography.body-sm}"
    height: 64px
    border: "bottom 1px solid {colors.hairline}"
    description: "毛玻璃顶栏，sticky"

  # ── Cards ──
  feature-card:
    backgroundColor: "{colors.surface-1}"
    textColor: "{colors.ink}"
    typography: "{typography.body-md}"
    rounded: "{rounded.lg}"
    padding: 24px
    border: "1px solid {colors.hairline}"
    description: "标准功能卡片"

  glass-card:
    backgroundColor: "rgba(15, 16, 18, 0.6)"
    backdropFilter: "blur(16px)"
    textColor: "{colors.ink}"
    typography: "{typography.body-md}"
    rounded: "{rounded.xl}"
    padding: 24px
    border: "1px solid {colors.hairline-strong}"
    description: "毛玻璃卡片，用于承载截图"

  screenshot-frame:
    backgroundColor: "{colors.surface-1}"
    rounded: "{rounded.xl}"
    padding: 0
    border: "1px solid {colors.hairline}"
    overflow: "hidden"
    description: "产品截图容器，截图充满无边距"

  # ── Hero ──
  hero-band:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.ink}"
    typography: "{typography.display-xl}"
    padding: "120px 32px 96px"
    description: "全屏 Hero 区域，上方有 mesh gradient 辉光"

  hero-mesh:
    description: >
      Hero 上方的大气渐变层，使用 radial-gradient 和 conic-gradient 叠加：
      主光源从品牌绿 (#0d6b5e) 向外扩散，
      经过 #1a4a42 过渡到画布色 (#0a0a0c)，
      辅以极淡的青色 (#7fe0cb @ 8% opacity) 做呼吸光晕。
      覆盖 hero 上 60% 区域，pointer-events: none。
    opacity: 0.7
    blendMode: "screen"

  # ── Desktop Demo ──
  desktop-stage:
    backgroundColor: "{colors.surface-1}"
    rounded: "{rounded.xl}"
    border: "1px solid {colors.hairline-strong}"
    overflow: "hidden"
    aspectRatio: "16 / 9"
    description: "Windows 桌面模拟舞台，含壁纸 + 任务栏 + 托盘图标"

  # ── Stats ──
  stat-card:
    backgroundColor: "{colors.surface-1}"
    rounded: "{rounded.lg}"
    border: "1px solid {colors.hairline}"
    padding: "32px 24px"
    description: "数据展示卡片（后台占用指标）"

  stat-number:
    typography: "{typography.mono}"
    fontSize: 36px
    fontWeight: 600
    textColor: "{colors.brand-glow}"
    description: "数据数字，品牌绿辉光色"

  stat-label:
    typography: "{typography.caption}"
    textColor: "{colors.mute}"
    description: "数据标签"

  # ── Section ──
  section-band:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.ink}"
    padding: "{spacing.section} {spacing.xl}"
    description: "标准内容区域"

  section-band-elevated:
    backgroundColor: "{colors.surface-1}"
    textColor: "{colors.ink}"
    padding: "{spacing.section} {spacing.xl}"
    border: "top/bottom 1px solid {colors.hairline}"
    description: "抬起的内容区域，用 surface-1 区分"

  # ── CTA Banner ──
  cta-banner:
    backgroundColor: "{colors.surface-1}"
    textColor: "{colors.ink}"
    typography: "{typography.display-md}"
    rounded: "{rounded.xxl}"
    padding: "64px 48px"
    border: "1px solid {colors.hairline}"
    textAlign: "center"
    description: "结尾 CTA 区域"

  # ── Footer ──
  footer:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.mute}"
    typography: "{typography.body-sm}"
    padding: "48px 32px"
    border: "top 1px solid {colors.hairline}"

---

## Overview

Tenlux（执光）的产品网页采用**深色沉浸式设计**，整体视觉语言融合了三个来源：

1. **Linear/Raycast 的暗色画布体系**：近黑背景 `#0a0a0c` + 四级表面阶梯（canvas → surface-1 → surface-2 → surface-3）承载层级，hairline 1px 边框做分隔，完全不用阴影
2. **Stripe 的大气渐变 mesh**：Hero 区域上方用品牌绿 `#0d6b5e` 的 radial/conic gradient 做辉光效果，像光线从屏幕中央破开黑暗
3. **Tenlux 自身的品牌色**：品牌绿 `#0d6b5e`（深）/ `#7fe0cb`（辉光）作为唯一彩色强调，暖橙 `#efb18b` 做极少量辅助点缀

网页同时支持**深浅主题切换**（通过 CSS `data-theme` 属性），深色模式是默认状态。亮色模式采用 Vercel 式高对比逻辑（纯白画布 + 深色文字 + 品牌绿 CTA）。主题切换本身就是产品核心能力"深浅模式切换"的活体演示。

**品牌气质**：安静、克制、可靠、有审美但不浮夸。网页的每一个元素都应该传达"这是一个安静地待在你托盘里、但随时准备好的工具"。

## Design Principles

1. **深色优先**：默认深色模式，亮色模式是补充而非替代
2. **产品即装饰**：Tenlux 的实际界面截图是网页的主要视觉元素，不依赖外部插图或照片
3. **光的故事**：品牌绿辉光作为贯穿全站的视觉线索，从 hero 到 CTA，像一条光的轨迹
4. **hairline 层级**：用表面色阶 + 1px hairline 建立层级感，绝不使用 drop-shadow
5. **动效克制**：滚动淡入 + 主题过渡 + hero 呼吸光晕，所有动画保持安静和缓慢
6. **中文优先**：网页主要文案为中文，字体用 Inter（拉丁）+ Noto Serif SC（中文），"执光"二字用 Noto Serif SC 大字号呈现

## Page Structure

### 1. Hero — "执光"
- 全屏深色，上方有 mesh gradient 辉光（品牌绿 → 深青 → 画布色）
- 中文大字标题"执光"，使用 `{typography.cn-display}`（96px Noto Serif SC 700）
- 副标题英文 "Tenlux" 用 `{typography.display-lg}` Inter
- 一行描述 + 两个 CTA 按钮（primary 下载 + secondary GitHub）
- hero 下方可选一个"向下滚动"的微动画指示器

### 2. Desktop Demo — 模拟桌面
- 保留现有 Windows 桌面模拟概念但升级：更大、更沉浸
- 16:9 容器 `{component.desktop-stage}`
- 点击托盘图标时整个区块做深浅切换动画（壁纸淡入淡出 + 任务栏变色）
- 配说明文字："点一下，就切过去。"

### 3. Feature Sections — 滚动叙事
- 每个核心功能用一个大 `{component.section-band}` 展示
- 布局：左文右图（或交替）
- 截图放在 `{component.glass-card}` 或 `{component.screenshot-frame}` 中
- 功能列表：控制台 / 切换选项（热键+定时+通知） / 深浅壁纸 / 常规设置
- 滚动触发淡入 + 微上移动画（CSS scroll-driven 或 IntersectionObserver）

### 4. Stats — 轻量存在
- 3-4 个数据卡片横排：Working Set 32MB / 8 线程 / 275 句柄 / < 10MB 私有内存
- 数字用 `{colors.brand-glow}` 品牌绿辉光色 + `{typography.mono}` 等宽字体
- 可选环形进度条或轻量动画

### 5. CTA Banner — 结尾
- `{component.cta-banner}` 圆角卡片
- 标题："把 Tenlux 放进你的任务栏。"
- 两个 CTA 按钮

### 6. Footer
- 简洁底部：版本信息 + GitHub + 许可证

## Animation & Motion

### Hero Mesh Glow
```css
/* 呼吸光晕：8秒周期，opacity 0.5 ↔ 0.8 */
@keyframes mesh-breathe {
  0%, 100% { opacity: 0.5; transform: scale(1); }
  50% { opacity: 0.8; transform: scale(1.02); }
}
```

### Scroll Reveal
```css
/* 滚动淡入：translateY(24px) → 0, opacity 0 → 1, 600ms */
.reveal {
  opacity: 0;
  transform: translateY(24px);
  transition: opacity 600ms ease, transform 600ms ease;
}
.reveal.visible {
  opacity: 1;
  transform: translateY(0);
}
```

### Theme Transition
```css
/* 主题切换：所有颜色属性 400ms 过渡 */
*, *::before, *::after {
  transition: background-color 400ms ease, color 300ms ease, border-color 300ms ease;
}
```

## Responsive Breakpoints

| Name | Width | Key Changes |
|---|---|---|
| Desktop-XL | 1440px+ | 完整布局 |
| Desktop | 1280px | 内容区 max-width 1120px |
| Tablet | 1024px | 左右布局 → 上下堆叠 |
| Mobile | 768px | 导航收起汉堡菜单；display-xl 缩至 40px |
| Mobile-S | 480px | 单列；cn-display 缩至 56px |

## Do's and Don'ts

### Do
- 保持整个深色模式的色调连续性——从 hero 到 footer 不中断暗色画布
- 用品牌绿辉光 `{colors.brand-glow}` 做 CTA 和数据的唯一彩色
- 用 Inter + Noto Serif SC 双字体系统
- 用 hairline + 表面阶梯建立层级
- 让产品截图做主角
- hero mesh gradient 的 opacity 保持克制（0.5-0.8），不要过亮

### Don't
- 不要引入第二个彩色品牌色（品牌绿是唯一的）
- 不要在深色画布上使用 drop-shadow
- 不要让 hero 辉光动画太快或太亮（8 秒周期，缓慢呼吸）
- 不要用 pill 圆角做卡片（pill 只用于按钮和标签）
- 不要让亮色模式成为默认——深色是 Tenlux 网页的主场
- 不要在功能区域使用大面积渐变（渐变只属于 hero）

## Font Loading

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Noto+Serif+SC:wght@600;700;800&display=swap" rel="stylesheet">
```

## Reference Sources

本设计综合了以下品牌的设计规范（来自 awesome-design-md）：

- **Linear** — 深色画布色阶、负字间距排版、产品截图主导
- **Raycast** — hairline 层级体系、表面阶梯、Inter + ss03 字体设置
- **Stripe** — hero 大气渐变 mesh、thin weight 优雅感
- **Vercel** — 亮色模式的高对比逻辑、几何排版、pill 按钮
- **Warp** — 克制动效、quiet confidence 的品牌语气
