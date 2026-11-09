<script setup lang="ts">
import BootSplash from '@ocharomarketing/connect/BootSplash.vue';

import AppRoot from './AppRoot.vue';

/** Mounts the app beneath the first-load splash, which replaces the static twin in `index.html`, then lifts. */
defineOptions({ name: 'AppBoot' });
const base = import.meta.env.BASE_URL;
</script>

<template>
  <AppRoot />
  <BootSplash :base="base" />
</template>
