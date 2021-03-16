//#define kIsDebugFrame

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.unity.mgobe;
using com.unity.mgobe.src.Util;
using KartGame.KartSystems;
using KartGame.Timeline;
using KartGame.Track;
using KartGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Client : MonoBehaviour {
    const string MyRoomType = "Battle";

    public static Client Ins = null;
    public TextAsset MGOBEconfig;

    public bool canRefreshRoom = false; //
    public bool canRefreshPlayer = false;
    public bool isStartFrameSync = false;

    //public bool isRecvFrame = false;

    // UI
    public Canvas uiCanvas;
    public Dropdown panelsDropdown;
    public LoginPanel loginPanel;
    public RoomsPanel roomsPanel;
    public GamePanel gamePanel;
    public CreateRoomPanel createRoomPanel;
    public WaitForMatchRoomPanel waitForMatchRoomPanel;
    public RoomInPanel roomInPanel;

    // Net
    public Global global = null;

#if kIsDebugFrame
    System.Text.StringBuilder sbDebugLog;
#endif

    PlayerInfo myPlayerInfo;
    List<RoomInfo> roomList;

    List<Action> actionList = new List<Action> ();
    static object onFrameLock = new object ();

    public PlayerInfoPara playerInfoPara {
        get => new PlayerInfoPara {
            Name = myPlayerInfo.Name,
            CustomPlayerStatus = myPlayerInfo.CustomPlayerStatus,
            CustomProfile = myPlayerInfo.CustomProfile
        };
    }

    // Prefabs
    public GameObject kartOtherPrefab;

    // Game states
    public MetaGameController game;
    UInt64 lastSendFrameId = 0;
    public bool isReadyToBattle = false;
    public bool isReadyToFight = false;
    public bool isInBattle = false;

    public FighterServerPaser serverParser;

    // Start is called before the first frame update
    void Start () {
        Ins = this;

        serverParser = new FighterServerPaser();

#if kIsDebugFrame
        sbDebugLog = new System.Text.StringBuilder();
#endif

        DontDestroyOnLoad (this);
        DontDestroyOnLoad (uiCanvas);

        GenerateRandomUsername ();
        initSDK ();

        roomsPanel.UpdateButtonsInteractive (roomList);
        StartCoroutine ("RefreshRoomOrPlayer");
    }

    private void OnDestroy () {
        Global.UnInit ();
    }

    // Update is called once per frame
    void Update () {

        if (isReadyToBattle) {
            TryBeginBattle ();
        }

        if (isReadyToFight)
        {
            TryBeginFighter();
        }

        if (actionList.Count != 0) {

            lock (onFrameLock) {
                foreach (var item in actionList) {
                    if (item != null) item ();
                }
                actionList.Clear ();
            }
        }
    }

    private void AddAction (Action cb) {
        lock (onFrameLock) {
            actionList.Add (cb);
        }
    }

    void FixedUpdate () {
        // netKitMgr.Tick();
    }

    // 初始化 mgobe SDK
    public void initSDK () {
        Global.OpenId = loginPanel.myOpenId.text;
        // Global.GameId = "obg-2ll8gv12";
        // Global.SecretKey = "3b11876f7ac511c0533c46cce8ddbe5e832716ae";
        // Global.Server = "2ll8gv12.wxlagame.com";

  
        string[] mgobeConfigArray = this.MGOBEconfig.text.Split(',');
        System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(".*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        for (int i = 0; i < 3; i++)
        {
            string mgobeConfig = mgobeConfigArray[i];
            mgobeConfig = r.Replace(mgobeConfig, "");
            switch (i)
            {
                case 0:
                    Global.GameId = mgobeConfig;
                    break;
                case 1:
                    Global.SecretKey = mgobeConfig;
                    break;
                case 2:
                    Global.Server = mgobeConfig;
                    break;
                default:
                    break;
            }
        }

        GameInfoPara gameInfo = new GameInfoPara {
            GameId = Global.GameId,
            SecretKey = Global.SecretKey,
            OpenId = Global.OpenId
        };
        ConfigPara config = new ConfigPara {
            Url = Global.Server,
            ReconnectMaxTimes = 5,
            ReconnectInterval = 4000,
            ResendInterval = 2000,
            ResendTimeout = 20000,
            IsAutoRequestFrame = true,
        };

        // 初始化监听器 Listener
        Listener.Init (gameInfo, config, (ResponseEvent eve) => {
            if (eve.Code == ErrCode.EcOk) {
                AddAction (() => Task.Run (() => CloudBaseClient.Init (Global.OpenId, Global.GameId)));
                Global.Room = new Room (null);
                Listener.Add (Global.Room);
                canRefreshRoom = true;
                RefreshRoomList ();
                loginPanel.loginBtn.interactable = true;
            }
            // 初始化广播回调事件
            this.initBroadcast ();

        });
    }

    void initBroadcast () {
        // 设置收帧广播回调函数
        Global.Room.OnRecvFrame = eve => {
            RecvFrameBst bst = (RecvFrameBst) eve.Data;
            AddAction (() => this.OnFrame (bst.Frame));
        };

        // 设置消息接收广播回调函数
        Global.Room.OnRecvFromClient = eve => {
            RecvFromClientBst bst = (RecvFromClientBst) eve.Data;
        };

        // 设置服务器接收广播回调函数
        Global.Room.OnRecvFromGameSvr = eve => {
            RecvFromGameSvrBst bst = (RecvFromGameSvrBst) eve.Data;
        };

        // 设置房间改变广播回调函数
        Global.Room.OnChangeRoom = eve => {
            RefreshRoomList ();
        };

        // 设置匹配成功广播回调函数
        Room.OnMatch = eve => {
            RefreshRoomList ();
            Debugger.Log ("on match!");
        };

        // 设置取消匹配广播回调函数
        Room.OnCancelMatch = eve => {
            RefreshRoomList ();
            Debugger.Log ("on cancel match! ");
        };
    }

    IEnumerator LoadKartScene () {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync (2);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone || !isStartFrameSync) {
            yield return null;
        }
    }

    IEnumerator RefreshRoomOrPlayer () {
        while (true) {
            yield return null;
            if (canRefreshRoom) {
                RefreshRoomList ();
                yield return new WaitForSeconds (0.5f);
            } else if (canRefreshPlayer) {
                GetRoomPlayerInfo ();
                yield return new WaitForSeconds (0.5f);
            }
        }
    }

    public void OnChangePanel () {
        GameObject[] panelArr = new GameObject[] { loginPanel.gameObject, roomsPanel.gameObject, gamePanel.gameObject };
        for (int i = 0; i < panelArr.Length; ++i) {
            panelArr[i].SetActive (panelsDropdown.value == i);
        }
    }

    public static string CreateUserName () {
        string userName = string.Format ("user-{0}", Application.platform);
        return userName;
    }

    public void GenerateRandomUsername () {
       // loginPanel.myOpenId.text = string.Format ("Lz#{0}", UnityEngine.Random.Range (1, 1000));
       loginPanel.myOpenId.text = Client.CreateUserName();
    }
 
    public void Login () {
        loginPanel.loginBtn.interactable = false;

        string playerId = GamePlayerInfo.GetInfo ().Id;
        loginPanel.myPlayerId.text = playerId;

        myPlayerInfo = new PlayerInfo ();
        myPlayerInfo.Id = playerId;
        myPlayerInfo.Name = loginPanel.myOpenId.text; // use openId as player name right now
        myPlayerInfo.TeamId = "0";
        myPlayerInfo.CustomPlayerStatus = 0;
        myPlayerInfo.CustomProfile = "";
        // myPlayerInfo.MatchAttributes = new MatchAttribute();
        panelsDropdown.value = 1;

        loginPanel.loginBtn.interactable = true;
    }

    public void CreateRoomPanel () {
        createRoomPanel.canCreate = true;
        createRoomPanel.gameObject.SetActive (true);
    }

    public void HideRoomPanel () {
        createRoomPanel.canCreate = false;
        createRoomPanel.gameObject.SetActive (false);
    }
    public void CreateRoom () {
        CreateTeamRoomPara para = new CreateTeamRoomPara {
            RoomName = createRoomPanel.getName (),
            MaxPlayers = createRoomPanel.getMaxPlayerNumber (),
            RoomType = MyRoomType,
            CustomProperties = "0",
            IsPrivate = false,
            PlayerInfo = this.playerInfoPara,
            TeamNumber = createRoomPanel.getMaxPlayerNumber ()
        };

        // 创建团队房间
        Global.Room.CreateTeamRoom (para, eve => {
            if (eve.Code == 0) {
                RefreshRoomList ();
                AddAction (() => { onRoomInPanel (); });
            } else {
                Debugger.Log ("create Team Room Fail: {0}", eve.Code);
                AddAction (() => { HideRoomPanel (); });

            }

        });
    }

    public void onRoomInPanel () {
        waitForMatchRoomPanel.canLoad = false;
        waitForMatchRoomPanel.canDisplayFailPanel = true;
        waitForMatchRoomPanel.isActive = false;
        createRoomPanel.canCreate = false;
        canRefreshRoom = false;
        canRefreshPlayer = true;
        roomInPanel.gameObject.SetActive (true);
        createRoomPanel.gameObject.SetActive (false);
        roomsPanel.gameObject.SetActive (false);
    }

    // 更新房间列表
    public void RefreshRoomList () {
        GetRoomListPara para = new GetRoomListPara {
            PageNo = 1,
            PageSize = 20,
            RoomType = "Battle"
        };

        // 获取房间列表
        Room.GetRoomList (para, (eve) => {
            if (eve.Code == ErrCode.EcOk) {
                try {
                    var rsp = (GetRoomListRsp) eve.Data;
                    var rlist = new List<RoomInfo> ();
                    foreach (var item in rsp.RoomList) {
                        rlist.Add (new RoomInfo (item));
                    }
                    roomList = rlist;
                    AddAction (() => roomsPanel.UpdateRoomListBtns (roomList));
                } catch (System.Exception e) {

                    Debug.LogError (e);
                }

            } else {
                // debugger.Log ("Get room list error: {0}", eve.code);
            }
        });
    }

    // 加入房间
    public void JoinRoom () {
        canRefreshRoom = false;
        canRefreshPlayer = true;
        int roomIdx = roomsPanel.GetSelectionIndex (roomList);
        if (roomIdx != -1) {
            RoomInfo roomInfo = roomList[roomIdx];
            var maxPlayer = Convert.ToInt32 (roomInfo.MaxPlayers);
            if (maxPlayer == roomInfo.PlayerList.Count) {
                return;
            }
            var teams = new HashSet<string> ();
            foreach (var item in roomInfo.PlayerList) {
                Debug.Log("item.TeamId:"+ item.TeamId);
                teams.Add (item.TeamId);
            }
            var teamId = 0;
            for (int i = 0; i < maxPlayer; i++) {
                if (!teams.Contains (i + "")) {
                    teamId = i;
                    break;
                }
            }

            // 初始化房间
            Global.Room.InitRoom (roomInfo);

            JoinTeamRoomPara para = new JoinTeamRoomPara {
                PlayerInfo = this.playerInfoPara,
                TeamId = teamId + "",
            };

            // 加入团队房间
            Global.Room.JoinTeamRoom (para, (eve) => {
                Debug.Log("JoinTeamRoom result:"+eve.Code);
                if (eve.Code == ErrCode.EcOk) {
                    AddAction (() => { onRoomInPanel (); });
                }
                RefreshRoomList ();
            });
        }
    }

    public void LeaveRoom () {
        // 离开房间
        Global.Room.LeaveRoom (eve => {
            RefreshRoomList ();
            isReadyToBattle = false;
        });
        roomInPanel.gameObject.SetActive (false);
        canRefreshPlayer = false;
        canRefreshRoom = true;
        roomsPanel.gameObject.SetActive (true);
        waitForMatchRoomPanel.isActive = true;
        roomInPanel.setReadyBtn.interactable = true;
        roomInPanel.setReadyBtn.gameObject.SetActive (true);
        roomInPanel.cancelReadyBtn.gameObject.SetActive (false);
    }

    public void MatchPlayer () {
        MatchPlayersPara para = new MatchPlayersPara {
            MatchCode = "match-XXXXXXXXXXXX",
            PlayerInfoPara = new MatchPlayerInfoPara {
            Name = myPlayerInfo.Name,
            CustomPlayerStatus = (ulong) myPlayerInfo.CustomPlayerStatus,
            CustomProfile = myPlayerInfo.CustomProfile,
            MatchAttributes = new List<MatchAttribute> ()
            }
        };
        para.PlayerInfoPara.MatchAttributes.Add (new MatchAttribute {
            Name = "Score",
                Value = 0
        });

        // 进行玩家匹配
        Global.Room.MatchPlayers (para, eve => {
            if (eve.Code == 0) {
                RefreshRoomList ();
            } else { }
        });
    }

    public void MatchRoom () {
        CreateWaitForMatchRoomPanel ();
        MatchRoomPara para = new MatchRoomPara {
            RoomType = "Battle",
            MaxPlayers = 2,
            PlayerInfo = this.playerInfoPara
        };
        // 进行房间匹配
        Global.Room.MatchRoom (para, eve => {
            if (eve.Code != 0) {
                RefreshRoomList ();
                AddAction (() => { onRoomInPanel (); });
            } else {
                RefreshRoomList ();
                AddAction (() => { onRoomInPanel (); });
            }
        });
    }

    public void MatchGroup () {
        var playerInfo = new MatchGroupPlayerInfoPara {
            Id = myPlayerInfo.Id,
            Name = myPlayerInfo.Name,
            CustomPlayerStatus = (ulong) myPlayerInfo.CustomPlayerStatus,
            CustomProfile = myPlayerInfo.CustomProfile,
            MatchAttributes = new List<MatchAttribute> ()
        };

        playerInfo.MatchAttributes.Add (new MatchAttribute { Name = "skill", Value = 9 });
        var para = new MatchGroupPara {
            MatchCode = "match-evtp3fdv",
            // matchCode = "match-hel6rt0j",
            PlayerInfoList = new List<MatchGroupPlayerInfoPara> ()
        };
        para.PlayerInfoList.Add (playerInfo);
        // 进行组队匹配
        Global.Room.MatchGroup (para, eve => {
            if (eve.Code != 0) {
                Debugger.Log ("发起匹配失败");
                return;
            }
            Debugger.Log ("发起匹配成功 {0}", eve.Code);
        });
    }

    public void cancelPlayerMatch () {
        var para = new CancelPlayerMatchPara {
            MatchType = MatchType.PlayerComplex
        };
        // 取消匹配
        Global.Room.CancelPlayerMatch (para, eve => {
            Debugger.Log ("取消比赛: {0}", eve.Code);
        });
    }

    public void CreateWaitForMatchRoomPanel () {
        waitForMatchRoomPanel.canLoad = true;
        waitForMatchRoomPanel.canDisplayFailPanel = false;
        waitForMatchRoomPanel.gameObject.SetActive (true);
    }

    public void StopMatchRoom () {
        waitForMatchRoomPanel.canLoad = false;
        waitForMatchRoomPanel.canDisplayFailPanel = false;
        waitForMatchRoomPanel.isActive = false;
    }

    void GetRoomPlayerInfo () {
        roomInPanel.ShowRoomInfo (Global.Room.RoomInfo);
    }

    public void SetReadyToBattle () {
        roomInPanel.setReadyBtn.interactable = false;
        int flag = isReadyToBattle ? 0 : 1;
        // 更改自定义玩家状态
        Global.Room.ChangeCustomPlayerStatus (new ChangeCustomPlayerStatusPara { CustomPlayerStatus = (ulong) flag },
            eve => {
                if (eve.Code == ErrCode.EcOk) {
                    isReadyToBattle = !isReadyToBattle;
                    //    RefreshRoomList();
                    AddAction (() => {
                        if (isReadyToBattle) {
                            roomInPanel.cancelReadyBtn.interactable = true;
                            roomInPanel.cancelReadyBtn.gameObject.SetActive (true);
                            roomInPanel.setReadyBtn.gameObject.SetActive (false);
                        } else {
                            roomInPanel.setReadyBtn.interactable = true;
                            roomInPanel.cancelReadyBtn.gameObject.SetActive (false);
                            roomInPanel.setReadyBtn.gameObject.SetActive (true);
                        }
                    });
                } else {
                    roomsPanel.setReadyBtn.interactable = true;
                }
            });

    }

    public void SetSceneLoaded()
    {
        // 更改自定义玩家状态
        Global.Room.ChangeCustomPlayerStatus(new ChangeCustomPlayerStatusPara { CustomPlayerStatus = ECustomPlayerStatus.SceneLoaded },
            eve => {
                if (eve.Code == ErrCode.EcOk)
                {
                    isReadyToFight = true;
                    //    RefreshRoomList();
                    AddAction(() => {
                        Debug.Log("my scene loaded");
                    });
                }
                else
                {
                    Debug.LogError("my scene loaded error:" + eve.Code);
                }
            });
    }

    void TryBeginBattle () {
        if (Global.Room != null) {
            if (Global.Room.RoomInfo.PlayerList.Count != Convert.ToInt32 (Global.Room.RoomInfo.MaxPlayers)) {
                return;
            }
            foreach (PlayerInfo player in Global.Room.RoomInfo.PlayerList) {
                if (player.CommonNetworkState == NetworkState.CommonOffline)
                    return;
                if (player.CustomPlayerStatus != 1)
                    return;
            }
            LoadScene ();
            isReadyToBattle = false;

        }
    }

    void TryBeginFighter()
    {
        if (Global.Room != null)
        {
            if (Global.Room.RoomInfo.PlayerList.Count != Convert.ToInt32(Global.Room.RoomInfo.MaxPlayers))
            {
                return;
            }
            foreach (PlayerInfo player in Global.Room.RoomInfo.PlayerList)
            {
                if (player.CommonNetworkState == NetworkState.CommonOffline)
                    return;
                if (player.CustomPlayerStatus != ECustomPlayerStatus.SceneLoaded)
                    return;
            }

            isReadyToFight = false;

            Debug.Log("2)所有玩家场景都加载好了，播放3/2/1");
            game.raceCountdownTrigger.TriggerDirector();
        }
    }

    void LoadScene()
    {
        StartCoroutine(LoadKartScene());
    }

   public void StartFrameSync () {
      
        MXLockStepMsgGetter.Init();

        // 开始帧同步(房间里任意一人调用此函数，其他人OnFrame都会受到消息，所以所有人的帧是同步）
        Global.Room.StartFrameSync(eve => {
            try
            {
                if (eve.Code == ErrCode.EcOk)
                {
                    isStartFrameSync = true;
                    isInBattle = true;
                }
                else
                {
                    roomsPanel.setReadyBtn.interactable = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        });
    }

    //         void OnFrame (Frame fr) {
    // #if true
    //         if (game == null || !game.isStarted) return;

    //         //只执行一次
    //         if (!isRecvFrame && fr.Id > 10) {
    //             // Start countdown at the same time for all the clients
    //             game.raceCountdownTrigger.TriggerDirector ();
    //             isRecvFrame = true;
    //         }
    //         var acceleration = game.myKartInput?.Acceleration ?? 0;
    //         var steering = game.myKartInput?.Steering ?? 0;

    //         // if(fr.Id == 30) {  
    //         //     Global.Room.RequestFrame(new RequestFramePara {
    //         //         BeginFrameId = 20,
    //         //         EndFrameId = 30
    //         //     }, e => {
    //         //         var rsp =  (RequestFrameRsp)e.Data;
    //         //         foreach (Frame item in rsp.Frames)
    //         //         {
    //         //             Debugger.Log("RequestFrame rsp: {0} {1} {2}", item.Id, item.RoomId, item.Time);

    //         //         }
    //         //     });
    //         // }

    //         Debug.Log("fr.Id:"+ fr.Id);
    //         var para = new SendFramePara {
    //             Data =
    //             $"{acceleration},{steering},{game.myKartInfo.Position.x:0.###},{game.myKartInfo.Position.y:0.###},{game.myKartInfo.Position.z:0.###},{game.myKartInfo.Rotation.x:0.####},{game.myKartInfo.Rotation.y:0.####},{game.myKartInfo.Rotation.z:0.####},{game.myKartInfo.Rotation.w:0.####}"
    //         };
    //         // 发送帧
    //         Global.Room.SendFrame (para, eve => { });

    //         foreach (var item in fr.Items.Where (item => game.otherKarts.ContainsKey (item.PlayerId))) {
    //             game.otherKarts[item.PlayerId].OnFrame ((string) item.Data);
    //         }
    // #else
    //         if (fr.Id > lastSendFrameId + 15) {
    //             lastSendFrameId = fr.Id;
    //             netKitMgr.SendFrame (string.Format ("FakeFrameData #{0}", UnityEngine.Random.Range (1, 1000)));
    //         }

    //         if (fr.Items.Count > 0) {
    //             string log = "";
    //             log += string.Format ("[Broadcast] FrameId({0}), ItemCount({1})\n", fr.Id, fr.Items.Count);
    //             for (int i = 0; i < fr.Items.Count; ++i) {
    //                 var item = fr.Items[i];
    //                 log += string.Format ("    #{0}: {1}/{2}/{3}\n", i, item.PlayerId, item.Timestamp, item.Data);
    //             }
    //             gamePanel.LogMsg (log);
    //         }
    // #endif
    //     }


    float timeLast = 0;
    FighterServerData serverdata;
    void OnFrame (Frame fr) {

        if (game == null || !game.isStarted) return;

        // wait for interleave frame count (equal to 2 currently).If otherelse receive frame event less then 2 frame，still having to wait 
        if (fr.Id == (MXLockStepMsgGetter.cacheFrame + MXLockStepMsgGetter.stepFrameCount + MXLockStepMsgGetter.interleaveFrameCount))
        {
            MXLockStepMsgGetter.GetNetMsg();
            MXLockStepMsgGetter.cacheFrame += MXLockStepMsgGetter.stepFrameCount;
        }

        // 发送帧
        if(serverdata == null)
            serverdata = new FighterServerData();
        serverdata.deltaMove = (DeltaMove) (game.myFighterInput?.deltaMove ?? 0);
        serverdata.isPlayHitAction = game.myFighterInput.IsPlayHitAction;
        serverdata.frameId = fr.Id;
        SendFramePara para = serverdata.Serialize();
        Global.Room.SendFrame (para, eve => { });

#if kIsDebugFrame
        sbDebugLog.Clear();
       
        sbDebugLog.AppendFormat("deltaTime:{0}, frameid:{1}", Time.time - timeLast, fr.Id);
        timeLast = Time.time;
#endif
        foreach (var item in fr.Items) {

            if (game.otherFighters.ContainsKey(item.PlayerId)) {
                serverdata = serverParser.OnFrameOther(item.Data);
#if kIsDebugFrame
                sbDebugLog.AppendFormat("|other:{0}", serverdata.ToString());
#endif
            }else {
                serverdata = serverParser.OnFrameMySelf(item.Data);
#if kIsDebugFrame
                sbDebugLog.AppendFormat("|myself:{0}", serverdata.ToString());
#endif
            }
        }
#if kIsDebugFrame
        Debug.Log(sbDebugLog.ToString());
#endif
    }
}

public static class ECustomPlayerStatus
{
    public static ulong SceneLoaded = 3;//场景加载完成
}