import { createRouter, createWebHashHistory } from 'vue-router'
import { isAuthenticated } from '../stores/auth'

// 路由级动态 import：每个页面单独成 chunk，首屏不再把四个页面一起加载。
const AdminLayout = () => import('../layouts/AdminLayout.vue')
const AdminUsersView = () => import('../views/AdminUsersView.vue')
const FilesView = () => import('../views/FilesView.vue')
const LoginView = () => import('../views/LoginView.vue')
const MessagesView = () => import('../views/MessagesView.vue')
const OnlineView = () => import('../views/OnlineView.vue')
const UsersView = () => import('../views/UsersView.vue')

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
        { path: 'admin-users', name: 'admin-users', component: AdminUsersView, meta: { requiresAuth: true, title: '后端用户' } },
        { path: 'files', name: 'files', component: FilesView, meta: { requiresAuth: true, title: '文件管理' } },
        { path: 'messages', name: 'messages', component: MessagesView, meta: { requiresAuth: true, title: '消息查询' } },
        { path: 'online', name: 'online', component: OnlineView, meta: { requiresAuth: true, title: '在线管理' } },
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
