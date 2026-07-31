<p align="center">
  <sub><a href="README.md">English</a> &middot; <a href="README.zh.md">中文</a></sub>
</p>

# Utility Master

一个可以查看/上传CS2道具的落点/投点的工具，完全Vibe-Coding。

## 关于

CS2 道具与技巧工具

### 链接

- [GitHub Issues](https://github.com/primspark/UtilityMaster/issues)
- [Bilibili](https://space.bilibili.com/558194430)
- [primspark@outlook.com](mailto:primspark@outlook.com)

### 许可证

MIT 许可证 - 开源

### 特别鸣谢

- 地图图标、小地图图片来自 [MurkyYT/cs2-map-icons](https://github.com/MurkyYT/cs2-map-icons)
- 道具图片来自 [Liquipedia](https://liquipedia.net/)

### 声明

1. 道具图标、地图图像及相关素材版权归 Valve Corporation 所有，仅用于非商业的社区用途。如需移除请联系 primspark@outlook.com。
2. 此为社区制作的第三方工具，与 Valve Corporation 无任何关联、赞助或背书关系。
3. CS2 相关商标归 Valve Corporation 所有。
4. 仅供学习交流使用。

## 源码版本

此仓库只保留原始源码和资源，不包含任何编译产物。下载源码后需要自行编译：

```powershell
dotnet restore
dotnet build -c Release
```

编译完成后可以运行 `bin\Release\net8.0-windows\UtilityMaster.exe`，也可以直接使用：

```powershell
dotnet run --project UtilityMaster.csproj
```

## 测试

```powershell
dotnet test UtilityMaster.Tests\UtilityMaster.Tests.csproj -c Debug
```

测试工程只用于本地验证，不参与软件发布，发布产物中不会包含测试文件。

## Release 版本

已打包好的软件本体不会提交到源码仓库，而是单独放在 Release 中分发。Release 压缩包只包含可运行程序、运行依赖和资源文件，不包含源码、编译中间文件或开发工具文件。
Release 压缩包只保留英文、简体中文和繁体中文语言资源，其他语言资源不会打包。

## 未来更新
1. 更新大多数地图的默认道具/技巧
2. 更多功能？

---

v1.5.0
