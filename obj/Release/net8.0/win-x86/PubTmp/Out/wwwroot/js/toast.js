const Toast = {
    container: null,

    init() {
        if (this.container) return;
        this.container = document.createElement('div');
        this.container.style.cssText =
            'position:fixed;top:70px;left:20px;' +
            'z-index:9999;display:flex;' +
            'flex-direction:column;gap:8px;' +
            'max-width:320px;';
        document.body.appendChild(this.container);
    },

    show(message, type = 'success', duration = 4000) {
        this.init();

        const colors = {
            success: {
                bg: '#d1fae5', border: '#28a745',
                icon: 'fa-check-circle',
                text: '#065f46'
            },
            error: {
                bg: '#fee2e2', border: '#dc3545',
                icon: 'fa-times-circle',
                text: '#991b1b'
            },
            warning: {
                bg: '#fef3c7', border: '#ffc107',
                icon: 'fa-exclamation-triangle',
                text: '#92400e'
            },
            info: {
                bg: '#dbeafe', border: '#2d6a9f',
                icon: 'fa-info-circle',
                text: '#1e3a5f'
            },
        };

        const c = colors[type] || colors.info;
        const toast = document.createElement('div');
        toast.style.cssText =
            `background:${c.bg};border:1px solid ${c.border};` +
            `color:${c.text};border-radius:12px;` +
            'padding:12px 16px;display:flex;' +
            'align-items:flex-start;gap:10px;' +
            'box-shadow:0 4px 16px rgba(0,0,0,.12);' +
            'animation:slideIn .3s ease;' +
            'cursor:pointer;font-size:13px;' +
            'font-family:inherit;';

        toast.innerHTML =
            `<i class="fas ${c.icon}" ` +
            `style="margin-top:1px;flex-shrink:0;` +
            `color:${c.border};font-size:15px;"></i>` +
            `<span style="flex:1;line-height:1.5;">` +
            `${message}</span>` +
            `<i class="fas fa-times" ` +
            `style="opacity:.5;font-size:12px;` +
            `margin-top:2px;flex-shrink:0;"></i>`;

        toast.onclick = () => this.dismiss(toast);
        this.container.appendChild(toast);

        setTimeout(() => this.dismiss(toast), duration);
        return toast;
    },

    dismiss(toast) {
        toast.style.animation = 'slideOut .3s ease';
        setTimeout(() => toast.remove(), 280);
    },

    success: (msg, dur) =>
        Toast.show(msg, 'success', dur),
    error: (msg, dur) =>
        Toast.show(msg, 'error', dur),
    warning: (msg, dur) =>
        Toast.show(msg, 'warning', dur),
    info: (msg, dur) =>
        Toast.show(msg, 'info', dur),
};

// CSS Animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from { opacity:0; transform:translateX(-20px); }
        to   { opacity:1; transform:translateX(0); }
    }
    @keyframes slideOut {
        from { opacity:1; transform:translateX(0); }
        to   { opacity:0; transform:translateX(-20px); }
    }`;
document.head.appendChild(style);