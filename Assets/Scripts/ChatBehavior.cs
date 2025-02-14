// Comportamiento básico del servicio WebSocket: simplemente devuelve el mensaje recibido.
using WebSocketSharp;
using WebSocketSharp.Server;

public class ChatBehavior : WebSocketBehavior
{
    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        // Envía de vuelta el mismo mensaje recibido.
        //Send(e.Data);
        Sessions.Broadcast(e.Data);
    }
}