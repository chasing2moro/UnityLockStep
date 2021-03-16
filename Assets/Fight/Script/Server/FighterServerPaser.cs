
using System;
using System.Collections.Generic;

public class FighterServerPaser
{

    public Dictionary<UInt64, FighterServerData> serverDatasMySelf;
    public Dictionary<UInt64, FighterServerData> serverDatasOther;

    public FighterServerPaser()
    {
        this.serverDatasMySelf = new Dictionary<ulong, FighterServerData>();
        this.serverDatasOther = new Dictionary<ulong, FighterServerData>();
    }

    public FighterServerData OnFrameMySelf(string str)
    {
        return this.Enqueue(serverDatasMySelf, str);
    }

    public FighterServerData OnFrameOther(string str)
    {
        return this.Enqueue(serverDatasOther, str);
    }

    public FighterServerData Enqueue(Dictionary<ulong, FighterServerData> serverDataQueue, string str)
    {
        FighterServerData serverData = new FighterServerData();
        serverData.Deserialize(str);
        serverDataQueue[serverData.frameId] = serverData;
        return serverData;
    }

}