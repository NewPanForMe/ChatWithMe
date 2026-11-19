<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { request, describeError } from '../api/http'
import { notifySuccess, notifyError, notifyInfo } from '../utils/notify'
import type { PagedResult } from '../api/types'

interface NotificationDto { id: string; title: string; content: string; createdAtUtc: string }
const title = ref('')
const content = ref('')
const sending = ref(false)
const createVisible = ref(false)
const loading = ref(false)
let loadVersion = 0
const rows = ref<NotificationDto[]>([])
const total = ref(0)
const page = ref(1)
const detail = ref<NotificationDto | null>(null)
const detailVisible = ref(false)
const columns = [
  { colKey: 'title', title: '标题', ellipsis: true },
  { colKey: 'createdAtUtc', title: '发送时间', width: 200 },
  { colKey: 'operation', title: '操作', width: 100 },
]

async function load(showSuccess = false): Promise<void> {
  const version = ++loadVersion
  loading.value = true
  try {
    const data = await request<PagedResult<NotificationDto>>(`/api/admin/notifications?page=${page.value}&pageSize=20`)
    if (version !== loadVersion) return
    rows.value = data.items
    total.value = data.total
    if (showSuccess) notifySuccess('刷新成功')
  } catch (error) { if (version === loadVersion) notifyError('加载通知失败', describeError(error)) }
  finally { if (version === loadVersion) loading.value = false }
}

function openCreate(): void {
  title.value = ''
  content.value = ''
  createVisible.value = true
}

function closeCreate(): void {
  if (!sending.value) createVisible.value = false
}

async function send(): Promise<void> {
  if (sending.value) return
  if (!title.value.trim() || !content.value.trim()) {
    notifyError('请填写通知标题和正文')
    return
  }
  sending.value = true
  try {
    const result = await request<{ notification: NotificationDto; realtimeDelivered: boolean }>('/api/admin/notifications', {
      method: 'POST', body: JSON.stringify({ title: title.value.trim(), content: content.value.trim() }),
    })
    createVisible.value = false
    title.value = ''
    content.value = ''
    if (result.realtimeDelivered) notifySuccess('通知已保存并广播', '在线客户端实时接收，离线客户端登录后可查看。')
    else notifyInfo('通知已保存，实时广播失败', '客户端仍可刷新通知列表查看，请勿重复发送。')
    page.value = 1
    await load()
  } catch (error) { notifyError('发送通知失败', describeError(error)) }
  finally { sending.value = false }
}

function openDetail(row: NotificationDto): void { detail.value = row; detailVisible.value = true }
function changePage(current: number): void { page.value = current; void load() }
onMounted(() => { void load() })
</script>

<template>
  <div class="page">
    <div class="page-toolbar">
      <span>已发送通知（共 {{ total }} 条）</span>
      <div class="toolbar-right">
        <t-button theme="primary" @click="openCreate">添加通知</t-button>
        <t-button variant="outline" :loading="loading" @click="load(true)">刷新</t-button>
      </div>
    </div>
    <t-table row-key="id" :data="rows" :columns="columns" :loading="loading" table-layout="fixed">
      <template #operation="{ row }"><t-button variant="text" @click="openDetail(row)">查看详情</t-button></template>
    </t-table>
    <div class="page-footer">
      <t-pagination :current="page" :page-size="20" :total="total" :show-page-size="false" @current-change="changePage" />
    </div>
    <t-dialog
      :visible="createVisible"
      header="新增通知"
      placement="center"
      :confirm-btn="{ content: '发送通知', loading: sending }"
      :cancel-btn="{ content: '取消', disabled: sending }"
      :close-btn="!sending"
      :close-on-overlay-click="false"
      :close-on-esc-keydown="!sending"
      @confirm="send"
      @close="closeCreate"
    >
      <t-form label-align="top">
        <t-form-item label="通知标题">
          <t-input v-model="title" :maxlength="100" placeholder="请输入通知标题（最多 100 字）" :disabled="sending" />
        </t-form-item>
        <t-form-item label="通知正文">
          <t-textarea v-model="content" :maxlength="10000" placeholder="请输入通知正文（最多 10000 字）" :disabled="sending" />
        </t-form-item>
      </t-form>
      <p class="muted">通知将发送给全部客户端，在线实时推送，离线登录后可查看。</p>
    </t-dialog>
    <t-dialog v-model:visible="detailVisible" header="通知详情" placement="center" :footer="false">
      <h3>{{ detail?.title }}</h3>
      <p class="muted">{{ detail?.createdAtUtc }}</p>
      <p style="white-space: pre-wrap; overflow-wrap: anywhere">{{ detail?.content }}</p>
    </t-dialog>
  </div>
</template>
