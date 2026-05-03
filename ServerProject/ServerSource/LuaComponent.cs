using Barotrauma.Networking;

namespace DockyardTools;

public partial class LuaComponent : IClientSerializable, IServerSerializable
{
  public void ServerEventRead(IReadMessage msg, Client c)
  {
    ReadNetworkData(msg);
  }

  public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
  {
    WriteNetworkData(msg);
  }
}