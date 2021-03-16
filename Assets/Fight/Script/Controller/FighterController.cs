using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterController: MonoBehaviour, KartGame.KartSystems.IControllable
{
    public  KartGame.KartSystems.JoystickInput input;

    bool m_HasControl;
    /// <summary>
    /// This exists as part of the IMovable interface.  Typically it is called by the TrackManager when the race starts.
    /// </summary>
    public void EnableControl()
    {
        m_HasControl = true;
    }

    /// <summary>
    /// This exists as part of the IMovable interface.  Typically it is called by the TrackManager when the kart finishes its final lap.
    /// </summary>
    public void DisableControl()
    {
        m_HasControl = false;
    }

    /// <summary>
    /// This exists as part of the IMovable interface.  Typically it is called by the TrackManager to determine whether control should be re-enabled after a reposition. 
    /// </summary>
    /// <returns></returns>
    public bool IsControlled()
    {
        return m_HasControl;
    }

    public FighterLogic fighterLogic;
    Queue<FighterServerData> serverDatas = new Queue<FighterServerData>();
    void Awake()
    {

    }

    public void AddNetMsg(List<FighterServerData> vServerDatas)
    {
        foreach (var item in vServerDatas)
        {
            serverDatas.Enqueue(item);
        }
    }

    float deltaTime;
    void Update()
    {
        if (serverDatas.Count == 0)
            return;

        deltaTime = Time.deltaTime;
        fighterLogic.HandleData(serverDatas.Dequeue());
        fighterLogic.OnUpdate(ref deltaTime);
    }
}
