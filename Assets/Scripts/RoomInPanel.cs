using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using com.unity.mgobe;

// using com.unity.mgobe;


public class RoomInPanel : MonoBehaviour
{
    public Button setReadyBtn;
    public Button backBtn;
    public Text roomName;
    public Text playerNumber;
    public GameObject playerPrefab;
    public GameObject playerBtnsRoot;
    public Button cancelReadyBtn;

    int selectedRoomIndex = -1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowRoomInfo(RoomInfo roomInfo)
    {
        RectTransform root = playerBtnsRoot.GetComponent<RectTransform>();
        for (int i = 0; i < root.childCount; ++i)
        {
            Transform child = root.GetChild(i);
            Button btn = child.GetComponent<Button>();
            if (btn != null && btn.IsActive())
            {
                Object.Destroy(child.gameObject);
            }
        }
        if (roomInfo != null)
        {
            roomName.text = roomInfo.Name;
            playerNumber.text = roomInfo.PlayerList.Count + "/" + roomInfo.MaxPlayers;
            for (int i = 0; i < roomInfo.PlayerList.Count; i++)
            {
                GameObject newPlayerBtn = Object.Instantiate(playerPrefab);
                RectTransform newBtnRt = newPlayerBtn.GetComponent<RectTransform>();
                newBtnRt.SetParent(root);
                newBtnRt.anchoredPosition = new Vector2(145 + 340 * (i % 5), -40 + (i / 5) * (-380));
                //Text btnText = newBtnRt.GetChild(0).GetComponent<Text>();
                //btnText.text = string.Format("{0}/{1}", roomInfo.PlayerList[i].Name, roomInfo.PlayerList[i].Id);
                RoomButton roomBtn = newPlayerBtn.GetComponent<RoomButton>();

                newPlayerBtn.transform.Find("PlayerName").GetComponent<Text>().text = "用户" + roomInfo.PlayerList[i].Name;
                if (roomInfo.PlayerList[i].CustomPlayerStatus == 0)
                {
                    newPlayerBtn.transform.Find("SetReadyState").GetComponent<Text>().text = "未准备";
                    newPlayerBtn.transform.Find("ReadyStateImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("slice/statuspreparedness");
                }
                else
                {
                    newPlayerBtn.transform.Find("SetReadyState").GetComponent<Text>().text = "已准备";
                    newPlayerBtn.transform.Find("ReadyStateImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("slice/statusready");
                }
                newPlayerBtn.SetActive(true);
            }
            setReadyBtn.interactable = true;
        }
        
    }

    public void UpdateSelectedRoom(List<RoomInfo> roomList, int idx)
    {
        selectedRoomIndex = idx;
        /*
        string sm = "";
        if (roomList != null && selectedRoomIndex >= 0 && selectedRoomIndex < roomList.Count)
        {
            RoomInfo ri = roomList[selectedRoomIndex];
            sm += string.Format("name: {0}\n", ri.Name);
            sm += string.Format("id: {0}\n", ri.Id);
            sm += string.Format("owner: {0}\n", ri.Owner);
            sm += string.Format("routeId: {0}\n", ri.RouteId);
            sm += string.Format("FrameSyncState: {0}\n", ri.FrameSyncState);
            sm += string.Format("FrameRate: {0}\n", ri.FrameRate);
            sm += string.Format("Players (count={0}):\n", ri.PlayerList.Count);
            for (int i = 0; i < ri.PlayerList.Count; ++i)
            {
                var pi = ri.PlayerList[i];
                sm += string.Format("    name({0}) teamId({1}) isReady({2})\n", pi.Name, pi.TeamId, pi.CustomPlayerStatus);
            }

            sm += string.Format("Teams (count={0}):\n", ri.TeamList.Count);
            for (int i = 0; i < ri.TeamList.Count; ++i)
            {
                var ti = ri.TeamList[i];
                sm += string.Format("    id({0}) name({1}) min({2}) max{{3}}\n",
                    ti.Id, ti.Name, ti.MinPlayers, ti.MinPlayers);
            }
        }
        */
    }
}
