import { createApp } from 'vue'
import TDesign from 'tdesign-vue-next'
import 'tdesign-vue-next/es/style/index.css'
import App from './App.vue'
import router from './router'
import { restoreSession } from './stores/auth'
import './styles.css'

restoreSession()

createApp(App).use(router).use(TDesign).mount('#app')
