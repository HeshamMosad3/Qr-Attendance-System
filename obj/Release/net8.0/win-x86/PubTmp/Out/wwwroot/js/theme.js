(function () {
    const THEME_KEY = 'qr-theme';

    function getTheme() {
        return localStorage.getItem(THEME_KEY) || 'light';
    }

    function applyTheme(theme) {
        document.documentElement
            .setAttribute('data-bs-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
        const icon = document.getElementById('themeIcon');
        if (icon) {
            icon.className = theme === 'dark'
                ? 'fas fa-sun'
                : 'fas fa-moon';
        }
    }

    function toggleTheme() {
        const current = getTheme();
        applyTheme(current === 'dark'
            ? 'light' : 'dark');
    }

    // تطبيق الثيم فور التحميل
    applyTheme(getTheme());

    window.toggleTheme = toggleTheme;
    window.applyTheme = applyTheme;
})();