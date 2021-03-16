using System;
using System.Collections.Generic;
using KartGame.KartSystems;
using KartGame.Timeline;
using com.unity.mgobe;
using com.unity.mgobe.src.Util;
using UnityEngine;

namespace KartGame.UI {
    //public class OtherKart {
    //    public GameObject go;

    //    /**
    //     go.GetComponent<KartMovement> ().input = input;
    //     go.GetComponent<KartAnimation> ().input = input;
    //    */
    //    public NetworkInput input;

    //    KartMovement movement;
    //    Vector3 pos;
    //    Quaternion rot;

    //    public OtherKart (GameObject go, NetworkInput input) {
    //        this.go = go;
    //        this.input = input;
    //        movement = go.GetComponent<KartMovement> ();
    //        movement.IsOtherKart = true;

    //        pos = new Vector3 ();
    //        rot = new Quaternion ();
    //    }

    //    public void OnFrame (string str) {
    //        string[] vals = str.Split (',');
    //        if (vals.Length == 9) {
    //            int intAcc;
    //            int intSteer;
    //            float px, py, pz;
    //            float rx, ry, rz, rw;
    //            if (Int32.TryParse (vals[0], out intAcc) &&
    //                Int32.TryParse (vals[1], out intSteer) &&
    //                float.TryParse (vals[2], out px) &&
    //                float.TryParse (vals[3], out py) &&
    //                float.TryParse (vals[4], out pz) &&
    //                float.TryParse (vals[5], out rx) &&
    //                float.TryParse (vals[6], out ry) &&
    //                float.TryParse (vals[7], out rz) &&
    //                float.TryParse (vals[8], out rw)) {
    //                input.m_Acceleration = intAcc;
    //                input.m_Steering = intSteer;
    //                pos.x = px;
    //                pos.y = py;
    //                pos.z = pz;
    //                rot.x = rx;
    //                rot.y = ry;
    //                rot.z = rz;
    //                rot.w = rw;
    //                rot.Normalize ();

    //                movement.syncPosition = pos;
    //                movement.syncRotation = rot;
    //            }
    //        }
    //    }
    //}

    /// <summary>
    /// The MetaGameController is responsible for switching control between the high level
    /// contexts of the application, eg the Main Menu and Gameplay systems.
    /// </summary>
    public class MetaGameController : MonoBehaviour {
        [Tooltip ("A reference to the main menu.")]
        public MainUIController mainMenu;
        [Tooltip ("A reference to the race countdown director trigger.")]
        public DirectorTrigger raceCountdownTrigger;
        [Tooltip ("The UI canvases used for game play.")]
        public Canvas[] gamePlayCanvas;

        public GameObject myFighterGameObject;
        public Joystick joystick;

        public IInputFighter myFighterInput;
        public FighterController myFighter;
        public Dictionary<string, FighterController> otherFighters;
        public FighterController otherFighter;
        
        public bool isStarted = false;

        bool m_ShowMainCanvas = true;
        bool m_FirstTime = true;

        public static MetaGameController Ins;
        void Awake()
        {
            Ins = this;
        }

        void OnDestory()
        {
            Ins = null;
        }


        void OnEnable () {
            _ToggleMainMenu (m_ShowMainCanvas);
        }
        void Start () {
            if (Client.Ins != null) {
                StartGame_Network ();
            } else {
                InitMyKart ();
            }

            //Start the game immediately rather than show the pause menu.
            HandleMenuButton ();
        }

        void StartGame_Network () {
            Client.Ins.game = this;
            Client.Ins.uiCanvas.gameObject.SetActive (false);

            InitKarts_Network ();
            //isStarted = true;
        }

        void InitKarts_Network () {
            otherFighters = new Dictionary<string, FighterController>();
            var myKartIdx = 0;
            for (int idx = 0; idx < Global.Room.RoomInfo.PlayerList.Count; ++idx) {
                var playerInfo = Global.Room.RoomInfo.PlayerList[idx];
                if (playerInfo.Id.Equals(Player.Id)) {
                    myKartIdx = idx;
                } else {
                    // other karts
                    var go = UnityEngine.Object.Instantiate (myFighterGameObject, GetKartStartPos_Network (idx), Quaternion.identity);
                    if (!go.activeSelf) {
                        go.SetActive (true);
                    }
                    otherFighter = go.GetComponent<FighterController>();
                    otherFighter.fighterLogic = go.AddComponent<FighterLogic>();
                    otherFighters[playerInfo.Id] = otherFighter;
                }
            }
            // my kart
            myFighterGameObject.transform.position = GetKartStartPos_Network (myKartIdx);
            InitMyKart ();
        }

