using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour
{
    public InputField myOpenId;
    public Text myPlayerId;
    public Button loginBtn;

    private void Start()
    {
       // myOpenId.text = string.Format("Lz#{0}", UnityEngine.Random.Range(1, 1000));
       myOpenId.text = Client.CreateUserName();
    }
}
