<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
const { t: ui } = useUiLocale();

/** Gives an explicit destination for unknown routes. */
defineOptions({ name: 'NotFoundPage' });
const desk = useMarketingWorkspace();
</script>
<template>
  <section class="rounded-2xl border border-border bg-card p-12 text-center">
    <p class="text-xs tracking-widest text-primary">404</p>
    <h1 class="mt-4 font-editorial text-3xl">{{ ui('This page was not found.') }}</h1>
    <p class="mt-4 text-sm text-muted-foreground">{{ ui('Return to the desk and choose a section.') }}</p>
    <Button class="mt-6" @click="desk.navigate('/')">{{ ui('Back to desk') }}</Button>
  </section>
</template>