        void InitMyKart () {
            var jsInput = myFighterGameObject.AddComponent<JoystickInput> ();
            myFighterInput = jsInput;
            jsInput.joystick = joystick;

            myFighter = myFighterGameObject.GetComponent<FighterController>();
            myFighter.fighterLogic = myFighterGameObject.AddComponent<FighterLogic>();
            myFighter.input = jsInput;
        }

        Vector3 GetKartStartPos_Network (int idx) {
            return new Vector3 (14f + idx * 3, 0.8f, 0);
        }

        /// <summary>
        /// Turns the main menu on or off.
        /// </summary>
        public void ToggleMainMenu (bool show) {
            if (m_ShowMainCanvas != show) {
                _ToggleMainMenu (show);
            }
        }

        void _ToggleMainMenu (bool show) {
            if (show) {
                // WORKAROUND: This is due to a problem where setting the time scale to 0 causes audio to stop playing.
                Time.timeScale = 0.00001f;
                mainMenu.gameObject.SetActive (true);
                foreach (var i in gamePlayCanvas) i.gameObject.SetActive (false);
            } else {
                Time.timeScale = 1;
                mainMenu.gameObject.SetActive (false);
                foreach (var i in gamePlayCanvas) i.gameObject.SetActive (true);
            }
            m_ShowMainCanvas = show;
        }

        void Update () {
            if (Input.GetButtonDown ("Menu")) {
                HandleMenuButton ();
            }
        }

        void HandleMenuButton () {
            ToggleMainMenu (show: !m_ShowMainCanvas);
            Time.timeScale = m_ShowMainCanvas ? 0f : 1f;

            if (m_FirstTime) {
                if (Client.Ins == null) {
                    raceCountdownTrigger.TriggerDirector ();
                    m_FirstTime = false;
                }
            }
        }
    }
}

// using System;
// using System.Collections.Generic;
// using KartGame.KartSystems;
// using KartGame.Timeline;
// using com.unity.mgobe;
// using com.unity.mgobe.src.Util;
// using UnityEngine;

// namespace KartGame.UI {
//     public class OtherKart {
//         public GameObject go;

//         /**
//          go.GetComponent<KartMovement> ().input = input;
//          go.GetComponent<KartAnimation> ().input = input;
//         */
//         public NetworkInput input;

//         KartMovement movement;
//         Vector3 pos;
//         Quaternion rot;

//         public OtherKart (GameObject go, NetworkInput input) {
//             this.go = go;
//             this.input = input;
//             movement = go.GetComponent<KartMovement> ();
//             movement.IsOtherKart = true;

//             pos = new Vector3 ();
//             rot = new Quaternion ();
//         }

//         public void OnFrame (string str) {
//             string[] vals = str.Split (',');
//             if (vals.Length == 9) {
//                 int intAcc;
//                 int intSteer;
//                 float px, py, pz;
//                 float rx, ry, rz, rw;
//                 if (Int32.TryParse (vals[0], out intAcc) &&
//                     Int32.TryParse (vals[1], out intSteer) &&
//                     float.TryParse (vals[2], out px) &&
//                     float.TryParse (vals[3], out py) &&
//                     float.TryParse (vals[4], out pz) &&
//                     float.TryParse (vals[5], out rx) &&
//                     float.TryParse (vals[6], out ry) &&
//                     float.TryParse (vals[7], out rz) &&
//                     float.TryParse (vals[8], out rw)) {
//                     input.m_Acceleration = intAcc;
//                     input.m_Steering = intSteer;
//                     pos.x = px;
//                     pos.y = py;
//                     pos.z = pz;
//                     rot.x = rx;
//                     rot.y = ry;
//                     rot.z = rz;
//                     rot.w = rw;
//                     rot.Normalize ();

//                     movement.syncPosition = pos;
//                     movement.syncRotation = rot;
//                 }
//             }
//         }
//     }

//     /// <summary>
//     /// The MetaGameController is responsible for switching control between the high level
//     /// contexts of the application, eg the Main Menu and Gameplay systems.
//     /// </summary>
//     public class MetaGameController : MonoBehaviour {
//         [Tooltip ("A reference to the main menu.")]
//         public MainUIController mainMenu;
//         [Tooltip ("A reference to the race countdown director trigger.")]
//         public DirectorTrigger raceCountdownTrigger;
//         [Tooltip ("The UI canvases used for game play.")]
//         public Canvas[] gamePlayCanvas;

