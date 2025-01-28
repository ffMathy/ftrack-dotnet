namespace FtrackDotNet.EventHub;

public class SocketIOFactory : ISocketIOFactory
{
    public ISocketIO Create(Uri url)
    {
        return new SocketIO(url);
    }
}