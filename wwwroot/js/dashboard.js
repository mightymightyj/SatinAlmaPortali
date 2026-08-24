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

async function handleCreateRequestSubmit(event) {
    event.preventDefault();
    var name = document.getElementById('reqProductName')?.value || '';
    var cat = document.getElementById('reqCategory')?.value || '';
    var qty = parseInt(document.getElementById('reqQuantity')?.value || '1', 10);
    var budget = document.getElementById('reqBudget')?.value ? parseFloat(document.getElementById('reqBudget').value) : null;
    var deadline = document.getElementById('reqDeadline')?.value || null;
    var notes = document.getElementById('reqNotes')?.value || '';

    if (!name.trim()) {
        showAppToast('Lütfen ürün veya hizmet adını giriniz.', false);
        return;
    }

    try {
        var response = await fetch('/Requests/CreateAjax', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                productName: name,
                requestType: cat,
                quantity: qty,
                estimatedBudget: budget,
                targetDeliveryDate: deadline,
                notes: notes
            })
        });

        var result = await response.json();
        if (result.success) {
            closeRequestModal();
            showAppToast('✓ ' + result.message);

            // Tabloya dinamik olarak en üste satır ekle
            var table = document.getElementById('buyerRequestsTable');
            if (table) {
                var tbody = table.querySelector('tbody');
                if (tbody) {
                    var tr = document.createElement('tr');
                    tr.setAttribute('data-status', 'Açık');
                    tr.style.animation = 'fadeInRow 0.4s ease';
                    var reqCode = 'REQ-' + (result.requestId ? ('000' + result.requestId).slice(-4) : 'YENI');
                    tr.innerHTML = '<td><div class="req-title-cell"><span class="req-code">' + reqCode + '</span><strong class="req-name">' + escapeHtml(name) + '</strong><small class="req-date">Bugün</small></div></td>' +
                        '<td><span class="category-tag">' + escapeHtml(cat) + '</span></td>' +
                        '<td><strong class="budget-amount">' + (budget ? Number(budget).toLocaleString('tr-TR') + ' ₺' : 'Belirtilmedi') + '</strong></td>' +
                        '<td><span class="status-badge badge-blue"><span class="status-dot"></span>Açık</span></td>' +
                        '<td class="text-right"><a href="/Requests" class="btn-table-action">İncele</a></td>';
                    tbody.insertBefore(tr, tbody.firstChild);
                }
            }
        } else {
            showAppToast('Hata: ' + (result.message || 'Talep kaydedilemedi.'), false);
        }
    } catch (err) {
        showAppToast('Sunucu hatası: ' + err.message, false);
    }
}

// --- TEDARİKÇİ (SUPPLIER) FİYAT GÜNCELLEME MODALI ---
function openPriceModal(itemId, itemName, currentPrice, currentStock) {
    var modal = document.getElementById('priceModal');
    if (!modal) return;

    var select = document.getElementById('priceProductSelect');
    var priceInput = document.getElementById('newUnitPrice');
    var stockInput = document.getElementById('newStockQty');
    var hiddenIdInput = document.getElementById('priceProductId');

    if (itemId && select) {
        select.value = itemId;
        if (priceInput && currentPrice) priceInput.value = currentPrice;
        if (stockInput && currentStock) stockInput.value = currentStock;
        if (hiddenIdInput) hiddenIdInput.value = itemId;
    } else if (select && select.selectedOptions.length > 0) {
        var opt = select.selectedOptions[0];
        var p = opt.getAttribute('data-price') || opt.value;
        var s = opt.getAttribute('data-stock') || '100';
        var id = opt.getAttribute('data-id') || opt.value;
        if (priceInput && p) priceInput.value = p;
        if (stockInput && s) stockInput.value = s;
        if (hiddenIdInput) hiddenIdInput.value = id;
    }

    modal.classList.add('is-open');
    modal.setAttribute('aria-hidden', 'false');
    if (priceInput) setTimeout(function () { priceInput.focus(); }, 50);
}

function closePriceModal() {
    var modal = document.getElementById('priceModal');
    if (modal) {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
    }
}

function handleProductPriceSelect(select) {
    var opt = select.selectedOptions[0];
    if (!opt) return;
    var price = opt.getAttribute('data-price') || opt.value;
    var stock = opt.getAttribute('data-stock');
    var id = opt.getAttribute('data-id') || opt.value;

    var priceInput = document.getElementById('newUnitPrice');
    var stockInput = document.getElementById('newStockQty');
    var hiddenId = document.getElementById('priceProductId');

    if (priceInput && price) priceInput.value = price;
    if (stockInput && stock) stockInput.value = stock;
    if (hiddenId) hiddenId.value = id;
}

