<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { describeError } from '../api/http'
import { login } from '../stores/auth'
import { notifyError, notifySuccess } from '../utils/notify'

const route = useRoute()
const router = useRouter()

const email = ref('')
const password = ref('')
const loading = ref(false)

async function onSubmit(): Promise<void> {
  if (!email.value.trim() || !password.value) {
    notifyError('无法登录', '请输入邮箱和密码。')
    return
  }

  loading.value = true

  try {
    await login(email.value.trim(), password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/users'
    await router.replace(redirect)
    notifySuccess('登录成功')
  } catch (exception) {
    notifyError('登录失败', describeError(exception))
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-card">
      <div class="login-head">
        <span class="brand-mark large">C</span>
        <h1>ChatWithMe 管理后台</h1>
      </div>

      <t-form class="login-form" label-align="top">
        <t-form-item label="邮箱">
          <t-input v-model="email" placeholder="name@example.com" autocomplete="username" />
        </t-form-item>
        <t-form-item label="密码">
          <t-input v-model="password" type="password" placeholder="请输入密码" autocomplete="current-password"
            @enter="onSubmit" />
        </t-form-item>

        <t-button class="login-submit" theme="primary" block size="large" :loading="loading" @click="onSubmit">登录</t-button>
      </t-form>

    </div>
  </div>
</template>
