<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { RouterLink } from 'vue-router';
const { t: ui } = useUiLocale();

/** Unknown route. */
defineOptions({ name: 'NotFoundPage' });
</script>

<template>
  <section class="panel p-12 text-center">
    <h1 class="font-editorial text-3xl">{{ ui('Page not found.') }}</h1>
    <RouterLink to="/" class="btn-primary mt-6">{{ ui('Back to stories') }}</RouterLink>
  </section>
</template>
