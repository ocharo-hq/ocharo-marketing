<script setup lang="ts">
import Button from './compatibility/ActionButton.vue';
import { useMarketingWorkspace } from '../../application/marketing';
/** Gives an explicit destination for unknown routes. */
defineOptions({ name: 'NotFoundPage' });
const desk = useMarketingWorkspace();
</script>
<template>
  <section class="rounded-2xl border border-border bg-card p-12 text-center">
    <p class="text-xs tracking-widest text-primary">404</p>
    <h1 class="mt-4 font-editorial text-3xl">Bu sahifa topilmadi.</h1>
    <p class="mt-4 text-sm text-muted-foreground">Ish stoliga qaytib, kerakli bo‘limni tanlang.</p>
    <Button class="mt-6" @click="desk.navigate('/')">Ish stoliga qaytish</Button>
  </section>
</template>
