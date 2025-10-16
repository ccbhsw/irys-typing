🧠 Irys SpriteType 自动提交脚本

一个用于 自动提交 Irys SpriteType 游戏分数 的脚本，支持：

自动循环提交分数

遇到「一小时限制」后自动等待

可多账号运行

支持代理（可选）

🪜 一、准备环境

1️⃣ 安装 .NET SDK 8.0 或更高版本
https://dotnet.microsoft.com/zh-cn/download/dotnet

2️⃣ 下载本项目代码（点击右上角 Code → Download ZIP）
3️⃣ 解压到任意文件夹，比如：

C:\Users\Administrator\irys-typing-script

🧩 二、文件说明
文件名	作用
Program.cs	主程序脚本
Address.txt	存放钱包私钥或地址，每行一个
Proxy.txt	存放代理地址（可选），格式如 http://127.0.0.1:7890
Log.txt	程序运行日志（自动生成）
Irys_Spritetype.csproj	项目配置文件
⚙️ 三、运行方法

1️⃣ 打开命令提示符（CMD）
2️⃣ 输入以下命令进入项目目录：

cd C:\Users\Administrator\irys-typing-script


3️⃣ 运行程序：

dotnet run


4️⃣ 按提示输入：

需要每个账号执行的次数

模式（安全模式或极速模式）

⏰ 四、运行机制

每个账号会按照设置次数循环提交

如果出现：

Hourly submission limit reached. Try again later.


程序会自动 等待 70 分钟 再重新开始

所有日志会记录在 Log.txt 文件中

🧱 五、注意事项

建议使用 安全模式，避免被频繁封禁

不建议在短时间内用多个账号高速提交

若运行出错，可查看 Log.txt 或重启脚本

📦 六、常见问题

Q1：出现 “SSL connection could not be established” 怎么办？
👉 说明代理或网络异常，可关闭代理或换节点。

Q2：日志显示一小时限制但仍继续提交？
👉 请确认你使用的是最新版本的脚本（支持自动等待功能）。

Q3：要添加多个账号？
👉 在 Address.txt 文件里，一行一个账号即可。

🧑‍💻 作者说明

本脚本仅供学习与技术交流使用，请勿用于任何违规用途。
若喜欢此项目，欢迎 Star⭐ 或 Fork🍴 支持一下。
