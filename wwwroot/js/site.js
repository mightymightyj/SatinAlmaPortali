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
});
