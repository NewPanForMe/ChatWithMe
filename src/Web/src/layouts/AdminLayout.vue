<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { logout, session } from '../stores/auth'
import { notifySuccess } from '../utils/notify'

const route = useRoute()
const router = useRouter()

const menus = [
  { value: '/notifications', label: '通知管理' },
  { value: '/users', label: '用户管理' },
  { value: '/admin-users', label: '后端用户' },
  { value: '/files', label: '文件管理' },
  { value: '/messages', label: '消息查询' },
  { value: '/online', label: '在线管理' },
]

const activeMenu = computed(() => menus.find((menu) => route.path.startsWith(menu.value))?.value ?? '/users')
const pageTitle = computed(() => menus.find((menu) => menu.value === activeMenu.value)?.label ?? '管理后台')

function onMenuChange(value: string | number): void {
  router.push(String(value))
}

async function onLogout(): Promise<void> {
  await logout()
  await router.replace({ name: 'login' })
  notifySuccess('已退出登录')
}
</script>

<template>
  <t-layout class="admin-shell">
    <t-aside class="admin-aside">
      <div class="brand">
        <span class="brand-mark">C</span>
        <div class="brand-text">
          <strong>ChatWithMe</strong>
          <small>管理后台</small>
        </div>
      </div>

      <t-menu :value="activeMenu" class="admin-menu" @change="onMenuChange">
        <t-menu-item v-for="menu in menus" :key="menu.value" :value="menu.value">
          {{ menu.label }}
        </t-menu-item>
      </t-menu>
    </t-aside>

    <t-layout>
      <t-header class="admin-header">
        <h1>{{ pageTitle }}</h1>
        <div class="header-actions">
          <span class="current-user">{{ session.user?.nickName || session.user?.email }}</span>
          <t-button theme="default" variant="outline" size="small" @click="onLogout">退出登录</t-button>
        </div>
      </t-header>

      <t-content class="admin-content">
        <router-view />
      </t-content>
    </t-layout>
  </t-layout>
</template>
