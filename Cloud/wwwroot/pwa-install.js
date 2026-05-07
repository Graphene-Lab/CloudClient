// PWA install prompt handler (no external JS dependency)
(function () {
    let deferredPrompt = null;

    function showPrompt() {
        const prompt = document.getElementById('pwaInstallPrompt');
        if (prompt) {
            prompt.style.display = 'block';
        }
    }

    function hidePrompt() {
        const prompt = document.getElementById('pwaInstallPrompt');
        if (prompt) {
            prompt.style.display = 'none';
        }
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', showPrompt, { once: true });
        } else {
            showPrompt();
        }
    });

    window.pwaInstall = async function () {
        hidePrompt();
        if (deferredPrompt) {
            await deferredPrompt.prompt();
            deferredPrompt = null;
        }
    };

    window.pwaDismiss = function () {
        hidePrompt();
        deferredPrompt = null;
    };
})();
