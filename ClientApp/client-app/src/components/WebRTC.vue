<template>
  <div>
    <h1>Video Call with SignalR</h1>
    <div>
      <input v-model="roomName" placeholder="Enter Room Name" />
      <button @click="joinRoom" :disabled="isConnected">Join Room</button>
      <button @click="leaveRoom" :disabled="!isConnected">Leave Room</button>
    </div>
    <div>
      <h3>Local Video</h3>
      <video ref="localVideo" autoplay playsinline muted></video>
    </div>
    <div>
      <h3>Remote Video</h3>
      <video ref="remoteVideo" autoplay playsinline></video>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue';
import {
  createSignalRConnection,
  onSignalREvent,
  joinRoom as signalRJoinRoom,
  leaveRoom as signalRLeaveRoom,
  sendSignal as signalRSendSignal,
  stopSignalRConnection,
} from '@/utils/connection';

const roomName = ref('');
const isConnected = ref(false);
const localVideo = ref(null);
const remoteVideo = ref(null);

let peerConnection = null;
let localStream = null;
const iceConfig = { iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] };

// Thiết lập WebRTC kết nối
const setupPeerConnection = async () => {
  peerConnection = new RTCPeerConnection(iceConfig);

  peerConnection.onicecandidate = (event) => {
    if (event.candidate) {
      signalRSendSignal(roomName.value, connection.connectionId, JSON.stringify({ candidate: event.candidate }));
    }
  };

  peerConnection.ontrack = (event) => {
    if (!remoteVideo.value.srcObject) {
      remoteVideo.value.srcObject = new MediaStream();
    }
    remoteVideo.value.srcObject.addTrack(event.track);
  };

  localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
  localVideo.value.srcObject = localStream;
  localStream.getTracks().forEach((track) => peerConnection.addTrack(track, localStream));
};

// Tạo offer WebRTC
const createOffer = async (userId) => {
  const offer = await peerConnection.createOffer();
  await peerConnection.setLocalDescription(offer);

  signalRSendSignal(roomName.value, userId, JSON.stringify({ type: 'offer', sdp: offer.sdp }));
};

// Xử lý offer từ người khác
const handleOffer = async (userId, offer) => {
  await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
  const answer = await peerConnection.createAnswer();
  await peerConnection.setLocalDescription(answer);

  signalRSendSignal(roomName.value, userId, JSON.stringify({ type: 'answer', sdp: answer.sdp }));
};

// Xử lý sự kiện SignalR
const handleSignalREvents = () => {
  onSignalREvent('UserJoined', async (userId) => {
    console.log(`${userId} joined`);
    if (userId !== connection.connectionId) {
      await createOffer(userId);
    }
  });

  onSignalREvent('ReceiveSignal', async (userId, signal) => {
    const data = JSON.parse(signal);

    if (data.type === 'offer') {
      await handleOffer(userId, data);
    } else if (data.type === 'answer') {
      await peerConnection.setRemoteDescription(new RTCSessionDescription(data));
    } else if (data.candidate) {
      await peerConnection.addIceCandidate(new RTCIceCandidate(data));
    }
  });

  onSignalREvent('UserLeft', (userId) => {
    console.log(`${userId} left`);
  });
};

// Tham gia phòng
const joinRoom = async () => {
  await createSignalRConnection('https://localhost:7229/videocall');
  await setupPeerConnection();
  handleSignalREvents();

  await signalRJoinRoom(roomName.value);
  isConnected.value = true;
};

// Rời phòng
const leaveRoom = async () => {
  await signalRLeaveRoom(roomName.value);
  isConnected.value = false;

  if (peerConnection) {
    peerConnection.close();
    peerConnection = null;
  }
};

// Lifecycle hooks
onMounted(async () => {
  console.log('Component mounted.');
});

onBeforeUnmount(() => {
  stopSignalRConnection();
  if (peerConnection) {
    peerConnection.close();
  }
});
</script>

<style scoped>
video {
  width: 300px;
  height: 200px;
  background: #ddd;
  margin: 10px;
}

button {
  margin: 10px;
  padding: 10px 20px;
  background-color: #4caf50;
  color: white;
  border: none;
  cursor: pointer;
}

button:disabled {
  background-color: #9e9e9e;
  cursor: not-allowed;
}
</style>
