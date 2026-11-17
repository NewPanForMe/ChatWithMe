<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { describeError, request } from '../api/http'
import type { OnlineUserDto } from '../api/types'
import { notifyError, notifySuccess } from '../utils/notify'
import { confirmAction } from '../utils/confirm'

const rows = ref<OnlineUserDto[]>([])
const loading = ref(false)

const columns = [
  { colKey: 'email', title: '邮箱', width: 280 },
  { colKey: 'nickName', title: '昵称', width: 160 },
  { colKey: 'connections', title: '在线连接数', width: 130 },
  { colKey: 'id', title: '用户 ID', ellipsis: true },
  { colKey: 'operation', title: '操作', width: 110 },
]

async function load(successTitle = ''): Promise<void> {
  loading.value = true

  try {
    rows.value = await request<OnlineUserDto[]>('/api/admin/users/online')
    if (successTitle) {
      notifySuccess(successTitle)
    }
  } catch (exception) {
    notifyError('刷新失败', describeError(exception))
    rows.value = []
  } finally {
    loading.value = false
  }
}

async function kick(row: OnlineUserDto): Promise<void> {
  try {
    const result = await request<{ sessionVersion: number; revokedRefreshTokens: number; closedConnections: number }>(
      `/api/admin/users/${row.id}/kick`,
      { method: 'POST' },
    )

    notifySuccess(
      `已强制下线 ${row.nickName}`,
      `会话版本 ${result.sessionVersion}，吊销刷新令牌 ${result.revokedRefreshTokens} 个，断开连接 ${result.closedConnections} 个`,
    )
    await load()
  } catch (exception) {
    notifyError('操作失败', describeError(exception))
  }
}

onMounted(() => {
  void load()
})
</script>

<template>
  <div class="page">
    <div class="page-toolbar">
      <div class="toolbar-left">
        <span class="muted">实时连接由 SignalR 维护，多端登录会计为多条连接。</span>
      </div>
      <div class="toolbar-right">
        <span class="muted">在线 {{ rows.length }} 人</span>
        <t-button variant="outline" :loading="loading" @click="load('刷新成功')">刷新</t-button>
      </div>
    </div>

    <t-table row-key="id" :data="rows" :columns="columns" :loading="loading" stripe hover table-layout="fixed">
      <template #connections="{ row }">
        <t-tag v-if="row.connections > 0" theme="success" variant="light">{{ row.connections }}</t-tag>
        <span v-else class="muted">0</span>
      </template>
      <template #id="{ row }">
        <span>{{ row.id }}</span>
      </template>
      <template #operation="{ row }">
        <t-button theme="danger" variant="text" size="small"
          @click="confirmAction('强制下线', `确定强制下线用户“${row.nickName}”（${row.email}）？其访问令牌与刷新令牌将立即失效。`, () => kick(row))">
          踢下线
        </t-button>
      </template>
    </t-table>

    <div class="page-footer">
      <span class="muted">踢人 = 会话版本自增 + 吊销全部刷新令牌 + 断开实时连接，旧访问令牌下一次请求即失效。</span>
    </div>
  </div>
</template>
