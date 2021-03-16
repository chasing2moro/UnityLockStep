using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomPanel : MonoBehaviour {
    public InputField roomName;
    public InputField roomMaxPlayerNumber;
    public Button createRoomBtn;
    private string name;
    private uint maxPlayerNumber;
    public bool canCreate = false;
    // Start is called before the first frame update
    void Start () {
        roomName.text = "";
        roomMaxPlayerNumber.text = "2";
    }

    // Update is called once per frame
    void Update () {
        uint num = roomMaxPlayerNumber.text == "" ? 0 : (uint) int.Parse (roomMaxPlayerNumber.text);
        if (canCreate) {
            name = roomName.text;
            maxPlayerNumber = num;
        }
    }

    public string getName () {
        return name;
    }
    public uint getMaxPlayerNumber () {
        return maxPlayerNumber;
    }
}