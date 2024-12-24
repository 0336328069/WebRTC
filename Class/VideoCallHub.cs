using Microsoft.AspNetCore.SignalR;

public class VideoCallHub : Hub
{
    public async Task SendSignal(string roomName, string user, string signal)
    {
        await Clients.Group(roomName).SendAsync("ReceiveSignal", user, signal);
    }

    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("UserLeft", Context.ConnectionId);
    }
}