//         public GameObject myKart;
//         public Joystick joystick;

//         public IInput myKartInput;
//         public IKartInfo myKartInfo;
//         public Dictionary<string, OtherKart> otherFighters;
//         public bool isStarted = false;

//         bool m_ShowMainCanvas = true;
//         bool m_FirstTime = true;

//         void OnEnable () {
//             _ToggleMainMenu (m_ShowMainCanvas);
//         }
//         void Start () {
//             if (Client.Ins != null) {
//                 StartGame_Network ();
//             } else {
//                 InitMyKart ();
//             }

//             //Start the game immediately rather than show the pause menu.
//             HandleMenuButton ();
//         }

//         void StartGame_Network () {
//             Client.Ins.game = this;
//             Client.Ins.uiCanvas.gameObject.SetActive (false);

//             InitKarts_Network ();
//             isStarted = true;
//         }

//         void InitKarts_Network () {
//             otherFighters = new Dictionary<string, OtherKart> ();
//             var myKartIdx = 0;
//             for (int idx = 0; idx < Global.Room.RoomInfo.PlayerList.Count; ++idx) {
//                 var playerInfo = Global.Room.RoomInfo.PlayerList[idx];
//                 if (playerInfo.Id.Equals(Player.Id)) {
//                     myKartIdx = idx;
//                     // // my kart
//                     // myKart.transform.position = GetKartStartPos_Network (idx);
//                     // InitMyKart ();
//                 } else {
//                     // other karts
//                     var go = UnityEngine.Object.Instantiate (myKart, GetKartStartPos_Network (idx), Quaternion.identity);
//                     var netInput = myKart.AddComponent<NetworkInput> ();
//                     go.GetComponent<KartMovement> ().input = netInput;
//                     go.GetComponent<KartAnimation> ().input = netInput;
//                     if (!go.activeSelf) {
//                         go.SetActive (true);
//                     }
//                     otherFighters[playerInfo.Id] = new OtherKart (go, netInput);
//                 }
//             }
//             // my kart
//             myKart.transform.position = GetKartStartPos_Network (myKartIdx);
//             InitMyKart ();
//         }

//         void InitMyKart () {
//             myKartInfo = myKart.GetComponent<KartMovement> ();

//             var jsInput = myKart.AddComponent<JoystickInput> ();
//             jsInput.joystick = joystick;
//             myKartInput = jsInput;
//             myKart.GetComponent<KartMovement> ().input = myKartInput;
//             myKart.GetComponent<KartAnimation> ().input = myKartInput;
//             myKart.SetActive (true);
//         }

//         Vector3 GetKartStartPos_Network (int idx) {
//             return new Vector3 (16.0f, 0.8f, idx * (-6.0f));
//         }

//         /// <summary>
//         /// Turns the main menu on or off.
//         /// </summary>
//         public void ToggleMainMenu (bool show) {
//             if (m_ShowMainCanvas != show) {
//                 _ToggleMainMenu (show);
//             }
//         }

//         void _ToggleMainMenu (bool show) {
//             if (show) {
//                 // WORKAROUND: This is due to a problem where setting the time scale to 0 causes audio to stop playing.
//                 Time.timeScale = 0.00001f;
//                 mainMenu.gameObject.SetActive (true);
//                 foreach (var i in gamePlayCanvas) i.gameObject.SetActive (false);
//             } else {
//                 Time.timeScale = 1;
//                 mainMenu.gameObject.SetActive (false);
//                 foreach (var i in gamePlayCanvas) i.gameObject.SetActive (true);
//             }
//             m_ShowMainCanvas = show;
//         }

//         void Update () {
//             if (Input.GetButtonDown ("Menu")) {
//                 HandleMenuButton ();
//             }
//         }

//         void HandleMenuButton () {
//             ToggleMainMenu (show: !m_ShowMainCanvas);
//             Time.timeScale = m_ShowMainCanvas ? 0f : 1f;

//             if (m_FirstTime) {
//                 if (Client.Ins == null) {
//                     raceCountdownTrigger.TriggerDirector ();
//                     m_FirstTime = false;
//                 }
//             }
//         }
//     }
// }