using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class BasicWebSocketClient : MonoBehaviour
{
    private static Queue<Action> _actionToRun = new Queue<Action>(); // Cola con las acciones a realizar en la GUI

    public TMP_Text chatDisplay;  // Texto donde se muestra el historial del chat
    public TMP_InputField inputField; // Input donde el usuario escribe
    public Button sendButton; // Botón para enviar mensajes
    public ScrollRect scrollRect; // Scroll View para manejar el desplazamiento
    public TMP_Text onlineCount; // Contador de usuarios online
    public TMP_Text username; // Nombre de usuario del cliente
    public TMP_Text writingMessage; // Mensaje que indica usuarios escribiendo

    private WebSocket ws; // Instancia del cliente WebSocket

    // Se ejecuta al iniciar la escena
    void Start()
    {
        // Intenta conectase al servidor
        TryToConnect();

        // Añade listeners al botón y al inputField para enviar los mensajes al servidor
        sendButton.onClick.AddListener(SendMessageToServer);
        inputField.onSubmit.AddListener(delegate { SendMessageToServer(); });
        inputField.onValueChanged.AddListener(SendWritingToServer);
        
        //Limpiar el chatDisplay
        chatDisplay.text = "";

        // Se asegura que el inputField este seleccionado y el foco activo
        inputField.Select();
        inputField.ActivateInputField();
    }

    void Update() {
        // Busca acciones en cola y las ejecuta
        if (_actionToRun.Count > 0)
        {
            Action action;

            // Bloquea el acceso a la cola de acciones para eliminar la realizada
            lock (_actionToRun)
            {
                action = _actionToRun.Dequeue();
            }

            // Realiza la acción
            action?.Invoke();
        }
    }

    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cambiar de escena o cerrar la aplicación)
    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }

    private void TryToConnect()
    {
        // Crear una instancia del WebSocket apuntando a la URI del servidor
        ws = new WebSocket("ws://127.0.0.1:7777/");

        // Evento OnOpen: se invoca cuando se establece la conexión con el servidor
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket conectado correctamente.");
        };

        // Evento OnMessage: se invoca cuando se recibe un mensaje del servidor
        ws.OnMessage += (sender, e) =>
        {
            if (e.Data.StartsWith("online:"))
            {
                // Si el mensaje empieza por online: actualiza el texto de usuarios online
                EnqueueUIAction(() => onlineCount.text = e.Data.Split(":")[1] + " Usuario/s");
            }
            else if (e.Data.StartsWith("username:"))
            {
                // Si el mensaje empieza por username: actualiza el texto del nombre del usuario
                string[] user = e.Data.Split(":");
                EnqueueUIAction(() => username.text = $"Nombre: <color={user[2]}><b>{user[1]}</b></color>");
            }
            else if (e.Data.StartsWith("writing:"))
            {
                // Si el mensaje empieza por writing: actualiza el texto de los usuarios escribiendo
                string[] users = e.Data.Split(":");
                string message = "";
                // Si no tiene usuarios deja el mensaje vacío
                if (users[1] != "no")
                {
                    // Si tiene usuarios los recorre y los inserta separados por comas
                    for (int i = 1; i < users.Length; i++)
                    {
                        message += users[i];
                        if (i != users.Length - 1) message += ", ";
                    }
                    message += " escribiendo...";
                }
                EnqueueUIAction(() => writingMessage.text = message);
            }
            else
            {
                // Recoge el mensaje y lo imprime en el chat
                EnqueueUIAction(() => ShowMessageInChat(e.Data));
            }
            Debug.Log("Mensaje recibido: " + e.Data);
        };

        // Evento OnError: se invoca cuando ocurre un error en la conexión
        ws.OnError += (sender, e) =>
        {
            Debug.LogError("Error en el WebSocket: " + e.Message);
        };

        // Evento OnClose: se invoca cuando se cierra la conexión con el servidor
        ws.OnClose += (sender, e) =>
        {
            if (e.Code == 1001)
            {
                // Si se cierra la conexión por desconexión del servidor lo muestra en el chat
                EnqueueUIAction(() => ShowMessageInChat("Host desconectado"));
            }
            Debug.Log("WebSocket cerrado. Código: " + e.Code + ", Razón: " + e.Reason);
        };

        // Conectar de forma asíncrona al servidor WebSocket
        ws.ConnectAsync();
    }

    private void ShowMessageInChat(string message)
    {
        // Agregar el mensaje al historial del chat
        chatDisplay.text += "\n" + message;

        // Forzar actualización del Layout para el Scroll
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatDisplay.rectTransform);

        // Hacer que el Scroll se desplace hasta el final
        ScrollToBottom();
    }

    // Método para enviar un mensaje al servidor (puedes llamarlo, por ejemplo, desde un botón en la UI)
    private void SendMessageToServer()
    {
        // Comprueba si la conexión está abierta y hay mensaje en el inputField
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            if (!string.IsNullOrEmpty(inputField.text))
            {
                // Envía mensaje al servidor
                ws.Send(inputField.text);

                // Limpiar input y mantener el foco
                inputField.text = "";
                inputField.ActivateInputField();
            }
        }
        else
        {
            Debug.LogError("No se puede enviar el mensaje. La conexión no está abierta.");
        }
    }

    // Avísa al servidor si el usuario está escribiendo
    private void SendWritingToServer(string message)
    {
        // Comprueba si hay texto en el inputField para avisar si está escribiendo
        if (string.IsNullOrEmpty(message))
        {
            ws.Send("writing:no");
        }
        else
        {
            ws.Send("writing:yes");
        }
    }

    // Hacer que el Scroll se desplace hasta el final
    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // Pone en cola la acción pasada por parámetro
    private void EnqueueUIAction(Action action) {
        lock (_actionToRun) {
            _actionToRun.Enqueue(action);
        }
    }
}
