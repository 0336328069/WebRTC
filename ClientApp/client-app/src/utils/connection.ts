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

// Đăng ký user local với server
export async function registerUser(userId: string) {
  if (!connection) {
    throw new Error('SignalR connection has not been initialized.')
  }
  await connection.invoke('RegisterUser', userId)
  console.log(`User registered: ${userId}`)
}

// Gửi tín hiệu WebRTC
export async function sendSignal(targetUserId: string, signal: string) {
  if (!connection) {
    throw new Error('SignalR connection has not been initialized.')
  }
  await connection.invoke('SendSignal', targetUserId, signal)
}

// Nhận tín hiệu từ server
export function onSignalREvent(eventName: string, callback: (...args: any[]) => void) {
  if (!connection) {
    throw new Error('SignalR connection has not been initialized.')
  }
  connection.on(eventName, callback)
}

// Hủy kết nối SignalR
export async function stopSignalRConnection() {
  if (connection) {
    await connection.stop()
    console.log('SignalR connection stopped.')
    connection = null
  }
}

export const endCallConnection = async (localUserId: string) => {
  if (connection) {
    await connection.invoke('EndCall', localUserId)
  }
}
