<script setup lang="ts">
import MarketingSelect from '@ocharomarketing/connect/MarketingSelect.vue';
import { requestFailureMessage } from '@ocharomarketing/connect/RequestFeedback';
import { useErrorToast } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { ref } from 'vue';
import { RouterLink } from 'vue-router';
import { useMarketingWorkspace } from '../../application/marketing';
import { integrationFetch } from '@ocharomarketing/connect';
import { Button } from '@wow-two-beta/fe/presentation/actions';
const { t: ui } = useUiLocale();

const desk = useMarketingWorkspace(),
  manifest = ref<File | null>(null),
  image = ref<File | null>(null),
  productId = ref(''),
  error = ref(''),
  draftId = ref(''),
  busy = ref(false);
useErrorToast(error);
function choose(event: Event, kind: 'manifest' | 'image') {
  const file = (event.target as HTMLInputElement).files?.[0] ?? null;
  if (kind === 'manifest') manifest.value = file;
  else image.value = file;
}
async function submit() {
  if (!manifest.value || !image.value || !productId.value) return;
  busy.value = true;
  error.value = '';
  try {
    const body = new FormData();
    body.append('manifest', manifest.value);
    body.append('image', image.value);
    body.append('productId', productId.value);
    const r = await integrationFetch('/api/marketing/creative-imports', { method: 'POST', body });
    if (!r.ok) {
      error.value = requestFailureMessage({ code: 'http', status: r.status });
      return;
    }
    const result = await r.json();
    draftId.value = result.draft.id;
    await desk.refresh();
  } catch {
    error.value = requestFailureMessage({ code: 'transport', status: 0 });
  } finally {
    busy.value = false;
  }
}
</script>
<template>
  <section class="max-w-3xl">
    <p class="text-xs uppercase tracking-widest text-muted-foreground">Studio → Marketing</p>
    <h1 class="font-editorial text-4xl mt-3">{{ ui('Bring a concept into review') }}</h1>
    <p class="my-5 text-muted-foreground">
      {{
        ui(
          'Import the PNG and review manifest explicitly exported from Studio. The result is a draft labelled creative visualization. Product facts, rights and publication need their own review.',
        )
      }}
    </p>
    <form class="grid gap-5 rounded-2xl border border-border bg-card p-5 sm:p-7" @submit.prevent="submit">
      <label class="block min-w-0 text-xs font-semibold"
        >{{ ui('Studio review JSON')
        }}<input
          class="mt-2 block min-h-11 w-full min-w-0 rounded-xl border border-border bg-background px-3 py-2.5 text-sm text-foreground outline-none focus:ring-2 focus:ring-ring file:mr-3 file:rounded-lg file:border-0 file:bg-muted file:px-3 file:py-2 file:text-xs file:font-semibold"
          type="file"
          accept=".json"
          required
          @change="choose($event, 'manifest')" /></label
      ><label class="block min-w-0 text-xs font-semibold"
        >{{ ui('Matching PNG')
        }}<input
          class="mt-2 block min-h-11 w-full min-w-0 rounded-xl border border-border bg-background px-3 py-2.5 text-sm text-foreground outline-none focus:ring-2 focus:ring-ring file:mr-3 file:rounded-lg file:border-0 file:bg-muted file:px-3 file:py-2 file:text-xs file:font-semibold"
          type="file"
          accept="image/png"
          required
          @change="choose($event, 'image')" /></label
      ><label class="block min-w-0 text-xs font-semibold"
        >{{ ui('Product context')
        }}<MarketingSelect
          v-model="productId"
          class="mt-2 block w-full min-w-0"
          required
          :placeholder="ui('Choose an existing piece')"
          :label="ui('Product context')"
          :options="
            (desk.workspace.value?.products ?? []).map((product) => ({ value: product.id, label: product.name }))
          "
          :disabled="busy" /></label
      ><Button type="submit" :is-disabled="busy">{{ busy ? ui('Checking package…') : ui('Import as a draft') }}</Button>
      <RouterLink v-if="draftId" :to="'/posts/' + draftId" class="text-primary underline">{{
        ui('Open imported draft →')
      }}</RouterLink>
    </form>
  </section>
</template>
