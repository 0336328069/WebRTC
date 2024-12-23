using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;

namespace WebRTC.Controllers
{
    [ApiController]
    [Route("ws")]
    public class WebSocketController : ControllerBase
    {
        private static readonly List<WebSocket> ConnectedClients = new();

        [HttpGet("connect")]
        public async Task Connect()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                ConnectedClients.Add(webSocket);
                await HandleWebSocketAsync(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received: {message}");

                    // Không thêm "Echo: ", chỉ gửi lại thông điệp gốc
                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
