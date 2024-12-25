<script setup lang="ts">
import { ref, onMounted } from "vue";
import { createSignalRConnection, sendSignal, onSignalREvent, endCallConnection } from "@/utils/connection";

// Biến chính
const localStream = ref<MediaStream | null>(null);
const remoteStream = ref<MediaStream | null>(new MediaStream());
const peerConnection = ref<RTCPeerConnection | null>(null);
const localVideo = ref<HTMLVideoElement | null>(null);
const remoteVideo = ref<HTMLVideoElement | null>(null);
const remoteUserId = ref("");
const localUserId = ref("");
const inCall = ref(false);
const isReady = ref(false);
const isCallEnded = ref(false);
const iceConfig = {
  iceServers: [
    {
      urls: "turn:relay1.expressturn.com:3478",
      username: "ef0XOO2TKNCM6Y4RBZ",
      credential: "toLMiaha1hYZFJiB",
    },
  ],
};

// Thiết lập luồng video local
const setupLocalStream = async () => {
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
    localStream.value = stream;
    if (localVideo.value) {
      localVideo.value.srcObject = stream;
    }
  } catch (error) {
    console.error("Không thể truy cập camera/microphone:", error);
  }
};

const endCall = async () => {
  if (isCallEnded.value) return; // Nếu đã kết thúc thì không làm gì nữa

  isCallEnded.value = true; // Đánh dấu đã kết thúc

  // Gửi tín hiệu EndCall tới server
  try {
    if (localUserId.value) {
      await endCallConnection(localUserId.value);
    }
  } catch (error) {
    console.error("Không thể gửi tín hiệu kết thúc:", error);
  }

  // Dọn dẹp local stream và UI
  cleanupCall();
};

const cleanupCall = () => {
  if (peerConnection.value) {
    peerConnection.value.close();
    peerConnection.value = null;
  }
  if (remoteStream.value) {
    remoteStream.value.getTracks().forEach((track) => track.stop());
    remoteStream.value = null;
    if (remoteVideo.value) {
      remoteVideo.value.srcObject = null;
    }
  }

  inCall.value = false;
};
// Tạo kết nối WebRTC PeerConnection
const createPeerConnection = () => {
  const pc = new RTCPeerConnection(iceConfig);

  pc.onicecandidate = (event) => {
    if (event.candidate) {
      sendSignal(remoteUserId.value, JSON.stringify({ candidate: event.candidate }));
    }
  };

  pc.ontrack = (event) => {
    if (remoteStream.value) {
      remoteStream.value.addTrack(event.track);
      if (remoteVideo.value) {
        remoteVideo.value.srcObject = remoteStream.value;
      }
    }
  };

  if (localStream.value) {
    localStream.value.getTracks().forEach((track) => pc.addTrack(track, localStream.value!));
  }

  return pc;
};

// Gọi người dùng khác
const callUser = async () => {
  if (!remoteUserId.value) {
    alert("Vui lòng nhập User ID để gọi.");
    return;
  }

  // Thiết lập local stream nếu chưa được thiết lập
  if (!localStream.value) {
    await setupLocalStream();
  }

  peerConnection.value = createPeerConnection();
  const offer = await peerConnection.value.createOffer();
  await peerConnection.value.setLocalDescription(offer);

  sendSignal(remoteUserId.value, JSON.stringify({ type: "offer", sdp: offer.sdp }));
  inCall.value = true;
};

// Xử lý khi nhận được offer
const handleOffer = async (sdp: string, fromUserId: string) => {
  const isAccepted = window.confirm(`Người dùng ${fromUserId} muốn gọi cho bạn. Chấp nhận cuộc gọi?`);

  if (isAccepted) {
    peerConnection.value = createPeerConnection();
    await peerConnection.value.setRemoteDescription(new RTCSessionDescription({ type: "offer", sdp }));
    const answer = await peerConnection.value.createAnswer();
    await peerConnection.value.setLocalDescription(answer);

    sendSignal(fromUserId, JSON.stringify({ type: "answer", sdp: answer.sdp }));
    inCall.value = true;
  } else {
    console.log(`Người dùng ${fromUserId} đã từ chối cuộc gọi.`);
    sendSignal(fromUserId, JSON.stringify({ type: "reject" }));
  }
};

// Xử lý khi nhận được answer
const handleAnswer = async (sdp: string) => {
  if (peerConnection.value) {
    await peerConnection.value.setRemoteDescription(new RTCSessionDescription({ type: "answer", sdp }));
  }
};

// Xử lý khi nhận được ICE Candidate
const handleCandidate = async (candidate: RTCIceCandidateInit) => {
  if (peerConnection.value) {
    await peerConnection.value.addIceCandidate(new RTCIceCandidate(candidate));
  }
};
// Thiết lập kết nối SignalR
const setupSignalR = async () => {
  try {
    const connection = await createSignalRConnection("https://localhost:7229/videocall");
    localUserId.value = connection.connectionId;
    console.log(`SignalR connected with ID: ${localUserId.value}`); // Log ID của người dùng local

    // Đăng ký sự kiện ReceiveSignal
    onSignalREvent("ReceiveSignal", (fromUserId, data) => {
      const message = JSON.parse(data);
      console.log(message.type);
      if (message.type === "offer") {
        handleOffer(message.sdp, fromUserId);
      } else if (message.type === "answer") {
        handleAnswer(message.sdp);
      } else if (message.type === "reject") {
        alert(`Người dùng ${fromUserId} đã từ chối cuộc gọi.`);
        inCall.value = false;
      } else if (message.candidate) {
        handleCandidate(message.candidate);
      }
    });

    onSignalREvent("ReceiveCallEnded", (callerId) => {
      if (callerId === localUserId.value) {
        console.log("Tín hiệu CallEnded từ chính user hiện tại, bỏ qua.");
        return;
      }

      if (!isCallEnded.value) {
        isCallEnded.value = true;
        cleanupCall();
      }
    });

    isReady.value = true;
  } catch (error) {
    console.error("SignalR connection error:", error);
  }
};

// Khi component được mount
onMounted(() => {
  setupLocalStream();
  setupSignalR();
});
</script>

<template>
  <div>
    <h1>Video Call</h1>
    <h2>User ID của bạn: {{ localUserId }}</h2>
    <div>
      <input v-model="remoteUserId" placeholder="Nhập User ID để gọi" />
      <button @click="callUser" :disabled="!isReady || inCall">Gọi</button>
      <button @click="endCall" :disabled="!inCall">Kết thúc</button>
    </div>
    <div>
      <h3>Video của bạn</h3>
      <video ref="localVideo" autoplay playsinline muted></video>
    </div>
    <div>
      <h3>Video từ người khác</h3>
      <video ref="remoteVideo" autoplay playsinline></video>
    </div>
  </div>
</template>

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
