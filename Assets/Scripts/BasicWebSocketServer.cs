using System;
using System.Net.Sockets;
using UnityEngine;
using WebSocketSharp.Server;

// Clase que se adjunta a un GameObject en Unity para iniciar el servidor WebSocket.
public class BasicWebSocketServer : MonoBehaviour
{
    public GameObject client; // GameObject con el script del cliente

    private WebSocketServer wss; // Instancia del servidor WebSocket.
    private int port = 7777; // Puerto del servidor

    // Se ejecuta al iniciar la escena.
    void Start()
    {
        // Intenta conectarse y activa el cliente
        TryToConnect();
        client.SetActive(true);
    }

    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cerrar la aplicación o cambiar de escena).
    void OnDestroy()
    {
        // Si el servidor está activo, se detiene de forma limpia.
        if (wss != null)
        {
            wss.Stop();
            wss = null;
            Debug.Log("Servidor WebSocket detenido.");
        }
    }

    private void TryToConnect()
    {
        try
        {
            // Comprueba si el puerto está activo y desactiva el servidor
            using (TcpClient client = new TcpClient())
            {
                client.Connect("localhost", port);
            }
            gameObject.SetActive(false);
        }
        catch (SocketException)
        {
            try
            {
                Debug.Log("Iniciando servidor...");
                // Crear un servidor WebSocket que escucha en el puerto 7777.
                wss = new WebSocketServer(port);

                // Añadir un servicio en la ruta "/" que utiliza el comportamiento ChatBehavior.
                wss.AddWebSocketService<ChatBehavior>("/");

                // Iniciar el servidor.
                wss.Start();

                Debug.Log("Servidor WebSocket iniciado en ws://127.0.0.1:7777/");
            }
            catch (Exception e)
            {
                Debug.Log("Error al intentar iniciar servidor: " + e.Message);
            }
        }
    }
}
