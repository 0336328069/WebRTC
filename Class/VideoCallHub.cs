using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class VideoCallHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> ConnectedUsers = new(); // Lưu mapping userId với ConnectionId

    // Khi user kết nối, lưu vào danh sách
    public async Task RegisterUser(string userId)
    {
        ConnectedUsers[userId] = Context.ConnectionId;
        await Clients.Caller.SendAsync("UserRegistered", userId);
        Console.WriteLine($"User {userId} registered with connection {Context.ConnectionId}");
    }

    // Khi user gửi tín hiệu (offer/answer/ICE candidate)
    public async Task SendSignal(string targetUserId, string signal)
    {
        if (ConnectedUsers.TryGetValue(targetUserId, out string targetConnectionId))
        {
            Console.WriteLine($"Sending signal from {Context.ConnectionId} to {targetUserId}: {signal}");
            await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", Context.ConnectionId, signal);
        }
        else
        {
            Console.WriteLine($"Target user {targetUserId} is not connected.");
        }
    }
    public async Task EndCall(string callerId)
    {
        Console.WriteLine($"User {callerId} đã kết thúc cuộc gọi.");

        // Phát sự kiện CallEnded tới tất cả các client
        await Clients.All.SendAsync("ReceiveCallEnded", callerId);
    }
    public override async Task OnConnectedAsync()
    {
        // Lấy User ID (có thể dùng Context.UserIdentifier hoặc tự sinh ra một ID)
        var userId = Context.ConnectionId; // Hoặc Context.UserIdentifier nếu sử dụng auth
        var connectionId = Context.ConnectionId;

        // Thêm user vào ConnectedUsers
        if (!ConnectedUsers.TryAdd(userId, connectionId))
        {
            Console.WriteLine($"Failed to add user {userId} to ConnectedUsers.");
        }
        else
        {
            Console.WriteLine($"User connected: {userId} with ConnectionId: {connectionId}");
        }

        await base.OnConnectedAsync();
    }

    // Khi user ngắt kết nối
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var disconnectedUser = ConnectedUsers.FirstOrDefault(u => u.Value == Context.ConnectionId);
        if (!string.IsNullOrEmpty(disconnectedUser.Key))
        {
            ConnectedUsers.TryRemove(disconnectedUser.Key, out _);
            Console.WriteLine($"User {disconnectedUser.Key} disconnected.");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