async function handlePriceUpdateSubmit(event) {
    event.preventDefault();
    var select = document.getElementById('priceProductSelect');
    var opt = select ? select.selectedOptions[0] : null;
    var rawId = document.getElementById('priceProductId')?.value || (opt ? (opt.getAttribute('data-id') || opt.value) : '1');
    var itemId = parseInt(rawId, 10) || 1;
    var productName = opt ? opt.text.split('(')[0].trim() : 'Ürün';
    var newPrice = parseFloat(document.getElementById('newUnitPrice')?.value || '0');
    var newStock = parseInt(document.getElementById('newStockQty')?.value || '0', 10);
    var note = document.getElementById('priceChangeNote')?.value || '';

    if (newPrice <= 0) {
        showAppToast('Lütfen geçerli bir birim fiyat giriniz.', false);
        return;
    }

    try {
        var response = await fetch('/Dashboard/UpdateProductPrice', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                itemID: itemId,
                newUnitPrice: newPrice,
                newStockQuantity: newStock,
                note: note
            })
        });

        var result = await response.json();
        if (result.success) {
            closePriceModal();
            showAppToast('✓ ' + result.message, true);
            setTimeout(function () {
                window.location.reload();
            }, 1200);
        } else {
            showAppToast('Hata: ' + (result.message || 'Fiyat güncellenemedi.'), false);
        }
    } catch (err) {
        showAppToast('Sunucu bağlantı hatası: ' + err.message, false);
    }
}

// --- TEDARİKÇİ (SUPPLIER) TEKLİF VERME MODALI ---
function openOfferModal(rfqCodeOrId, productName, currentBid) {
    // 1. Sayısal ID veya REQ-0001 formatını ayıkla
    var reqId = 0;
    if (typeof rfqCodeOrId === 'string' && rfqCodeOrId.indexOf('-') > -1) {
        var parts = rfqCodeOrId.split('-');
        reqId = parseInt(parts[parts.length - 1], 10) || 0;
    } else {
        reqId = parseInt(rfqCodeOrId, 10) || 0;
    }

    // A) Requests/Index sayfasındaki modal: supplierBidModal
    var bidModal = document.getElementById('supplierBidModal');
    if (bidModal) {
        var reqInput = document.getElementById('bidRequestID');
        var prodTitle = document.getElementById('bidProductTitle');
        var priceInput = document.getElementById('bidOfferedPrice');

        if (reqInput) reqInput.value = reqId;
        if (prodTitle) prodTitle.textContent = productName || ('Talep #' + reqId);
        if (priceInput && currentBid) priceInput.value = parseFloat(currentBid);

        bidModal.classList.add('is-open');
        if (priceInput) setTimeout(function () { priceInput.focus(); }, 50);
        return;
    }

    // B) Dashboard/Supplier sayfasındaki modal: offerModal
    var modal = document.getElementById('offerModal');
    if (modal) {
        var title = document.getElementById('offerModalTitle');
        var subtitle = document.getElementById('offerModalSubtitle');
        var hiddenCode = document.getElementById('offerRfqCode');
        var bidInput = document.getElementById('offerAmount');

        if (title) title.textContent = currentBid ? 'Teklifi Düzenle' : 'İhaleye Fiyat Teklifi Ver';
        if (subtitle) subtitle.textContent = (rfqCodeOrId || ('Talep #' + reqId)) + (productName ? ' · ' + productName : '');
        if (hiddenCode) hiddenCode.value = reqId;
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
    var bidModal = document.getElementById('supplierBidModal');
    if (bidModal) {
        bidModal.classList.remove('is-open');
    }
}

function closeBidModal() {
    closeOfferModal();
}

async function handleOfferSubmit(event) {
    event.preventDefault();
    var code = document.getElementById('offerRfqCode')?.value || '';
    var amount = parseFloat(document.getElementById('offerAmount')?.value || '0');
    var days = parseInt(document.getElementById('offerDeliveryDays')?.value || '7', 10);
    var valDays = parseInt(document.getElementById('offerValidityDays')?.value || '30', 10);
    var notes = document.getElementById('offerNotes')?.value || '';

    var reqId = 0;
    if (typeof code === 'string' && code.indexOf('-') > -1) {
        var parts = code.split('-');
        reqId = parseInt(parts[parts.length - 1], 10) || 0;
    } else {
        reqId = parseInt(code, 10) || 0;
    }

    if (reqId <= 0) {
        showAppToast('Hata: Talep numarası tespit edilemedi.', false);
        return;
    }

    if (amount <= 0) {
        showAppToast('Lütfen geçerli bir teklif tutarı giriniz.', false);
        return;
    }

    try {
        var response = await fetch('/Requests/SubmitOfferAjax', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                requestId: reqId,
                offeredPrice: amount,
                deliveryDays: days,
                validityDays: valDays,
                supplierNotes: notes
            })
        });

        var result = await response.json();
        if (result.success) {
            closeOfferModal();
            showAppToast('✓ ' + result.message, true);
            setTimeout(function () {
                window.location.reload();
            }, 1200);
        } else {
            showAppToast('Teklif iletilemedi: ' + (result.message || 'Bilinmeyen hata.'), false);
        }
    } catch (err) {
        showAppToast('Sunucu bağlantı hatası: ' + err.message, false);
    }
}

function escapeHtml(text) {
    if (!text) return '';
    var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
    return text.replace(/[&<>"']/g, function(m) { return map[m]; });
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
