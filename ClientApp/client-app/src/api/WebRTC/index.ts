let socket: WebSocket | null = null

export const connectWebSocket = (
  onMessageCallback: (message: string) => void,
  onErrorCallback: (error: Event) => void,
  onCloseCallback: (event: CloseEvent) => void,
) => {
  const websocketUrl = 'wss://localhost:44387/ws/connect'

  socket = new WebSocket(websocketUrl)

  socket.onopen = () => {
    console.log('WebSocket connection established.')
  }

  socket.onmessage = (event: MessageEvent) => {
    console.log('Message received from server:', event.data)
    onMessageCallback(event.data)
  }

  socket.onerror = (error: Event) => {
    console.error('WebSocket error:', error)
    onErrorCallback(error)
  }

  socket.onclose = (event: CloseEvent) => {
    console.log('WebSocket connection closed:', event.reason)
    onCloseCallback(event)
  }
}

export const sendMessage = (message: string) => {
  if (socket && socket.readyState === WebSocket.OPEN) {
    socket.send(message)
    console.log('Message sent:', message)
  } else {
    console.error('WebSocket is not connected.')
  }
}

export const closeWebSocket = () => {
  if (socket) {
    socket.close()
    console.log('WebSocket connection closed.')
  }
}
