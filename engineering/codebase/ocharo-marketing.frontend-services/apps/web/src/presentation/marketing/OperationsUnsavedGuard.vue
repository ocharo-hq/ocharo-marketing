<script setup lang="ts">
import { computed } from 'vue';
import { useNavigationBlocker } from '@wow-two-beta/ui-vue/router';
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalTitle,
  ModalDescription,
  ModalFooter,
} from '@wow-two-beta/ui-vue/presentation/overlays';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
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
        ><ModalTitle>Saqlanmagan o‘zgarishlar</ModalTitle>
        <ModalDescription>Tahrir saqlanmagan. Sahifada qoling yoki saqlamasdan chiqing.</ModalDescription></ModalHeader
      >
      <ModalFooter
        ><Button variant="outline" @click="blocker.reset">Tahrirlashni davom ettirish</Button>
        <Button @click="blocker.proceed">Saqlamasdan chiqish</Button></ModalFooter
      >
    </ModalContent>
  </Modal>
</template>
