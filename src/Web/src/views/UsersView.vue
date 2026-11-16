<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { describeError, request } from '../api/http'
import type { PagedResult, UserDto } from '../api/types'

const rows = ref<UserDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const keyword = ref('')
const loading = ref(false)
const error = ref('')

const columns = [
  { colKey: 'email', title: '邮箱', width: 320 },
  { colKey: 'nickName', title: '昵称', width: 140 },
  { colKey: 'createdAtUtc', title: '创建时间（UTC+8）', width: 190 },
  { colKey: 'id', title: '用户 ID', ellipsis: true },
]

async function load(): Promise<void> {
  loading.value = true
  error.value = ''

  try {
    const query = new URLSearchParams({ page: String(page.value), pageSize: String(pageSize.value) })

    if (keyword.value.trim()) {
      query.set('keyword', keyword.value.trim())
    }

    const data = await request<PagedResult<UserDto>>(`/api/users?${query.toString()}`)
    rows.value = data.items
    total.value = data.total
  } catch (exception) {
    error.value = describeError(exception)
    rows.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

function onSearch(): void {
  page.value = 1
  void load()
}

function onCurrentChange(current: number, size: number): void {
  page.value = current

  if (size) {
    pageSize.value = size
  }

  void load()
}

function onPageSizeChange(size: number, current: number): void {
  pageSize.value = size
  page.value = current || 1
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
        <t-button variant="outline" :loading="loading" @click="load">刷新</t-button>
      </div>
    </div>

    <t-alert v-if="error" theme="error" :message="error" class="page-error" />

    <t-table
      row-key="id"
      :data="rows"
      :columns="columns"
      :loading="loading"
      stripe
      hover
      table-layout="fixed"
    />

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
