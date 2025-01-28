namespace FtrackDotNet.EventHub;

public interface ISocketIOFactory
{
    ISocketIO Create(Uri url);
}