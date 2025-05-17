# MyPrtSc
Enhanced PrtSc, auto saving and screenshot sources detecting

### 介绍

游玩galgame的你，是如何记录下美好瞬间的呢？

点开截图软件 > 拉框框 > 确定 ？

PrtSc > Word > 保存为图片 ？

有时候还要反复退出和进入全屏？

现在全都不需要！这个小程序会监听PrtSc，截屏保存分类一键解决。

### 基本功能

- 按下PrtSc自动保存截屏。
- 通过截屏时的焦点窗口名称识别软件，分门别类存储。
- 如果游戏名是乱码，会尝试恢复（目前仅支持`Shift-JIS -> GBK`类型的乱码）。

### 部署和使用

前往Release页面下载exe文件，点开即用。右键系统托盘图标可以退出。

**依赖** .NET 7.3

### 配置文件

第一次运行程序后会在相同目录下生成配置文件`config.json`。

截屏的结果默认保存在`D:/MyPrtSc_screenshot`。

修改配置文件后，需要**重启程序**才会生效。

配置文件`config.json`包括如下字段：

- `BaseDir: string` 保存截屏的根目录，包含截屏自不同软件的子文件夹。
- `IfAutoConvert: bool` 是否自动转换乱码。