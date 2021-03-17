# UnityLockStep
需要：Unity2019.4.9f1
- 很多人找Unity的LockStep例子工程都找不到，或者找到了也要搭建服务器很麻烦，导致很多人放弃LockStep的学习。
- 所以我利用腾讯的 游戏联机对战引擎(MGOBE 1.2.6.1) sdk给大家写了一个简陋版本的LockStep
> 此Demo是基于 https://mgobebucket.unitychina.cn/KartGame.zip 游戏联机对战引擎(MGOBE)例子修改，并非全部由本人写。
1. 把Assets/Fight/Asset/MGOBEconfig.txt中替换为你的gameId,secretKey,server。如何免费获取MGOBE的gameId,secretKey,server，请阅读https://console.cloud.tencent.com/minigamecloud
2. Build一个windows客户端自己和自己建立连接
3. 入口场景为Assets/Scenes/1.unity
4. 登录界面，随便命名一个账号点击“登录”
5. 点击“自动匹配” 建房间（如果进入房间界面顶部没东西，需要退出来等待数秒，再一次点击“自动匹配” ）
6. 进入房间后，点击“准备”
> 另一个客户端也要重复4/5/6步骤
