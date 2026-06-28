using Barotrauma.Items.Components;

namespace DockyardTools;


public class SignalBusController : ItemComponent
{
  public static readonly string BUSID_DELIMITER = "::";
  public string SignalBusInName { get; init; }
  public string SignalBusOutName { get; init; }
  
  private readonly ConcurrentDictionary<Guid, (string MessageId, object Data)> _dataCache = new();

  private readonly ConcurrentDictionary<string, List<IDataBusSubscriber>> _subscribers = new();
  
  public SignalBusController(Item item, ContentXElement element) : base(item, element)
  {
    // ReSharper disable once VirtualMemberCallInConstructor
    IsActive = true;
    SignalBusInName = element.GetAttributeString("signalBusInName", "DataBusIn");
    SignalBusOutName = element.GetAttributeString("signalBusOutName", "DataBusOut");
  }

  public void Subscribe(string messageId, IDataBusSubscriber subscriber)
  {
    var subs = _subscribers.GetOrAdd(messageId, id => new List<IDataBusSubscriber>());
    subs.Add(subscriber);
  }

  public void Unsubscribe(string messageId, IDataBusSubscriber subscriber)
  {
    if (!_subscribers.TryGetValue(messageId, out var subs))
    {
      return;
    }
    
    subs.Remove(subscriber);
  }

  public void SendDataObject(string messageId, object data)
  {
    if (messageId.IsNullOrWhiteSpace())
    {
      throw new ArgumentNullException($"{nameof(SendDataObject)}: {nameof(messageId)} is null.");
    }
    
    if (data is null)
    {
      throw new ArgumentNullException($"{nameof(SendDataObject)}: {nameof(data)} is null for message {messageId}.");
    }

    var busContentId = Guid.NewGuid();
    _dataCache[busContentId] = (messageId, data);
    
    Item.SendSignal($"{SignalBusOutName}{BUSID_DELIMITER}{busContentId.ToString()}", SignalBusOutName);
  }

  public override void ReceiveSignal(Signal signal, Connection connection)
  {
    base.ReceiveSignal(signal, connection);
    if (connection.Name != SignalBusInName)
    {
      return;
    }
    
    var signalComponents = signal.value.Split(BUSID_DELIMITER, StringSplitOptions.None);
    if (signalComponents.Length != 2)
    {
      return;
    }

    var guidParseSuccess = Guid.TryParse(signalComponents[1], out var busContentId);
    if (!guidParseSuccess)
    {
      return;
    }
    
    var source = signal.source.GetComponents<SignalBusController>()
      .FirstOrDefault(sbc => sbc?.SignalBusOutName == signalComponents[0], null);
    if (source is null)
    {
      return;
    }

    if (!source.TryGetDataObject(busContentId, out var dataPack))
    {
      return;
    }
    
    if (dataPack.MessageId.IsNullOrWhiteSpace())
    {
      return;
    }

    if (!_subscribers.TryGetValue(dataPack.MessageId, out var subscribers) || subscribers.Count == 0)
    {
      return;
    }

    foreach (var subscriber in subscribers)
    {
      subscriber.OnSignalReceived(signal, connection, dataPack.MessageId, dataPack.Data);
    }
  }

  private bool TryGetDataObject(Guid busId, out (string MessageId, object Data) dataPack)
  {
    // remove the data from the cache once time has been given for signals to reach recipients.
    // TTL = 2 seconds.
    CoroutineManager.Invoke(() =>
    {
      _dataCache.TryRemove(busId, out _);
    }, 2f);
    
    return _dataCache.TryGetValue(busId, out dataPack);
  }
}