using Barotrauma.Items.Components;

namespace DockyardTools;

public interface IDataBusSubscriber
{
    public void OnSignalReceived(Signal source, Connection connection, string messageId, object? data);
}