<template>
  <div>
    <h1>Video Call with WebRTC</h1>
    <div class="videos">
      <video ref="localVideo" autoplay playsinline muted></video>
      <video ref="remoteVideo" autoplay playsinline></video>
    </div>
    <button @click="startCall">Start Call</button>
    <button @click="endCall">End Call</button>
    <div v-if="incomingCall">
      <p>Incoming call...</p>
      <button @click="acceptCall">Accept</button>
      <button @click="rejectCall">Reject</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { connectWebSocket, closeWebSocket, sendMessage } from "@/api/WebRTC";
import { ref, onMounted, onBeforeUnmount } from "vue";

const localVideo = ref<HTMLVideoElement | null>(null);
const remoteVideo = ref<HTMLVideoElement | null>(null);

let localStream: MediaStream | null = null;
let peerConnection: RTCPeerConnection | null = null;

const incomingCall = ref(false); // Trạng thái cuộc gọi đến
let offerData: RTCSessionDescriptionInit | null = null; // Dữ liệu offer từ người gọi

// ICE server configuration
const iceConfig = {
  iceServers: [{ urls: "stun:stun.l.google.com:19302" }],
};

// Hàm xử lý tín hiệu WebSocket
const handleSignalingMessage = async (message: string) => {
  const data = JSON.parse(message);

  if (data.type === "offer") {
    // Khi nhận được offer, hiển thị thông báo cuộc gọi đến
    incomingCall.value = true;
    offerData = data;
  }

  if (data.type === "answer" && peerConnection) {
    await peerConnection.setRemoteDescription(new RTCSessionDescription(data));
  }

  if (data.type === "candidate" && peerConnection) {
    await peerConnection.addIceCandidate(new RTCIceCandidate(data.candidate));
  }
};

// Hàm bắt đầu cuộc gọi
const startCall = async () => {
  localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
  if (localVideo.value) {
    localVideo.value.srcObject = localStream;
  }

  peerConnection = new RTCPeerConnection(iceConfig);

  localStream.getTracks().forEach((track) => peerConnection?.addTrack(track, localStream!));

  peerConnection.ontrack = (event) => {
    if (remoteVideo.value) {
      remoteVideo.value.srcObject = event.streams[0];
    }
  };

  peerConnection.onicecandidate = (event) => {
    if (event.candidate) {
      sendMessage(JSON.stringify({ type: "candidate", candidate: event.candidate }));
    }
  };

  const offer = await peerConnection.createOffer();
  await peerConnection.setLocalDescription(offer);
  sendMessage(JSON.stringify(offer));
};

// Hàm kết thúc cuộc gọi
const endCall = () => {
  peerConnection?.close();
  peerConnection = null;
  if (localStream) {
    localStream.getTracks().forEach((track) => track.stop());
    localStream = null;
  }
  if (localVideo.value) localVideo.value.srcObject = null;
  if (remoteVideo.value) remoteVideo.value.srcObject = null;
};

// Hàm chấp nhận cuộc gọi
const acceptCall = async () => {
  incomingCall.value = false; // Ẩn thông báo

  localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
  if (localVideo.value) {
    localVideo.value.srcObject = localStream;
  }

  peerConnection = new RTCPeerConnection(iceConfig);

  localStream.getTracks().forEach((track) => peerConnection?.addTrack(track, localStream!));

  peerConnection.ontrack = (event) => {
    if (remoteVideo.value) {
      remoteVideo.value.srcObject = event.streams[0];
    }
  };

  peerConnection.onicecandidate = (event) => {
    if (event.candidate) {
      sendMessage(JSON.stringify({ type: "candidate", candidate: event.candidate }));
    }
  };

  await peerConnection.setRemoteDescription(new RTCSessionDescription(offerData!));
  const answer = await peerConnection.createAnswer();
  await peerConnection.setLocalDescription(answer);
  sendMessage(JSON.stringify(peerConnection.localDescription));
};

// Hàm từ chối cuộc gọi
const rejectCall = () => {
  incomingCall.value = false;
};

// Khởi tạo WebSocket
onMounted(() => {
  connectWebSocket(handleSignalingMessage, console.error, () => console.log("WebSocket closed"));
});

onBeforeUnmount(() => {
  closeWebSocket();
});
</script>

<style scoped>
.videos {
  display: flex;
  gap: 10px;
}

video {
  width: 300px;
  height: 200px;
  border: 1px solid #ccc;
}

button {
  margin: 10px;
  padding: 10px 20px;
  background-color: #4caf50;
  color: white;
  border: none;
  cursor: pointer;
}

button:hover {
  background-color: #45a049;
}
</style>
