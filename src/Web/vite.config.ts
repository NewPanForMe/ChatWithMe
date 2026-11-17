import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite'
import { TDesignResolver } from 'unplugin-vue-components/resolvers'

export default defineConfig({
  plugins: [
    vue(),
    // TDesign 组件按需解析：模板里用到的组件才打进产物，不再整个库全量注册。
    Components({
      dts: 'src/components.d.ts',
      resolvers: [TDesignResolver({ library: 'vue-next' })],
    }),
  ],
  base: './',
  server: {
    // 与后端同站点（localhost:5217）才能带上 HttpOnly 刷新令牌 Cookie。
    host: 'localhost',
    port: 5173,
    strictPort: true,
  },
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
