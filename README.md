# 霓音 Neon music
我选择开源不搞奇奇怪怪的东西
这个项目也没有什么技术含金量，开发工具（Visual Studio + Microsoft.Web.WebView2 + 桥接html操作C#控制电脑本地读改写）  。
然后我在编写一套HTML JavaScript来制作音乐播放器。  

## 关于
开发者 - 小末

-主要可以改成其他内置网页 （网页打包app电脑版）
## 部署教程（C#）
- 1.Visual Studio创建一个WPF应用程序
- 2.（快捷键alt长按（T-N-N））工具-Nuget包管理器-管理解决方案的Nuget包管理器-搜索Microsoft.Web.WebView2-安装（我用的版本1.0.3405.78）
- 3.项目MainWindow.xaml.cs（主），MainWindow.xaml（界面），JsBridge.cs（桥接）
- 4.项目运行或者打包的必须主exe同目录文件有assets\Neonmusic.config（配置文件）
- （看测试包2）

## 介绍图
<img width="515" height="802" alt="屏幕截图 2026-05-02 180710" src="https://github.com/user-attachments/assets/0302a5ac-4447-4bbe-945e-44a9969207c4" />
<img width="512" height="805" alt="屏幕截图 2026-05-02 180703" src="https://github.com/user-attachments/assets/c973efb6-2dce-4fd9-b1a2-d00f4d6567ed" />
<img width="521" height="807" alt="屏幕截图 2026-05-02 180652" src="https://github.com/user-attachments/assets/5a71d81f-1857-47ef-9b25-93b56d91b439" />
