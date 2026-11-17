<script setup lang="ts">
import { ref } from 'vue'
import { describeError, request } from '../api/http'
import { notifyError, notifySuccess } from '../utils/notify'
import type { MessageDto, MessagePage } from '../api/types'

const conversationId = ref('')
const limit = ref(20)
const rows = ref<MessageDto[]>([])
const nextBefore = ref<string | null>(null)
const hasMore = ref(false)
const loading = ref(false)
const searched = ref(false)

const limitOptions = [
  { label: '10 条/页', value: 10 },
  { label: '20 条/页', value: 20 },
  { label: '50 条/页', value: 50 },
]

const columns = [
  { colKey: 'sentAtUtc', title: '发送时间（UTC+8）', width: 190 },
  { colKey: 'senderId', title: '发送者', width: 320, ellipsis: true },
  { colKey: 'content', title: '内容', ellipsis: true },
  { colKey: 'id', title: '消息 ID', width: 320, ellipsis: true },
]

async function fetchPage(reset: boolean): Promise<void> {
  if (!conversationId.value.trim()) {
    notifyError('无法查询消息', '请输入会话 ID（GUID）。')
    return
  }

  loading.value = true
  searched.value = true

  try {
    const query = new URLSearchParams({
      conversationId: conversationId.value.trim(),
      limit: String(limit.value),
    })

    if (!reset && nextBefore.value) {
      query.set('before', nextBefore.value)
    }

    const data = await request<MessagePage>(`/api/messages?${query.toString()}`)

    rows.value = reset ? data.items : [...rows.value, ...data.items]
    nextBefore.value = data.nextBefore
    hasMore.value = data.items.length >= limit.value && Boolean(data.nextBefore)
    notifySuccess(reset ? '查询成功' : '加载成功', `本次加载 ${data.items.length} 条消息。`)
  } catch (exception) {
    notifyError(reset ? '查询消息失败' : '加载消息失败', describeError(exception))
  } finally {
    loading.value = false
  }
}

function onSearch(): void {
  rows.value = []
  nextBefore.value = null
  hasMore.value = false
  void fetchPage(true)
}

function onLoadMore(): void {
  void fetchPage(false)
}
</script>

<template>
  <div class="page">
    <div class="page-toolbar">
      <div class="toolbar-left">
        <t-input
          v-model="conversationId"
          class="conversation-input"
          placeholder="会话 ID（GUID）"
          clearable
          @enter="onSearch"
        />
        <t-select v-model="limit" class="limit-select" :options="limitOptions" />
        <t-button theme="primary" :loading="loading" @click="onSearch">查询</t-button>
      </div>
      <div class="toolbar-right">
        <span class="muted">已加载 {{ rows.length }} 条</span>
      </div>
    </div>

    <t-table
      row-key="id"
      :data="rows"
      :columns="columns"
      :loading="loading"
      stripe
      hover
      table-layout="fixed"
    />

    <div class="page-footer messages-footer">
      <span v-if="!searched" class="muted">输入会话 ID 后查询，按时间倒序返回，使用 nextBefore 游标向前翻页。</span>
      <span v-else-if="!rows.length && !loading" class="muted">该会话暂无消息。</span>
      <template v-else>
        <t-button v-if="hasMore" variant="outline" :loading="loading" @click="onLoadMore">加载更多</t-button>
        <span v-else class="muted">已加载全部消息。</span>
      </template>
    </div>
  </div>
</template>
