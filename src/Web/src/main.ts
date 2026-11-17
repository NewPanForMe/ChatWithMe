import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import { bootstrapSession } from './stores/auth'
import './styles.css'

// 先用 HttpOnly Cookie 恢复会话，再挂载应用，避免受保护页面闪一下又被踢回登录页。
void bootstrapSession().finally(() => {
  createApp(App).use(router).mount('#app')
})
