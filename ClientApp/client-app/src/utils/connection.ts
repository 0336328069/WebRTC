import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

let connection: any = null

// Tạo kết nối SignalR
export async function createSignalRConnection(hubUrl: string) {
  if (connection) return connection

  connection = new HubConnectionBuilder()
    .configureLogging(LogLevel.Information)
    .withUrl(hubUrl)
    .withAutomaticReconnect()
    .build()

  try {
    await connection.start()
    console.log('SignalR connection established.')
  } catch (error) {
    console.error('SignalR connection error: ', error)
    throw error
  }

  return connection
}

// Đăng ký sự kiện từ server (dùng chung)
export function onSignalREvent(eventName: string, callback: (...args: any[]) => void) {
  if (!connection) {
    throw new Error('SignalR connection has not been initialized.')
  }
  connection.on(eventName, callback)
}

// Tham gia phòng
export async function joinRoom(roomName: string) {
  if (!connection) {
    throw new Error('SignalR connection has not been initialized.')
  }
  await connection.invoke('JoinRoom', roomName)
  console.log(`Joined room: ${roomName}`)
}

// Rời phòng
export async function leaveRoom(roomName: string) {
  if (!connection) {
    throw new Error('SignalR connection has not been initialized.')
  }
  await connection.invoke('LeaveRoom', roomName)
  console.log(`Left room: ${roomName}`)
}

// Gửi tín hiệu WebRTC
export async function sendSignal(roomName: string, userId: string, signal: string) {
  if (!connection) {
    throw new Error('SignalR connection has not been initialized.')
  }
  await connection.invoke('SendSignal', roomName, userId, signal)
  console.log(`Signal sent to room ${roomName} for user ${userId}`)
}

// Hủy kết nối SignalR
export async function stopSignalRConnection() {
  if (connection) {
    await connection.stop()
    console.log('SignalR connection stopped.')
    connection = null
  }
}
