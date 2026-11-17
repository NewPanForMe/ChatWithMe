import { NotifyPlugin } from 'tdesign-vue-next'

/**
 * 全局消息通知：标题统一为“通知”，操作结果和详情放在内容中，位置固定在右上角。
 * 页面里不要再各自维护提示状态，直接调用这里的函数即可。
 */
const PLACEMENT = 'top-right' as const

const SUCCESS_DURATION = 4000
const ERROR_DURATION = 6000

export function notifySuccess(message: string, detail = ''): void {
  NotifyPlugin.success({
    placement: PLACEMENT,
    title: '通知',
    content: detail ? `${message}：${detail}` : message,
    duration: SUCCESS_DURATION,
    closeBtn: true,
  })
}

export function notifyError(message: string, detail = ''): void {
  NotifyPlugin.error({
    placement: PLACEMENT,
    title: '通知',
    content: detail ? `${message}：${detail}` : message,
    duration: ERROR_DURATION,
    closeBtn: true,
  })
}

export function notifyInfo(message: string, detail = ''): void {
  NotifyPlugin.info({
    placement: PLACEMENT,
    title: '通知',
    content: detail ? `${message}：${detail}` : message,
    duration: SUCCESS_DURATION,
    closeBtn: true,
  })
}
