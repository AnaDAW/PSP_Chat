// Comportamiento básico del servicio WebSocket: simplemente devuelve el mensaje recibido.
using System;
using System.Collections.Generic;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;
using UnityEngine;
using Random = System.Random;

public class ChatBehavior : WebSocketBehavior
{
    private static int clientCount = 0; // Cantidad de conexiones activas
    private static Dictionary<string, string[]> clients = new Dictionary<string, string[]>(); // Clientes conectados
    private static string historicFile = string.Empty; // El archivo donde se guarda el histórico
    private static List<string> usersWriting = new List<string>(); // Usuarios que están escribiendo

    // Se invoca cuando se conecta un cliente
    protected override void OnOpen()
    {
        // Bloque el acceso a los clientes
        lock (clients)
        {
            // Si el histórico no tiene nombre se lo coloca utilizando la fecha y hora actual
            if (string.IsNullOrEmpty(historicFile)) historicFile = $"historic-{DateTime.Now.ToString("yyyMMddHHmmss")}.log";

            // Genera y guarda el nombre de usuario y color
            clients[ID] = new string[2];
            clients[ID][0] = "Usuario" + (++clientCount);
            clients[ID][1] = CreateRandomColor();
            Debug.Log("Usuario creado: " + clients[ID]);

            // Envia el número de usuarios actuales a todos
            Sessions.Broadcast("online:" + clientCount);
        }

        // Devuelve el nombre de usuario y el color creado
        Send($"username:{clients[ID][0]}:{clients[ID][1]}");
        
        // Formatear el mensaje con el color del usuario y lo envía a todos
        string formattedMessage = $"<color={clients[ID][1]}><b>{clients[ID][0]}</b></color> se ha conectado";
        Sessions.Broadcast(formattedMessage);

        // Guarda el mensaje en el histórico
        SaveMessageInHistoric($"{clients[ID][0]} se ha conectado");
    }

    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        string formattedMessage;
        if (e.Data.StartsWith("writing:"))
        {
            // Si el mensaje empieza por writing: se bloque la variable usersWriting
            lock (usersWriting)
            {
                formattedMessage = "writing";
                if (e.Data.EndsWith("no"))
                {
                    // Si el mensaje termina por no se elimina al usuario de la lista
                    usersWriting.Remove(ID);
                }
                else
                {
                    // Si el usuario no existe se añade a la lista
                    if(!usersWriting.Contains(ID)) usersWriting.Add(ID);
                }

                // Recorre los usuarios de la lista e inserta el nombre de manera formateada
                foreach (string user in usersWriting)
                {
                    formattedMessage += $":<color={clients[user][1]}><b>{clients[user][0]}</b></color>";
                }
                
                // Si la lista está vacía inserta :no
                if(usersWriting.Count == 0) formattedMessage += ":no";
            }
        }
        else
        {
            // Formatear el mensaje recibido con el color del usuario
            formattedMessage = $"<color={clients[ID][1]}><b>{clients[ID][0]}:</b></color> {e.Data}";
            // Guarda el mensaje en el histórico
            SaveMessageInHistoric($"{clients[ID][0]}: {e.Data}");
        }
        // Envía el mensaje a todos
        Sessions.Broadcast(formattedMessage);
    }

    // Se invoca cuando se desconecta un cliente
    protected override void OnClose(CloseEventArgs e)
    {
        string writingMessage = "writing";
        // Bloque la variable usersWriting
        lock (usersWriting)
        {
            // Se asegura de eliminar al cliente de la lista
            usersWriting.Remove(ID);
            // Recorre los usuarios de la lista e inserta el nombre de manera formateada
            foreach (string user in usersWriting)
            {
                writingMessage += $":<color={clients[user][1]}><b>{clients[user][0]}</b></color>";
            }
             // Si la lista está vacía inserta :no
            if(usersWriting.Count == 0) writingMessage += ":no";
        }
        // Envía el resultado de recorrer la lista a todos
        Sessions.Broadcast(writingMessage);

        // Formatear el mensaje con el color del usuario y lo envía a todos
        string formattedMessage = $"<color={clients[ID][1]}><b>{clients[ID][0]}</b></color> se ha desconectado";
        Sessions.Broadcast(formattedMessage);
        SaveMessageInHistoric($"{clients[ID][0]} se ha desconectado");

        // Bloque el acceso a los clientes y elimina el cliente desconectado
        lock (clients)
        {
            clientCount--;
            clients.Remove(ID);
            
            // Envia el número de usuarios actuales a todos
            Sessions.Broadcast("online:" + clientCount);
        }
    }

    // Crea un color a partir de un random hexadecimal
    private string CreateRandomColor()
    {
        Random random = new Random();
        string randomColor = $"#{random.Next(0x1000000):X6}";
        return randomColor;
    }

    // Guarda el mensaje en el histórico
    private void SaveMessageInHistoric(string message)
    {
        // Comprueba si existe la carpeta en la raiz de la aplicación, en caso contrario la crea
        string directory = Path.Combine(Environment.CurrentDirectory, "HistoricLog");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        // Comprueba si existe el archivo dentro de la carpeta, en caso contrario lo crea
        string path = Path.Combine(directory, historicFile);
        if (!File.Exists(path)) File.Create(path).Close();
        // Escribe el mensaje recibido junto a la fecha actual
        File.AppendAllText(path, $"{DateTime.Now} - {message}{Environment.NewLine}");
        Debug.Log("Mensaje guardado: " + path);
    }
}