# Tenlux / 执光 发布产物导出

## 作用

这个流程用于导出真正给用户分发的软件产物，而不是营销素材包。

## 命令

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Export-ReleaseBundle.ps1
```

默认行为：

- `Release` 配置
- `x64` 平台
- 输出到 `dist/`

## 产物内容

- `app/`：`dotnet publish` 产物
- `README.md`
- `CHANGELOG.md`
- `RELEASE.md`
- `SUPPORT.md`
- `LICENSE.md`
- 对应 zip 包

## 适合什么时候用

- 发测试版给朋友
- 发给种子用户
- 做一次版本归档
- 准备手动分发压缩包
