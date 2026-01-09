// Theme detection and system preference monitoring
window.themeDetector = {
    // Get the current system theme preference
    getSystemTheme: function () {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    },

    // Check if system prefers dark mode
    isDarkMode: function () {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    // Listen for system theme changes and notify Blazor
    addSystemThemeListener: function (dotNetHelper) {
        if (window.matchMedia) {
            const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');

            // Store the listener so we can remove it later
            window.themeDetector.listener = (e) => {
                dotNetHelper.invokeMethodAsync('OnSystemThemeChanged', e.matches);
            };

            darkModeQuery.addEventListener('change', window.themeDetector.listener);
        }
    },

    // Remove the system theme listener
    removeSystemThemeListener: function () {
        if (window.matchMedia && window.themeDetector.listener) {
            const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');
            darkModeQuery.removeEventListener('change', window.themeDetector.listener);
            window.themeDetector.listener = null;
        }
    }
};
