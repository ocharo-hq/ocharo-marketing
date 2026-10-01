<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, ref } from 'vue';
import { ArrowUpRight, Link2, RefreshCw, ShieldCheck, Sparkles } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import GenerationPanel from './GenerationPanel.vue';
import StatusIndicator from './StatusIndicator.vue';
import ConnectionReadiness from './ConnectionReadiness.vue';
const { t: ui } = useUiLocale();

/** Reports real server-side configuration without simulating account authorization. */
defineOptions({ name: 'ConnectionsPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const view = ref('channels');
/** Social destinations are shown independently from paid generation providers. */
const socials = computed(() => workspace.value?.connections.filter((item) => item.kind === 'social') ?? []);
/** Configured providers remain visibly distinct from verified external accounts. */
const providers = computed(() => workspace.value?.connections.filter((item) => item.kind !== 'social') ?? []);
/** Allows only documented secure external links. */
function documentation(url: string) {
  try {
    const result = new URL(url);
    return result.protocol === 'https:' ? result.href : null;
  } catch {
    return null;
  }
}
</script>

<template>
  <div class="space-y-7">
    <div class="flex flex-wrap items-start justify-between gap-5">
      <div>
        <p class="text-xs uppercase tracking-widest text-primary">{{ ui('Connections') }}</p>
        <h1 class="mt-2 font-editorial text-3xl">{{ ui('Every channel has its own approach.') }}</h1>
        <p class="mt-3 max-w-xl text-sm leading-7 text-muted-foreground">
          {{ ui('Configured service does not mean a verified connection. Secret keys are not entered on this page.') }}
        </p>
      </div>
      <Button variant="outline" :is-loading="desk.loading.value" @click="desk.refresh()"
        ><Icon :icon="RefreshCw" :size="15" class="mr-2" />{{ ui('Refresh status') }}</Button
      >
    </div>
    <div class="inline-flex flex-wrap gap-1 rounded-xl border border-border bg-card p-1">
      <button
        v-for="tab in [
          { id: 'channels', name: 'Kanallar', icon: Link2 },
          { id: 'generation', name: 'Foto tayyorlash', icon: Sparkles },
        ]"
        :key="tab.id"
        :class="[
          'flex items-center gap-2 rounded-lg px-4 py-2.5 text-xs',
          view === tab.id ? 'bg-primary text-primary-foreground' : 'text-muted-foreground',
        ]"
        :aria-pressed="view === tab.id"
        @click="view = tab.id"
      >
        <Icon :icon="tab.icon" :size="15" />{{ ui(tab.name) }}
      </button>
    </div>
    <GenerationPanel v-if="view === 'generation'" />
    <template v-else
      ><div class="grid gap-5 md:grid-cols-2">
        <article
          v-for="connection in socials"
          :key="connection.id"
          class="rounded-2xl border border-border bg-card p-6"
        >
          <div class="flex items-center justify-between gap-3">
            <div class="flex items-center gap-3">
              <div class="flex h-11 w-11 items-center justify-center rounded-xl bg-muted text-base font-semibold">
                {{ connection.name.slice(0, 1) }}
              </div>
              <h2 class="font-editorial text-xl">{{ connection.name }}</h2>
            </div>
            <StatusIndicator :status="connection.status" />
          </div>
          <ConnectionReadiness :connection="connection" />
          <p class="mt-5 text-xs leading-6 text-muted-foreground">{{ ui(connection.setupHint) }}</p>
          <p v-if="connection.capabilities.length" class="mt-3 text-[11px] leading-6 text-muted-foreground">
            {{ connection.capabilities.join(' · ') }}
          </p>
          <a
            v-if="documentation(connection.documentationUrl)"
            :href="documentation(connection.documentationUrl) ?? undefined"
            target="_blank"
            rel="noopener noreferrer"
            class="mt-5 inline-flex items-center gap-1 text-xs font-semibold text-primary"
            >{{ ui('Official setup guide') }} <Icon :icon="ArrowUpRight" :size="14"
          /></a>
        </article>
      </div>
      <section class="rounded-2xl border border-border bg-card p-6">
        <div class="flex items-center gap-3">
          <Icon :icon="Sparkles" :size="20" class="text-primary" />
          <h2 class="font-editorial text-2xl">{{ ui('Text and image services') }}</h2>
        </div>
        <div class="mt-5 divide-y divide-border">
          <article
            v-for="connection in providers"
            :key="connection.id"
            class="flex flex-wrap items-start justify-between gap-4 py-4"
          >
            <div class="max-w-2xl">
              <h3 class="text-sm font-semibold">{{ connection.name }}</h3>
              <p class="mt-2 text-xs leading-6 text-muted-foreground">{{ ui(connection.setupHint) }}</p>
              <a
                v-if="documentation(connection.documentationUrl)"
                :href="documentation(connection.documentationUrl) ?? undefined"
                target="_blank"
                rel="noopener noreferrer"
                class="mt-2 inline-flex items-center gap-1 text-xs text-primary"
                >{{ ui('Official documentation') }} <Icon :icon="ArrowUpRight" :size="13"
              /></a>
            </div>
            <StatusIndicator :status="connection.status" />
          </article>
        </div>
      </section>
      <div class="flex gap-3 rounded-xl border border-border p-5">
        <Icon :icon="ShieldCheck" :size="22" class="shrink-0 text-primary" />
        <div>
          <h3 class="text-sm font-semibold">{{ ui('Currently a local workspace') }}</h3>
          <p class="mt-2 text-xs leading-6 text-muted-foreground">
            {{
              ui(
                'For disconnected channels, download a ZIP package and publish manually. Actual delivery is configured separately and uses the approved post revision.',
              )
            }}
          </p>
        </div>
      </div>
    </template>
  </div>
</template>
