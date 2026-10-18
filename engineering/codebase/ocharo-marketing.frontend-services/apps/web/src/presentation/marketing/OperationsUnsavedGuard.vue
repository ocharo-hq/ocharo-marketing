<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed } from 'vue';
import { useNavigationBlocker } from '@wow-two-beta/fe/router';
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalTitle,
  ModalDescription,
  ModalFooter,
} from '@wow-two-beta/fe/presentation/overlays';
import { Button } from '@wow-two-beta/fe/presentation/actions';
const { t: ui } = useUiLocale();

const props = defineProps<{ dirty: boolean }>();
const blocker = useNavigationBlocker(computed(() => props.dirty));
</script>
<template>
  <Modal
    :open="blocker.state === 'blocked'"
    @update:open="
      (open) => {
        if (!open) blocker.reset();
      }
    "
  >
    <ModalContent class="max-w-md"
      ><ModalHeader
        ><ModalTitle>{{ ui('Unsaved changes') }}</ModalTitle>
        <ModalDescription>{{
          ui('Changes are unsaved. Stay on this page or leave without saving.')
        }}</ModalDescription></ModalHeader
      >
      <ModalFooter
        ><Button variant="outline" @click="blocker.reset">{{ ui('Continue editing') }}</Button>
        <Button @click="blocker.proceed">{{ ui('Leave without saving') }}</Button></ModalFooter
      >
    </ModalContent>
  </Modal>
</template>
