document.addEventListener('DOMContentLoaded', () => {
    const tabs = document.querySelectorAll('.auth-tab');

    tabs.forEach((tab) => {
        tab.addEventListener('click', () => {
            const target = tab.getAttribute('data-target');

            document.querySelectorAll('.auth-tab').forEach((item) => item.classList.remove('active'));
            document.querySelectorAll('.auth-panel').forEach((panel) => panel.classList.remove('active'));

            tab.classList.add('active');
            const activePanel = document.getElementById(target);
            if (activePanel) {
                activePanel.classList.add('active');
            }
        });
    });
});
