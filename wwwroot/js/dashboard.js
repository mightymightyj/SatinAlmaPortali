// Panel grafikleri için ipucu balonu.
// Veri, işaretlerin üzerindeki şeffaf hit alanlarında data-tip-* olarak durur:
//   data-tip-title="Oca 2026"
//   data-tip-rows="#2a78d6;Alış;1.284.500 ₺|#eb6834;Satış;845.200 ₺"
// Metin her zaman textContent ile yazılır, HTML olarak yorumlanmaz.
(function () {
    'use strict';

    var targets = document.querySelectorAll('[data-tip-title]');
    if (targets.length === 0) return;

    var tooltip = document.createElement('div');
    tooltip.className = 'viz-tooltip';
    tooltip.setAttribute('role', 'tooltip');
    document.body.appendChild(tooltip);

    function build(element) {
        tooltip.textContent = '';

        var title = document.createElement('div');
        title.className = 'tip-title';
        title.textContent = element.getAttribute('data-tip-title') || '';
        tooltip.appendChild(title);

        var raw = element.getAttribute('data-tip-rows') || '';
        raw.split('|').forEach(function (entry) {
            if (!entry) return;
            var parts = entry.split(';');
            if (parts.length < 3) return;

            var row = document.createElement('div');
            row.className = 'tip-row';

            var key = document.createElement('span');
            key.className = 'tip-key';

            var swatch = document.createElement('span');
            swatch.className = 'tip-swatch';
            swatch.style.backgroundColor = parts[0];

            key.appendChild(swatch);
            key.appendChild(document.createTextNode(parts[1]));

            var value = document.createElement('span');
            value.className = 'tip-value';
            value.textContent = parts[2];

            row.appendChild(key);
            row.appendChild(value);
            tooltip.appendChild(row);
        });
    }

    function place(x, y) {
        var box = tooltip.getBoundingClientRect();
        var left = x + 14;
        var top = y + 14;

        if (left + box.width > window.innerWidth - 8) left = x - box.width - 14;
        if (top + box.height > window.innerHeight - 8) top = y - box.height - 14;

        tooltip.style.left = Math.max(8, left) + 'px';
        tooltip.style.top = Math.max(8, top) + 'px';
    }

    function show(element, x, y) {
        build(element);
        tooltip.classList.add('is-visible');
        place(x, y);
    }

    function hide() {
        tooltip.classList.remove('is-visible');
    }

    Array.prototype.forEach.call(targets, function (element) {
        element.addEventListener('mouseenter', function (event) {
            show(element, event.clientX, event.clientY);
        });

        element.addEventListener('mousemove', function (event) {
            place(event.clientX, event.clientY);
        });

        element.addEventListener('mouseleave', hide);

        // Klavye ve dokunmatik için: odaklanınca işaretin kendi konumunu kullan.
        element.addEventListener('focus', function () {
            var box = element.getBoundingClientRect();
            show(element, box.left + box.width / 2, box.top);
        });

        element.addEventListener('blur', hide);
    });

    window.addEventListener('scroll', hide, true);
})();
