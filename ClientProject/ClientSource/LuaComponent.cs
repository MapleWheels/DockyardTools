using Barotrauma.LuaCs;
using Barotrauma.Networking;

namespace DockyardTools;

public partial class LuaComponent : IClientSerializable, IServerSerializable
{
  public void ClientEventWrite(IWriteMessage msg, NetEntityEvent.IData extraData = null)
  {
    if (NetSyncMode is NetSync.None or NetSync.ServerAuthority)
    {
      return;
    }
    WriteNetworkData(msg);
  }

  public void ClientEventRead(IReadMessage msg, float sendingTime)
  {
    ReadNetworkData(msg);
  }
}