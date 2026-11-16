import { createRouter, createWebHashHistory } from 'vue-router'
import AdminLayout from '../layouts/AdminLayout.vue'
import FilesView from '../views/FilesView.vue'
import LoginView from '../views/LoginView.vue'
import MessagesView from '../views/MessagesView.vue'
import UsersView from '../views/UsersView.vue'
import { isAuthenticated } from '../stores/auth'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { requiresAuth: false },
    },
    {
      path: '/',
      component: AdminLayout,
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: { name: 'users' } },
        { path: 'users', name: 'users', component: UsersView, meta: { requiresAuth: true, title: '用户管理' } },
        { path: 'files', name: 'files', component: FilesView, meta: { requiresAuth: true, title: '文件管理' } },
        { path: 'messages', name: 'messages', component: MessagesView, meta: { requiresAuth: true, title: '消息查询' } },
      ],
    },
    { path: '/:pathMatch(.*)*', redirect: { name: 'users' } },
  ],
})

router.beforeEach((to) => {
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)

  if (requiresAuth && !isAuthenticated()) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.name === 'login' && isAuthenticated()) {
    return { name: 'users' }
  }

  return true
})

export default router
