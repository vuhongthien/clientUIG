using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WebSocketSharp;

public class StompClient
{
    private WebSocket ws;
    private Dictionary<string, Action<string>> subscriptions = new Dictionary<string, Action<string>>();
    private Action onConnectedCallback;
    
    public void Connect(string url, Action onConnected = null)
    {
        this.onConnectedCallback = onConnected;
        ws = new WebSocket(url);
        
        ws.OnOpen += (sender, e) =>
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                Debug.Log("[STOMP] WebSocket opened");
                SendStompConnect();
            });
        };
        
        ws.OnMessage += (sender, e) =>
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    Debug.Log($"[STOMP] Raw message: {e.Data}");
                    HandleStompMessage(e.Data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[STOMP] Error handling message: {ex.Message}\nStackTrace: {ex.StackTrace}");
                }
            });
        };
        
        ws.OnError += (sender, e) =>
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                Debug.LogError($"[STOMP] WebSocket Error: {e.Message}");
                if (e.Exception != null)
                {
                    Debug.LogError($"[STOMP] Exception: {e.Exception}");
                }
            });
        };
        
        ws.OnClose += (sender, e) =>
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                Debug.Log($"[STOMP] Disconnected. Code: {e.Code}, Reason: {e.Reason}");
            });
        };
        
        ws.Connect();
    }
    
    private void SendStompConnect()
    {
        // ✅ FIX: Đảm bảo có dòng trống giữa header và body
        string frame = "CONNECT\n" +
                      "accept-version:1.1,1.2\n" +
                      "heart-beat:0,0\n" +
                      "\n" +  // ✅ CRITICAL: Dòng trống
                      "\0";
        ws.Send(frame);
        Debug.Log("[STOMP] Sent CONNECT frame");
    }
    
    private void HandleStompMessage(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            Debug.LogWarning("[STOMP] Received empty message");
            return;
        }
        
        string preview = data.Length > 50 ? data.Substring(0, 50) + "..." : data;
        Debug.Log($"[STOMP] Message preview: {preview}");
        
        if (data.StartsWith("CONNECTED"))
        {
            Debug.Log("[STOMP] Connected to server!");
            onConnectedCallback?.Invoke();
        }
        else if (data.StartsWith("MESSAGE"))
        {
            ParseMessage(data);
        }
        else if (data.StartsWith("ERROR"))
        {
            ParseErrorMessage(data);
        }
        else
        {
            Debug.LogWarning($"[STOMP] Unknown frame type: {data.Split('\n')[0]}");
        }
    }
    
    /// <summary>
    /// ✅ Parse error messages properly
    /// </summary>
    private void ParseErrorMessage(string frame)
    {
        try
        {
            string[] lines = frame.Split('\n');
            string errorMessage = "Unknown error";
            
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                
                if (string.IsNullOrEmpty(line))
                {
                    if (i + 1 < lines.Length)
                    {
                        errorMessage = string.Join("\n", lines, i + 1, lines.Length - i - 1);
                        errorMessage = errorMessage.TrimEnd('\0', '\n', '\r');
                    }
                    break;
                }
                
                if (line.StartsWith("message:"))
                {
                    errorMessage = line.Substring(8).Trim();
                }
            }
            
            Debug.LogError($"[STOMP] Server error: {errorMessage}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[STOMP] Error parsing error message: {ex.Message}");
        }
    }
    
    private void ParseMessage(string frame)
    {
        try
        {
            string[] lines = frame.Split('\n');
            string destination = "";
            int headerEndIndex = -1;
            
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                
                if (string.IsNullOrEmpty(line))
                {
                    headerEndIndex = i;
                    break;
                }
                
                if (line.StartsWith("destination:"))
                {
                    destination = line.Substring(12).Trim();
                }
            }
            
            if (headerEndIndex == -1)
            {
                Debug.LogError("[STOMP] Invalid MESSAGE frame: no header end found");
                return;
            }
            
            StringBuilder bodyBuilder = new StringBuilder();
            for (int i = headerEndIndex + 1; i < lines.Length; i++)
            {
                if (i > headerEndIndex + 1)
                {
                    bodyBuilder.Append('\n');
                }
                bodyBuilder.Append(lines[i]);
            }
            
            string body = bodyBuilder.ToString().TrimEnd('\0', '\n', '\r');
            
            Debug.Log($"[STOMP] Parsed message - Destination: {destination}, Body: {body}");
            
            if (subscriptions.ContainsKey(destination))
            {
                subscriptions[destination]?.Invoke(body);
            }
            else
            {
                Debug.LogWarning($"[STOMP] No subscription found for destination: {destination}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[STOMP] Error parsing message: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    public void Subscribe(string destination, Action<string> callback)
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            Debug.LogError("[STOMP] Cannot subscribe: WebSocket not connected");
            return;
        }
        
        subscriptions[destination] = callback;
        
        string id = $"sub-{subscriptions.Count}";
        
        // ✅ FIX: Đảm bảo có dòng trống
        string frame = "SUBSCRIBE\n" +
                      $"id:{id}\n" +
                      $"destination:{destination}\n" +
                      "ack:auto\n" +
                      "\n" +  // ✅ CRITICAL
                      "\0";
        
        ws.Send(frame);
        Debug.Log($"[STOMP] Subscribed to {destination} with id {id}");
    }
    
    public void Send(string destination, string body)
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            Debug.LogError("[STOMP] Cannot send: WebSocket not connected");
            return;
        }
        
        // ✅ FIX CRITICAL: Tính content-length bằng BYTES
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        int contentLength = bodyBytes.Length;
        
        string frame = "SEND\n" +
                      $"destination:{destination}\n" +
                      "content-type:application/json\n" +
                      $"content-length:{contentLength}\n" +
                      "\n" +  // ✅ Dòng trống
                      $"{body}\0";
        
        ws.Send(frame);
        Debug.Log($"[STOMP] Sent to {destination} (length: {contentLength} bytes): {body}");
    }
    
    public void Disconnect()
    {
        if (ws != null)
        {
            try
            {
                string frame = "DISCONNECT\n" +
                              "\n" +
                              "\0";
                ws.Send(frame);
                ws.Close();
                Debug.Log("[STOMP] Disconnected");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[STOMP] Error disconnecting: {ex.Message}");
            }
        }
    }
    
    public bool IsConnected()
    {
        return ws != null && ws.ReadyState == WebSocketState.Open;
    }
}