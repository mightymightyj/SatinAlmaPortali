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

// =============================================================================
// MODAL, FİLTRE VE ETKİLEŞİM FONKSİYONLARI (BUYER & SUPPLIER)
// =============================================================================

// Toast Bildirimi Göster
function showAppToast(message, isSuccess) {
    var toast = document.getElementById('appToast');
    if (!toast) return;
    toast.textContent = message;
    if (isSuccess === false) {
        toast.style.background = '#DC2626';
    } else {
        toast.style.background = '#0F172A';
    }
    toast.classList.add('is-show');
    setTimeout(function () {
        toast.classList.remove('is-show');
    }, 3800);
}

// --- ALICI (BUYER) TALEP MODALI ---
function openRequestModal() {
    var modal = document.getElementById('requestModal');
    if (modal) {
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        var input = document.getElementById('reqProductName');
        if (input) setTimeout(function () { input.focus(); }, 50);
    }
}

function closeRequestModal() {
    var modal = document.getElementById('requestModal');
    if (modal) {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
    }
}

function handleCreateRequestSubmit(event) {
    event.preventDefault();
    var name = document.getElementById('reqProductName')?.value || '';
    var cat = document.getElementById('reqCategory')?.value || '';
    var qty = document.getElementById('reqQuantity')?.value || '1';
    var budget = document.getElementById('reqBudget')?.value || '';

    closeRequestModal();
    showAppToast('✓ "' + name + '" talebi başarıyla oluşturuldu ve tedarikçilere yayınlandı!');

    // Tabloya dinamik olarak en üste satır ekle
    var table = document.getElementById('buyerRequestsTable');
    if (table) {
        var tbody = table.querySelector('tbody');
        if (tbody) {
            var tr = document.createElement('tr');
            tr.setAttribute('data-status', 'Açık');
            tr.style.animation = 'fadeInRow 0.4s ease';
            var reqCode = 'REQ-' + new Date().getFullYear() + '-' + Math.floor(100 + Math.random() * 900);
            tr.innerHTML = '<td><div class="req-title-cell"><span class="req-code">' + reqCode + '</span><strong class="req-name">' + name + '</strong><small class="req-date">Bugün</small></div></td>' +
                '<td><span class="category-tag">' + cat + '</span></td>' +
                '<td><span class="bids-pill">Yeni Yayınlandı</span></td>' +
                '<td><strong class="budget-amount">' + (budget ? Number(budget).toLocaleString('tr-TR') + ' ₺' : 'Belirtilmedi') + '</strong></td>' +
                '<td><span class="status-badge badge-blue"><span class="status-dot"></span>Açık</span></td>' +
                '<td class="text-right"><a href="/Requests" class="btn-table-action">İncele</a></td>';
            tbody.insertBefore(tr, tbody.firstChild);
        }
    }
}

// --- TEDARİKÇİ (SUPPLIER) FİYAT GÜNCELLEME MODALI ---
function openPriceModal() {
    var modal = document.getElementById('priceModal');
    if (modal) {
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
    }
}

function closePriceModal() {
    var modal = document.getElementById('priceModal');
    if (modal) {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
    }
}

function handleProductPriceSelect(select) {
    var price = select.value;
    var priceInput = document.getElementById('newUnitPrice');
    if (priceInput && price) {
        priceInput.value = price;
    }
}

function handlePriceUpdateSubmit(event) {
    event.preventDefault();
    var select = document.getElementById('priceProductSelect');
    var productName = select ? select.options[select.selectedIndex].text.split('(')[0].trim() : 'Ürün';
    var newPrice = document.getElementById('newUnitPrice')?.value || '0';

    closePriceModal();
    showAppToast('✓ ' + productName + ' birim fiyatı ' + Number(newPrice).toLocaleString('tr-TR') + ' ₺ olarak güncellendi!');
}

// --- TEDARİKÇİ (SUPPLIER) TEKLİF VERME MODALI ---
function openOfferModal(rfqCode, productName, currentBid) {
    var modal = document.getElementById('offerModal');
    if (modal) {
        var title = document.getElementById('offerModalTitle');
        var subtitle = document.getElementById('offerModalSubtitle');
        var hiddenCode = document.getElementById('offerRfqCode');
        var bidInput = document.getElementById('offerAmount');

        if (title) title.textContent = currentBid ? 'Teklifi Düzenle' : 'İhaleye Fiyat Teklifi Ver';
        if (subtitle) subtitle.textContent = (rfqCode || '') + ' · ' + (productName || '');
        if (hiddenCode) hiddenCode.value = rfqCode || '';
        if (bidInput) bidInput.value = currentBid ? parseFloat(currentBid) : '';

        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        if (bidInput) setTimeout(function () { bidInput.focus(); }, 50);
    }
}

function closeOfferModal() {
    var modal = document.getElementById('offerModal');
    if (modal) {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
    }
}

function handleOfferSubmit(event) {
    event.preventDefault();
    var code = document.getElementById('offerRfqCode')?.value || '';
    var amount = document.getElementById('offerAmount')?.value || '0';

    closeOfferModal();
    showAppToast('✓ ' + code + ' talebi için ' + Number(amount).toLocaleString('tr-TR') + ' ₺ tutarındaki teklifiniz alıcıya iletildi!');
}

// --- TABLO FİLTRELEME İŞLEMLERİ ---
function filterRequestTable(status, btn) {
    if (btn) {
        var parent = btn.parentElement;
        if (parent) {
            parent.querySelectorAll('.tab-pill').forEach(function (p) { p.classList.remove('is-active'); });
            btn.classList.add('is-active');
        }
    }

    var table = document.getElementById('buyerRequestsTable');
    if (!table) return;

    var rows = table.querySelectorAll('tbody tr');
    rows.forEach(function (row) {
        var rowStatus = row.getAttribute('data-status');
        if (status === 'all' || rowStatus === status) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function filterRfqTable(status, btn) {
    if (btn) {
        var parent = btn.parentElement;
        if (parent) {
            parent.querySelectorAll('.tab-pill').forEach(function (p) { p.classList.remove('is-active'); });
            btn.classList.add('is-active');
        }
    }

    var table = document.getElementById('supplierRfqTable');
    if (!table) return;

    var rows = table.querySelectorAll('tbody tr');
    rows.forEach(function (row) {
        var rowStatus = row.getAttribute('data-status');
        if (status === 'all' || rowStatus === status) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function filterRequestsByStatus(status) {
    var tab = document.querySelector('.table-filter-pills button[onclick*="' + status + '"]');
    if (tab) {
        tab.click();
    }
}

// --- BİLDİRİM AÇILIR MENÜSÜ ---
function toggleNotificationDropdown() {
    showAppToast('🔔 2 yeni bildiriminiz bulunmaktadır: 1 yeni teklif onaylandı, 1 RFQ süresi dolmak üzere.');
}

// --- KLAVYE ESC TUŞU İLE MODAL KAPATMA ---
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        closeRequestModal();
        closePriceModal();
        closeOfferModal();
    }
});
