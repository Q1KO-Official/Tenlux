# Tenlux / 执光 对外发布包说明

## 这个发布包里有什么

发布包建议至少包含：

- `Marketing/`
- `promo-site/`
- `README.md`
- `CHANGELOG.md`
- `RELEASE.md`
- `ROADMAP.md`
- `SUPPORT.md`
- `LICENSE.md`
- `PROJECT.md`

## 如何生成

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Export-LaunchPack.ps1
```

默认会在：

`dist/`

下生成：

- 一个带时间戳的文件夹
- 一个同名 zip 包

## 适合谁用

- 你自己做版本归档
- 发给合作伙伴 / 朋友 / 测试者
- 发到社交平台前先统一整理素材

## 建议搭配

- 发版前先跑 `13-launch-checklist.md`
- 素材录制按 `14-shotlist.md`
- 文案按平台从 `03` 到 `06` 里挑
