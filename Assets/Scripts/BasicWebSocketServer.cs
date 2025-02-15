using System.Net.Sockets;
using UnityEngine;
using WebSocketSharp.Server;

// Clase que se adjunta a un GameObject en Unity para iniciar el servidor WebSocket.
public class BasicWebSocketServer : MonoBehaviour
{
    public GameObject client;

    // Instancia del servidor WebSocket.
    private WebSocketServer wss;
    private int port = 7777;

    // Se ejecuta al iniciar la escena.
    void Start()
    {
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

    public void TryToConnect()
    {
        try
        {
            // Comprueba si el puerto está activo
            TcpClient client = new TcpClient();
            client.Connect("localhost", port);
            gameObject.SetActive(false);
        } catch (SocketException)
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
    }
}
