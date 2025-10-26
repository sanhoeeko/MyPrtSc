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
- 如果游戏名称显示为乱码，会尝试恢复（目前仅支持`Shift-JIS -> GBK`类型的乱码）。
- 通过修改配置文件`config.txt`自定义保存的图片格式。

### 部署和使用

前往Release页面下载exe文件，点开即用。右键系统托盘图标可以退出。

**依赖** `C# 7.3`, `.NET 4.7.2`, [optipng.exe](https://optipng.sourceforge.net/)[已内嵌]

### 配置文件

第一次运行程序后会在相同目录下生成配置文件`config.txt`。

截屏的结果默认保存在`D:/MyPrtSc_screenshot`。

修改配置文件后，需要**重启程序**才会生效。配置文件的各字段作用请参看配置文件中的注释。

#### v1.0.2 新增功能

- 现在支持3种图片格式：png（无损），jpg（有损，文件体积小，适合水群发贴吧），avif（有损，文件体积更小，但是需要专门看图软件打开，适合大量截图的保存）。

- 现在支持自定义热键，你的下一个PrtSc未必是PrtSc。

#### v1.0.1 新增功能

- （可选）如果游戏不是全屏模式，按下PrtSc对游戏窗口（识别到的焦点窗口）截图。

- （可选）内嵌了[optipng.exe](https://optipng.sourceforge.net/)优化输出文件大小，在不改变画质的前提下，现在保存的文件比单纯使用PrtSc要小 50% 左右。
