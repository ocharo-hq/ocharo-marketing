<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import PostComposerPage from '../presentation/marketing/PostComposerPage.vue';
/** Translates router-owned parameters into typed page inputs. */
defineOptions({ name: 'PostComposerRoute' });
const route = useRoute();
/** A string parameter excludes repeated query values. */
const draftId = computed(() => (typeof route.params.id === 'string' ? route.params.id : undefined));
/** The optional source product arrives from the atlas or catalogue. */
const productId = computed(() => (typeof route.query.product === 'string' ? route.query.product : undefined));
/** The optional preset is a selected atlas composition. */
const presetId = computed(() => (typeof route.query.preset === 'string' ? route.query.preset : undefined));
</script>
<template>
  <PostComposerPage :key="route.fullPath" :draft-id="draftId" :product-id="productId" :preset-id="presetId" />
</template>
