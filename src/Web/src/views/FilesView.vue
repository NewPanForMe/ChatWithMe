<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiBaseUrl, describeError, request } from '../api/http'
import { notifyError, notifySuccess } from '../utils/notify'
import type { FileAssetDto, FileLinkDto, PagedResult } from '../api/types'

const rows = ref<FileAssetDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const keyword = ref('')
const loading = ref(false)
const openingId = ref<string | null>(null)

const columns = [
  { colKey: 'originalName', title: '原始文件名', width: 260 },
  { colKey: 'contentType', title: '类型', width: 180 },
  { colKey: 'sizeInBytes', title: '大小', width: 120 },
  { colKey: 'createdAtUtc', title: '上传时间（UTC+8）', width: 200 },
  { colKey: 'url', title: '访问地址', ellipsis: true },
]

function formatSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }

  return `${(bytes / 1024 / 1024).toFixed(2)} MB`
}

async function load(successTitle = ''): Promise<void> {
  loading.value = true

  try {
    const query = new URLSearchParams({ page: String(page.value), pageSize: String(pageSize.value) })

    if (keyword.value.trim()) {
      query.set('keyword', keyword.value.trim())
    }

    const data = await request<PagedResult<FileAssetDto>>(`/api/files?${query.toString()}`)
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

/** 文件不再有匿名静态路径：先换一个短时签名链接，再在新标签页打开。 */
async function openFile(row: FileAssetDto): Promise<void> {
  // 先同步开空白页再赋值：await 之后浏览器已认为不是用户手势，window.open 会被拦截。
  const preview = window.open('', '_blank')

  openingId.value = row.id

  try {
    const link = await request<FileLinkDto>(`/api/files/${row.id}/link`)
    const target = `${apiBaseUrl}${link.url}`

    if (preview) {
      preview.opener = null
      preview.location.href = target
    } else {
      window.location.href = target
    }
  } catch (exception) {
    preview?.close()
    notifyError('打开文件失败', describeError(exception))
  } finally {
    openingId.value = null
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
        <t-input v-model="keyword" class="keyword-input" placeholder="按文件名搜索" clearable @enter="onSearch" />
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
      <template #originalName="{ row }">
        <a href="#" @click.prevent="openFile(row)">{{ row.originalName }}</a>
      </template>
      <template #sizeInBytes="{ row }">
        {{ formatSize(row.sizeInBytes) }}
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
