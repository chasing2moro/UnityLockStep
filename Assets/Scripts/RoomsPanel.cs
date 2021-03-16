using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using com.unity.mgobe;

public class RoomsPanel : MonoBehaviour
{
    public Text createRoomBtnText;
    public GameObject roomBtnsRoot;
    public GameObject roomBtnPrefab;
    public Text selectedRoomText;

    public Button createBtn;
    public Button joinBtn;
    public Button leaveBtn;
    public Button setReadyBtn;

    public RoomInPanel roomInPanel;

    int selectedRoomIndex = -1;

    private void Start()
    {
        //createRoomBtnText.text = string.Format("新建房间 Room#{0}", UnityEngine.Random.Range(1, 1000));
    }

    public void UpdateRoomListBtns(List<RoomInfo> roomList)
    {
        //Debug.Log("UpdateRoomListBtns:"+roomList);
        RectTransform root = roomBtnsRoot.GetComponent<RectTransform>();
        for (int i = 0; i < root.childCount; ++i)
        {
            Transform child = root.GetChild(i);
            Button btn = child.GetComponent<Button>();
            if (btn != null && btn.IsActive())
            {
                Object.Destroy(child.gameObject);
            }
        }

        if (roomList != null)
        {
            for (int i = 0; i < roomList.Count; ++i)
            {
                GameObject newBtnGo = Object.Instantiate(roomBtnPrefab);
                RectTransform newBtnRt = newBtnGo.GetComponent<RectTransform>();
                newBtnRt.SetParent(root);
                //newBtnGo.transform.localScale = new Vector3(1, 1, 1);
                newBtnRt.anchoredPosition = new Vector2(145 + 340 * (i % 5), -40 + (i / 5) * (-380));
                //Text btnText = newBtnRt.GetChild(0).GetComponent<Text>();
                //btnText.text = string.Format("{0}/{1}", roomList[i].Name, roomList[i].Id);
                RoomButton roomBtn = newBtnGo.GetComponent<RoomButton>();
                roomBtn.UpdateListener(this, roomInPanel, roomList, i);

                newBtnGo.transform.Find("RoomIndex").GetComponent<Text>().text = "房间" + roomList[i].Name;
                newBtnGo.transform.Find("Number").GetComponent<Text>().text = roomList[i].PlayerList.Count + "/" + roomList[i].MaxPlayers;

                newBtnGo.SetActive(true);
            }

            root.sizeDelta = new Vector2(root.sizeDelta.x, roomList.Count * 80 + 20);
        }

        UpdateSelectedRoom(roomList, selectedRoomIndex);
    }

    public void UpdateSelectedRoom(List<RoomInfo> roomList, int idx)
    {
        selectedRoomIndex = idx;
        string sm = "";
        if (roomList != null && selectedRoomIndex >= 0 && selectedRoomIndex < roomList.Count)
        {
            RoomInfo ri = roomList[selectedRoomIndex];
            sm += string.Format("Name: {0}\n", ri.Name);
            sm += string.Format("id: {0}\n", ri.Id);
            sm += string.Format("owner: {0}\n", ri.Owner);
            sm += string.Format("routeId: {0}\n", ri.RouteId);
            sm += string.Format("FrameSyncState: {0}\n", ri.FrameSyncState);
            sm += string.Format("FrameRate: {0}\n", ri.FrameRate);
            sm += string.Format("Players (count={0}):\n", ri.PlayerList.Count);
            for (int i = 0; i < ri.PlayerList.Count; ++i)
            {
                var pi = ri.PlayerList[i];
                sm += string.Format("    Name({0}) teamId({1}) isReady({2})\n", pi.Name, pi.TeamId, pi.CustomPlayerStatus);
            }

            sm += string.Format("Teams (count={0}):\n", ri.TeamList.Count);
            for (int i = 0; i < ri.TeamList.Count; ++i)
            {
                var ti = ri.TeamList[i];
                sm += string.Format("    id({0}) Name({1}) min({2}) max{{3}}\n",
                    ti.Id, ti.Name, ti.MinPlayers, ti.MinPlayers);
            }
        }

        selectedRoomText.text = sm;
        var rt = selectedRoomText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(selectedRoomText.preferredWidth, selectedRoomText.preferredHeight);

        UpdateButtonsInteractive(roomList);
    }

    public int GetSelectionIndex(List<RoomInfo> roomList)
    {
        if (roomList == null)
            return -1;

        if (selectedRoomIndex >= 0 && selectedRoomIndex < roomList.Count)
            return selectedRoomIndex;

        return -1;
    }

    public void UpdateButtonsInteractive(List<RoomInfo> roomList)
    {
        createBtn.interactable = !Global.IsInRoom();
        joinBtn.interactable = !Global.IsInRoom() && (GetSelectionIndex(roomList) != -1);
        leaveBtn.interactable = Global.IsInRoom();
        setReadyBtn.interactable = Global.IsInRoom() && (!Client.Ins.isReadyToBattle) && (!Client.Ins.isInBattle);
    }
}
