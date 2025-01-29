namespace FtrackDotNet.EventHub;

internal class SocketIOFactory : ISocketIOFactory
{
    public ISocketIO Create(Uri url)
    {
        return new SocketIO(url);
    }
}