import { DialogPlugin } from 'tdesign-vue-next'

/** 列表行操作统一使用屏幕居中的模态确认框。 */
export function confirmAction(header: string, body: string, action: () => Promise<void>): void {
  let submitting = false
  const dialog = DialogPlugin.confirm({
    header,
    body,
    placement: 'center',
    mode: 'modal',
    theme: 'warning',
    confirmBtn: { content: '确认', theme: 'danger' },
    cancelBtn: '取消',
    closeOnOverlayClick: false,
    onClose: () => {
      if (!submitting) {
        dialog.destroy()
      }
    },
    onConfirm: async () => {
      if (submitting) {
        return
      }

      submitting = true
      dialog.update({ confirmLoading: true, cancelBtn: { content: '取消', disabled: true }, closeBtn: false, closeOnEscKeydown: false })

      try {
        await action()
      } finally {
        dialog.destroy()
      }
    },
  })
}
