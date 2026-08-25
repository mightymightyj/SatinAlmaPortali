document.addEventListener('DOMContentLoaded', () => {
    function activateTab(targetId) {
        document.querySelectorAll('.auth-tab').forEach((item) => {
            if (item.getAttribute('data-target') === targetId) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });

        document.querySelectorAll('.auth-panel').forEach((panel) => {
            if (panel.id === targetId) {
                panel.classList.add('active');
            } else {
                panel.classList.remove('active');
            }
        });
    }

    const tabs = document.querySelectorAll('.auth-tab');
    tabs.forEach((tab) => {
        tab.addEventListener('click', () => {
            const target = tab.getAttribute('data-target');
            if (target) {
                activateTab(target);
            }
        });
    });

    const switchLinks = document.querySelectorAll('.switch-tab-link');
    switchLinks.forEach((link) => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const target = link.getAttribute('data-target');
            if (target) {
                activateTab(target);
            }
        });
    });

    // Global Navbar Sepet Rozeti Yönetimi
    initGlobalCartBadge();
});

/**
 * Tüm sayfalarda navbar sepet rozetini güncel tutar ve oturum açıldığında veritabanı ile senkronize eder.
 */
function updateGlobalNavCartBadge() {
    const navBadge = document.getElementById('navCartBadge');
    if (!navBadge) return;

    try {
        const stored = localStorage.getItem('b2b_portal_cart');
        const cart = stored ? JSON.parse(stored) : [];
        const count = Array.isArray(cart) ? cart.length : 0;
        navBadge.textContent = count;
        navBadge.style.display = count > 0 ? 'inline-block' : 'none';
    } catch (e) {
        navBadge.style.display = 'none';
    }
}

async function initGlobalCartBadge() {
    updateGlobalNavCartBadge();

    // Eğer trendyol-vitrin.js bu sayfada yüklü değilse, arka planda sepet senkronizasyonunu site.js yürütür
    if (typeof initB2BCartSync !== 'function') {
        try {
            let localCart = [];
            try {
                const stored = localStorage.getItem('b2b_portal_cart');
                localCart = stored ? JSON.parse(stored) : [];
            } catch (e) {}

            const response = await fetch('/Cart/SyncCart', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(localCart.map(i => ({
                    productID: i.productId || i.productID,
                    title: i.title || '',
                    sku: i.sku || '',
                    category: i.category || '',
                    price: i.price || 0,
                    quantity: i.quantity || 1,
                    seller: i.seller || '',
                    image: i.image || ''
                })))
            });

            const data = await response.json();
            if (data.success && data.isAuthenticated && Array.isArray(data.items)) {
                try {
                    localStorage.setItem('b2b_portal_cart', JSON.stringify(data.items));
                } catch (e) {}
                updateGlobalNavCartBadge();
            }
        } catch (err) {
            // Sessizce geç
        }
    }
}

// Diğer sekmelerdeki sepet değişikliklerini dinle
window.addEventListener('storage', function (e) {
    if (e.key === 'b2b_portal_cart') {
        updateGlobalNavCartBadge();
    }
});
