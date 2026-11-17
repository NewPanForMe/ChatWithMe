<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { describeError, request } from '../api/http'
import { notifyError, notifySuccess } from '../utils/notify'
import { confirmAction } from '../utils/confirm'
import type { PagedResult, UserDto } from '../api/types'

const rows = ref<UserDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const keyword = ref('')
const loading = ref(false)

const columns = [
  { colKey: 'email', title: '邮箱', width: 320 },
  { colKey: 'nickName', title: '昵称', width: 140 },
  { colKey: 'createdAtUtc', title: '创建时间（UTC+8）', width: 190 },
  { colKey: 'id', title: '用户 ID', ellipsis: true },
  { colKey: 'operation', title: '操作', width: 100 },
]

async function remove(row: UserDto): Promise<void> {
  try {
    await request<void>(`/api/users/${row.id}`, { method: 'DELETE' })
    notifySuccess('删除用户成功')
    await load()
  } catch (exception) {
    notifyError('删除用户失败', describeError(exception))
  }
}

async function load(successTitle = ''): Promise<void> {
  loading.value = true

  try {
    const query = new URLSearchParams({ page: String(page.value), pageSize: String(pageSize.value) })

    if (keyword.value.trim()) {
      query.set('keyword', keyword.value.trim())
    }

    const data = await request<PagedResult<UserDto>>(`/api/users?${query.toString()}`)
    rows.value = data.items
    total.value = data.total
    if (successTitle) {
      notifySuccess(successTitle)
    }
  } catch (exception) {
    notifyError('刷新失败', describeError(exception))
    rows.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

function onSearch(): void {
  page.value = 1
  void load('查询成功')
}

interface PageInfoLike {
  current?: number
  pageSize?: number
}

function onCurrentChange(current: number, pageInfo: PageInfoLike): void {
  page.value = current

  if (pageInfo?.pageSize) {
    pageSize.value = pageInfo.pageSize
  }

  void load()
}

function onPageSizeChange(size: number, pageInfo: PageInfoLike): void {
  pageSize.value = size
  page.value = pageInfo?.current ?? 1
  void load()
}

onMounted(() => {
  void load()
})
</script>

<template>
  <div class="page">
    <div class="page-toolbar">
      <div class="toolbar-left">
        <t-input v-model="keyword" class="keyword-input" placeholder="按邮箱或昵称搜索" clearable @enter="onSearch" />
        <t-button theme="primary" @click="onSearch">搜索</t-button>
      </div>
      <div class="toolbar-right">
        <span class="muted">共 {{ total }} 条</span>
        <t-button variant="outline" :loading="loading" @click="load('刷新成功')">刷新</t-button>
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
    >
      <template #operation="{ row }">
        <t-button theme="danger" variant="text" size="small"
          @click="confirmAction('删除用户', `确定删除用户“${row.nickName}”（${row.email}）？删除后该用户无法登录，会话成员及好友关系一并清理，消息保留。`, () => remove(row))">
          删除
        </t-button>
      </template>
    </t-table>

    <div class="page-footer">
      <t-pagination
        :current="page"
        :page-size="pageSize"
        :total="total"
        :page-size-options="[10, 20, 50]"
        show-jumper
        @current-change="onCurrentChange"
        @page-size-change="onPageSizeChange"
      />
    </div>
  </div>
</template>
