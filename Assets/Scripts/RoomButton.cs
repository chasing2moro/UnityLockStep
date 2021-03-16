using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using com.unity.mgobe;

public class RoomButton : MonoBehaviour
{
    RoomsPanel parentPanel;
    RoomInPanel roomInPanel;
    List<RoomInfo> roomList;
    int roomIndex = -1;

    public Client client;

    public void UpdateListener(RoomsPanel panel, RoomInPanel roomPanel, List<RoomInfo> roomList, int idx)
    {
        parentPanel = panel;
        roomInPanel = roomPanel;
        this.roomList = roomList;
        roomIndex = idx;

        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        parentPanel.UpdateSelectedRoom(roomList, roomIndex);
        roomInPanel.UpdateSelectedRoom(roomList, roomIndex);
        // roomInPanel.gameObject.SetActive(true);
        // parentPanel.gameObject.SetActive(false);
        client.JoinRoom();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
