<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { describeError, request } from '../api/http'
import { notifyError, notifySuccess } from '../utils/notify'
import { confirmAction } from '../utils/confirm'
import { session } from '../stores/auth'
import type { AdminAccountDto } from '../api/types'

const rows = ref<AdminAccountDto[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const submitting = ref(false)

const form = ref({ email: '', displayName: '', password: '' })

const columns = [
  { colKey: 'email', title: '邮箱', width: 280 },
  { colKey: 'displayName', title: '名称', width: 160 },
  { colKey: 'isActive', title: '状态', width: 100 },
  { colKey: 'createdAtUtc', title: '创建时间（UTC+8）', width: 190 },
  { colKey: 'operation', title: '操作', width: 110 },
]

async function load(successTitle = ''): Promise<void> {
  loading.value = true

  try {
    rows.value = await request<AdminAccountDto[]>('/api/admin/accounts')
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

function openCreate(): void {
  form.value = { email: '', displayName: '', password: '' }
  dialogVisible.value = true
}

async function submitCreate(): Promise<void> {
  if (!form.value.email.trim() || !form.value.displayName.trim() || form.value.password.length < 8) {
    notifyError('无法创建管理员', '请填写邮箱、名称，密码至少 8 位。')
    return
  }

  submitting.value = true

  try {
    await request<AdminAccountDto>('/api/admin/accounts', {
      method: 'POST',
      body: JSON.stringify({
        email: form.value.email.trim(),
        displayName: form.value.displayName.trim(),
        password: form.value.password,
      }),
    })

    dialogVisible.value = false
    notifySuccess('创建管理员成功')
    await load()
  } catch (exception) {
    notifyError('创建管理员失败', describeError(exception))
  } finally {
    submitting.value = false
  }
}

async function remove(row: AdminAccountDto): Promise<void> {
  try {
    await request<void>(`/api/admin/accounts/${row.id}`, { method: 'DELETE' })
    notifySuccess('删除管理员成功')
    await load()
  } catch (exception) {
    notifyError('删除管理员失败', describeError(exception))
  }
}

function isSelf(row: AdminAccountDto): boolean {
  return session.user?.id === row.id
}

onMounted(() => {
  void load()
})
</script>

<template>
  <div class="page">
    <div class="page-toolbar">
      <div class="toolbar-left">
        <span class="muted">管理后台账号与聊天用户分离，这里的账号只能登录管理后台。</span>
      </div>
      <div class="toolbar-right">
        <span class="muted">共 {{ rows.length }} 个</span>
        <t-button theme="primary" @click="openCreate">新建管理员</t-button>
        <t-button variant="outline" :loading="loading" @click="load('刷新成功')">刷新</t-button>
      </div>
    </div>

    <t-table row-key="id" :data="rows" :columns="columns" :loading="loading" stripe hover table-layout="fixed">
      <template #isActive="{ row }">
        <t-tag :theme="row.isActive ? 'success' : 'default'" variant="light">
          {{ row.isActive ? '启用' : '停用' }}
        </t-tag>
      </template>
      <template #operation="{ row }">
        <t-button
          v-if="!isSelf(row)"
          theme="danger"
          variant="text"
          size="small"
          @click="confirmAction('删除管理员', `确定删除管理员“${row.displayName}”（${row.email}）？删除后该管理员无法再登录管理后台。`, () => remove(row))"
        >
          删除
        </t-button>
        <span v-else class="muted">当前账号</span>
      </template>
    </t-table>

    <t-dialog v-model:visible="dialogVisible" header="新建管理员" :confirm-btn="{ content: '创建', loading: submitting }" @confirm="submitCreate">
      <t-form label-align="top">
        <t-form-item label="邮箱">
          <t-input v-model="form.email" placeholder="admin@example.com" />
        </t-form-item>
        <t-form-item label="名称">
          <t-input v-model="form.displayName" placeholder="显示名称" />
        </t-form-item>
        <t-form-item label="密码">
          <t-input v-model="form.password" type="password" placeholder="至少 8 位" />
        </t-form-item>
      </t-form>
    </t-dialog>
  </div>
</template>
