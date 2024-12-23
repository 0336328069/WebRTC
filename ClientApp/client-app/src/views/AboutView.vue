<template>
  <div class="about">
    <WebRTC />
  </div>
</template>
<script setup lang="ts">
import WebRTC from '@/components/WebRTC.vue';
</script>
<style>
@media (min-width: 1024px) {
  .about {
    min-height: 100vh;
    display: flex;
    align-items: center;
  }
}
</style>
