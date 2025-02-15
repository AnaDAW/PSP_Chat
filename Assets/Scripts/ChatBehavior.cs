// Comportamiento básico del servicio WebSocket: simplemente devuelve el mensaje recibido.
using System;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;

public class ChatBehavior : WebSocketBehavior
{
    private static int clientCount = 0; // Cantidad de conexiones activas
    private static Dictionary<string, string[]> clients = new Dictionary<string, string[]>(); // Clientes conectados

    protected override void OnOpen()
    {
        lock (clients)
        {
            clients[ID] = new string[2];
            clients[ID][0] = "Usuario" + (++clientCount);
            clients[ID][1] = CreateRandomColor();
        }
        
        // Formatear el mensaje con el color del usuario
        string formattedMessage = $"<color={clients[ID][1]}><b>{clients[ID][0]}</b></color> {ID} se ha conectado";
        Sessions.Broadcast(formattedMessage);
    }

    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        // Formatear el mensaje con el color del usuario
        string formattedMessage = $"<color={clients[ID][1]}><b>{clients[ID][0]}:</b></color> {e.Data}";

        // Envía de vuelta el mismo mensaje recibido.
        Sessions.Broadcast(formattedMessage);
    }

    protected override void OnClose(CloseEventArgs e)
    {
        lock (clients)
        {
            clientCount--;
            clients.Remove(ID);
        }

        // Formatear el mensaje con el color del usuario
        string formattedMessage = $"<color={clients[ID][1]}><b>{clients[ID][0]}</b></color> se ha desconectado";
        Sessions.Broadcast(formattedMessage);
    }

    // Crea un color a partir de un random hexadecimal
    private string CreateRandomColor()
    {
        Random random = new Random();
        string randomColor = $"#{random.Next(0x1000000):X6}";
        return randomColor;
    }
}