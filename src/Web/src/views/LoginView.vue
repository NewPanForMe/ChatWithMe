<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { apiBaseUrl, describeError } from '../api/http'
import { login } from '../stores/auth'

const route = useRoute()
const router = useRouter()

const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')

async function onSubmit(): Promise<void> {
  if (!email.value.trim() || !password.value) {
    error.value = '请输入邮箱和密码。'
    return
  }

  loading.value = true
  error.value = ''

  try {
    await login(email.value.trim(), password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/users'
    await router.replace(redirect)
  } catch (exception) {
    error.value = describeError(exception)
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
        <p>使用已注册账号登录，访问令牌 15 分钟内有效并自动续期。</p>
      </div>

      <t-form label-align="top">
        <t-form-item label="邮箱">
          <t-input v-model="email" placeholder="name@example.com" autocomplete="username" />
        </t-form-item>
        <t-form-item label="密码">
          <t-input
            v-model="password"
            type="password"
            placeholder="请输入密码"
            autocomplete="current-password"
            @enter="onSubmit"
          />
        </t-form-item>

        <t-alert v-if="error" theme="error" :message="error" class="login-error" />

        <t-button theme="primary" block size="large" :loading="loading" @click="onSubmit">登录</t-button>
      </t-form>

      <p class="login-tip">后端地址：{{ apiBaseUrl }}</p>
    </div>
  </div>
</template>
