using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class BasicWebSocketClient : MonoBehaviour
{
    private static Queue<Action> _actionToRun = new Queue<Action>(); // Queue con las acciones a realizar en la GUI

    public TMP_Text chatDisplay;  // Texto donde se muestra el historial del chat
    public TMP_InputField inputField; // Input donde el usuario escribe
    public Button sendButton; // Botón para enviar mensajes
    public ScrollRect scrollRect; // Scroll View para manejar el desplazamiento
    public BasicWebSocketServer server; // Servidor del chat

    // Instancia del cliente WebSocket
    private WebSocket ws;

    // Se ejecuta al iniciar la escena
    void Start()
    {
        TryToConnect();

        sendButton.onClick.AddListener(SendMessageToServer);
        inputField.onSubmit.AddListener(delegate { SendMessageToServer(); });
        
        //Limpiar el chatDisplay
        chatDisplay.text = "";

        inputField.Select();
        inputField.ActivateInputField();
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
            EnqueueUIAction(() => ShowMessageInChat(e.Data)); // Recoge el mensaje y lo imprime en el chat
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
                EnqueueUIAction(() => {
                    ShowMessageInChat("Host desconectado. Reconectando...");
                    Debug.Log("Activando servidor...");
                    server.TryToConnect();
                    TryToConnect();
                });
            }
            Debug.Log("WebSocket cerrado. Código: " + e.Code + ", Razón: " + e.Reason);
        };

        // Conectar de forma asíncrona al servidor WebSocket
        ws.ConnectAsync();
    }

    void Update() {
        if (_actionToRun.Count > 0)
        {
            Action action;

            lock (_actionToRun)
            {
                action = _actionToRun.Dequeue();
            }

            action?.Invoke();
        }
    }

    private void ShowMessageInChat(String message)
    {
        // Agregar el mensaje al historial del chat
        chatDisplay.text += "\n" + message;

        // Forzar actualización del Layout para el Scroll
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatDisplay.rectTransform);

        // Hacer que el Scroll se desplace hasta el final
        ScrollToBottom();
    }

    // Método para enviar un mensaje al servidor (puedes llamarlo, por ejemplo, desde un botón en la UI)
    public void SendMessageToServer()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            if (!string.IsNullOrEmpty(inputField.text))
            {
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

    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cambiar de escena o cerrar la aplicación)
    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void EnqueueUIAction(Action action) {
        lock (_actionToRun) {
            _actionToRun.Enqueue(action);
        }
    }
}
