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
    if (typeof showRealtimeToast === 'function') {
        showRealtimeToast(message, isSuccess === false ? 'error' : (isSuccess === true ? 'success' : 'info'));
        return;
    }
    var toast = document.getElementById('appToast') || document.getElementById('realtimeToast');
    if (!toast) return;
    toast.textContent = message;
    toast.style.background = (isSuccess === false) ? '#DC2626' : ((isSuccess === true) ? '#059669' : '#0F172A');
    toast.classList.remove('is-show');
    void toast.offsetWidth;
    toast.classList.add('is-show');
    setTimeout(function () {
        toast.classList.remove('is-show');
    }, 3800);
}

// --- ALICI (BUYER) TALEP MODALI (DİNAMİK İSTENİLENLER LİSTESİ) ---
function initDashRequestItems() {
    var tbody = document.getElementById('dashRequestItemsTableBody');
    if (!tbody) return;
    if (tbody.children.length === 0) {
        addDashRequestItemRow('', 1, '', '');
        addDashRequestItemRow('', 1, '', '');
        addDashRequestItemRow('', 1, '', '');
    }
    recalculateDashRequestTotals();
}

function loadDashSampleTemplate(type) {
    var tbody = document.getElementById('dashRequestItemsTableBody');
    var mainTitle = document.getElementById('reqProductName');
    if (!tbody) return;
    tbody.innerHTML = '';

    if (type === 'elektrik') {
        if (mainTitle) mainTitle.value = 'Fabrika Elektrik & Otomasyon Revizyonu Malzemeleri';
        addDashRequestItemRow('Schneider 24V 10A Ray Tipi Güç Kaynağı', 2, 2250, 'DIN Ray montaj, IP20');
        addDashRequestItemRow('Siemens S7-1200 PLC CPU 1214C DC/DC/DC', 1, 18500, '6ES7214-1AG40-0XB0 veya muadili');
        addDashRequestItemRow('Wago 2.5mm² Ray Klemens (Gri)', 150, 14, 'Yaylı bağlantı');
        addDashRequestItemRow('Öznur 3x2.5mm² NYAF Çok Damarlı Kablo', 100, 38, 'Halogen Free (HFFR)');
    } else if (type === 'ofis') {
        if (mainTitle) mainTitle.value = 'Merkez Ofis 2026 Q2 Kırtasiye ve Sarf Malzeme Alımı';
        addDashRequestItemRow('A4 80gr 1. Hamur Fotokopi Kağıdı (5x500)', 25, 920, 'Navigator veya Copier Bond');
        addDashRequestItemRow('Mavi Tükenmez Kalem 0.7mm', 10, 110, '50\'li paket');
        addDashRequestItemRow('Masaüstü Şeffaf Telli Dosya', 300, 4.5, 'PP malzeme');
        addDashRequestItemRow('Zımba Teli No:24/6', 15, 35, '1000\'li kutu');
    } else if (type === 'hirdavat') {
        if (mainTitle) mainTitle.value = 'Mekanik Bakım & Atölye Hırdavat İhtiyaçları';
        addDashRequestItemRow('M8x30mm İnbus 8.8 Çelik Cıvata', 500, 4.2, 'DIN 912 Galvanizli');
        addDashRequestItemRow('Loctite 243 Orta Mukavemet Civata Sabitleyici 50ml', 4, 490, 'Orijinal ambalaj');
        addDashRequestItemRow('Bosch 125x1.0mm Paslanmaz Kesme Taşı', 100, 38, 'Inox uyumlu');
        addDashRequestItemRow('WD-40 Çok Amaçlı Pas Sökücü Sprey 400ml', 12, 210, 'Akıllı pipetli');
    }
    recalculateDashRequestTotals();
    showAppToast('✓ Hazır örnek kalem listesi yüklendi.', true);
}

function addDashMultipleRequestItemRows(count) {
    for (var i = 0; i < count; i++) {
        addDashRequestItemRow('', 1, '', '');
    }
}

function addDashRequestItemRow(name, qty, price, desc) {
    var tbody = document.getElementById('dashRequestItemsTableBody');
    if (!tbody) return;

    var rowCount = tbody.children.length + 1;
    var tr = document.createElement('tr');
    tr.className = 'dash-item-data-row';

    var parsedQty = parseInt(qty, 10);
    if (isNaN(parsedQty) || parsedQty < 1) parsedQty = 1;

    tr.innerHTML = `
        <td style="text-align: center; font-weight: 700; color: #64748B;" class="dash-item-order-num">${rowCount}</td>
        <td>
            <input type="text" class="item-input dash-item-name" value="${escapeHtml(name || '')}" placeholder="Örn: Schneider 24V 10A Güç Kaynağı" required oninput="recalculateDashRequestTotals()" />
        </td>
        <td>
            <input type="number" class="item-input dash-item-qty" min="1" step="1" value="${parsedQty}" placeholder="Adet" required oninput="this.value = this.value.replace(/[^0-9]/g, ''); recalculateDashRequestTotals()" />
        </td>
        <td>
            <input type="number" class="item-input dash-item-price" min="0" step="any" value="${price || ''}" placeholder="0,00" oninput="recalculateDashRequestTotals()" />
        </td>
        <td>
            <span class="item-line-total-badge">—</span>
        </td>
        <td>
            <input type="text" class="item-input dash-item-desc" value="${escapeHtml(desc || '')}" placeholder="Marka, ölçü, şartname..." />
        </td>
        <td style="text-align: center; white-space: nowrap;">
            <button type="button" class="btn-remove-row" style="color: #2563EB;" onclick="cloneDashRequestItemRow(this)" title="Bu Kalemi Çoğalt">📋</button>
            <button type="button" class="btn-remove-row" onclick="removeDashRequestItemRow(this)" title="Kalemi Sil">&times;</button>
        </td>
    `;

    tbody.appendChild(tr);
    recalculateDashRequestTotals();
}

function cloneDashRequestItemRow(btn) {
    var tr = btn.closest('tr');
    if (!tr) return;
    var name = tr.querySelector('.dash-item-name')?.value || '';
    var qty = tr.querySelector('.dash-item-qty')?.value || 1;
    var price = tr.querySelector('.dash-item-price')?.value || '';
    var desc = tr.querySelector('.dash-item-desc')?.value || '';
    addDashRequestItemRow(name, qty, price, desc);
}

function removeDashRequestItemRow(btn) {
    var tbody = document.getElementById('dashRequestItemsTableBody');
    if (!tbody) return;

    var rows = tbody.querySelectorAll('.dash-item-data-row');
    if (rows.length <= 1) {
        showAppToast('Talebinizde en az 1 kalem bulunmalıdır.', false);
        return;
    }

    var tr = btn.closest('tr');
    if (tr) {
        tr.remove();
        reindexDashRequestItemRows();
        recalculateDashRequestTotals();
    }
}

function reindexDashRequestItemRows() {
    var tbody = document.getElementById('dashRequestItemsTableBody');
    if (!tbody) return;
    var rows = tbody.querySelectorAll('.dash-item-data-row');
    rows.forEach(function (row, idx) {
        var numCell = row.querySelector('.dash-item-order-num');
        if (numCell) numCell.textContent = (idx + 1);
    });
}

function recalculateDashRequestTotals() {
    var tbody = document.getElementById('dashRequestItemsTableBody');
    if (!tbody) return;

    var rows = tbody.querySelectorAll('.dash-item-data-row');
    var totalItems = rows.length;
    var totalQty = 0;
    var totalBudget = 0;
    var hasAnyBudget = false;

    rows.forEach(function (row) {
        var qtyVal = parseInt(row.querySelector('.dash-item-qty')?.value || '0', 10);
        var priceVal = parseFloat(row.querySelector('.dash-item-price')?.value || '0');
        var totalBadge = row.querySelector('.item-line-total-badge');

        if (!isNaN(qtyVal) && qtyVal > 0) {
            totalQty += qtyVal;
        }

        if (!isNaN(priceVal) && priceVal > 0 && !isNaN(qtyVal) && qtyVal > 0) {
            var lineTotal = qtyVal * priceVal;
            totalBudget += lineTotal;
            hasAnyBudget = true;
            if (totalBadge) totalBadge.textContent = Number(lineTotal).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
        } else {
            if (totalBadge) totalBadge.textContent = '—';
        }
    });

    var summaryItemsEl = document.getElementById('dashSummaryTotalItems');
    var summaryQtyEl = document.getElementById('dashSummaryTotalQty');
    var summaryBudgetEl = document.getElementById('dashSummaryTotalBudget');

    if (summaryItemsEl) summaryItemsEl.textContent = totalItems + ' Kalem';
    if (summaryQtyEl) summaryQtyEl.textContent = Number(totalQty).toLocaleString('tr-TR') + ' Adet';
    if (summaryBudgetEl) {
        if (hasAnyBudget) {
            summaryBudgetEl.textContent = Number(totalBudget).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
        } else {
            summaryBudgetEl.textContent = 'Bütçe Belirtilmedi';
        }
    }

    // Talep başlığı boşsa ilk kalemin adını placeholder yap
    var firstItemName = rows[0]?.querySelector('.dash-item-name')?.value?.trim();
    var mainTitleInput = document.getElementById('reqProductName');
    if (mainTitleInput && !mainTitleInput.value.trim() && firstItemName) {
        mainTitleInput.placeholder = firstItemName + (totalItems > 1 ? ' ve ' + (totalItems - 1) + ' diğer kalem' : '');
    }
}

function openRequestModal() {
    var modal = document.getElementById('requestModal');
    if (modal) {
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        initDashRequestItems();
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
    var form = event.target;
    var submitBtn = document.getElementById('dashCreateSubmitBtn') || form.querySelector('button[type="submit"]');
    var originalText = submitBtn ? submitBtn.innerHTML : 'Talebi ve İstenilenler Listesini Yayınla';

    var name = document.getElementById('reqProductName')?.value?.trim() || '';
    var cat = document.getElementById('reqCategory')?.value || 'Satın Alma';
    var deadline = document.getElementById('reqDeadline')?.value || null;
    var notes = document.getElementById('reqNotes')?.value || '';

    // Kalemleri Topla
    var tbody = document.getElementById('dashRequestItemsTableBody');
    var rows = tbody ? tbody.querySelectorAll('.dash-item-data-row') : [];
    var items = [];
    var totalQty = 0;
    var totalBudget = 0;

    rows.forEach(function (row, idx) {
        var itName = row.querySelector('.dash-item-name')?.value?.trim() || '';
        var qty = parseInt(row.querySelector('.dash-item-qty')?.value || '0', 10);
        var unit = 'Adet';
        var priceStr = row.querySelector('.dash-item-price')?.value;
        var price = priceStr ? parseFloat(priceStr) : null;
        var desc = row.querySelector('.dash-item-desc')?.value?.trim() || null;

        if (itName && qty > 0) {
            items.push({
                itemOrder: idx + 1,
                itemName: itName,
                quantity: qty,
                unit: 'Adet',
                estimatedUnitPrice: price,
                itemDescription: desc
            });
            totalQty += qty;
            if (price && price > 0) {
                totalBudget += (qty * price);
            }
        }
    });

    if (items.length === 0 && !name) {
        showAppToast('Lütfen en az 1 kalem malzeme/hizmet adı ve miktarı giriniz.', false);
        return;
    }

    if (!name && items.length > 0) {
        name = items[0].itemName + (items.length > 1 ? ' ve ' + (items.length - 1) + ' diğer kalem' : '');
    }

    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '⏳ İstenilenler Listesi Kaydediliyor...';
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
                quantity: totalQty > 0 ? Math.round(totalQty) : 1,
                estimatedBudget: totalBudget > 0 ? totalBudget : null,
                targetDeliveryDate: deadline,
                notes: notes,
                items: items
            })
        });

        var result = await response.json();
        if (result.success) {
            closeRequestModal();
            form.reset();
            if (tbody) tbody.innerHTML = '';
            showAppToast('✓ ' + (result.message || 'Talebiniz ve istenilenler listesi başarıyla yayınlandı!'), true);

            // Tabloya dinamik olarak en üste satır ekle
            var table = document.getElementById('buyerRequestsTable');
            if (table) {
                var tb = table.querySelector('tbody');
                if (tb) {
                    var tr = document.createElement('tr');
                    tr.setAttribute('data-status', 'Açık');
                    tr.style.animation = 'fadeInRow 0.4s ease';
                    var reqCode = 'REQ-' + (result.requestId ? ('000' + result.requestId).slice(-4) : 'YENI');
                    tr.innerHTML = '<td><div class="req-title-cell"><span class="req-code">' + reqCode + '</span><strong class="req-name">' + escapeHtml(name) + ' (' + items.length + ' Kalem)</strong><small class="req-date">Bugün</small></div></td>' +
                        '<td><span class="category-tag">' + escapeHtml(cat) + '</span></td>' +
                        '<td><strong class="budget-amount">' + (totalBudget > 0 ? Number(totalBudget).toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + ' ₺' : 'Belirtilmedi') + '</strong></td>' +
                        '<td><span class="status-badge badge-blue"><span class="status-dot"></span>Açık</span></td>' +
                        '<td class="text-right"><a href="/Requests" class="btn-table-action">İncele</a></td>';
                    tb.insertBefore(tr, tb.firstChild);
                }
            }
        } else {
            showAppToast('Hata: ' + (result.message || 'Talep kaydedilemedi.'), false);
        }
    } catch (err) {
        showAppToast('Sunucu hatası: ' + err.message, false);
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
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
    var titleEl = modal.querySelector('.modal-head h3');

    if (itemId && select) {
        var existingOpt = Array.from(select.options).find(function(o) { return o.value == itemId; });
        if (!existingOpt) {
            var newOpt = document.createElement('option');
            newOpt.value = itemId;
            newOpt.setAttribute('data-id', itemId);
            newOpt.setAttribute('data-price', currentPrice !== undefined ? currentPrice : '0');
            newOpt.setAttribute('data-stock', currentStock !== undefined ? currentStock : '0');
            newOpt.textContent = (itemName || ('Ürün #' + itemId)) + (currentPrice ? ' (Mevcut: ' + Number(currentPrice).toLocaleString('tr-TR') + ' ₺)' : '');
            select.appendChild(newOpt);
            newOpt.selected = true;
        } else {
            existingOpt.selected = true;
            if (itemName) existingOpt.textContent = itemName + (currentPrice ? ' (Mevcut: ' + Number(currentPrice).toLocaleString('tr-TR') + ' ₺)' : '');
            if (currentPrice !== undefined) existingOpt.setAttribute('data-price', currentPrice);
            if (currentStock !== undefined) existingOpt.setAttribute('data-stock', currentStock);
        }

        select.value = itemId;
        if (priceInput && currentPrice !== undefined && currentPrice !== null) priceInput.value = currentPrice;
        if (stockInput && currentStock !== undefined && currentStock !== null) stockInput.value = currentStock;
        if (hiddenIdInput) hiddenIdInput.value = itemId;
        if (titleEl) titleEl.textContent = itemName ? (itemName + ' - Fiyat & Stok Düzenle') : 'Katalog Ürün Fiyatı Güncelle';
    } else if (select && select.selectedOptions.length > 0) {
        var opt = select.selectedOptions[0];
        var p = opt.getAttribute('data-price') || opt.value;
        var s = opt.getAttribute('data-stock') || '100';
        var id = opt.getAttribute('data-id') || opt.value;
        if (priceInput && p) priceInput.value = p;
        if (stockInput && s) stockInput.value = s;
        if (hiddenIdInput) hiddenIdInput.value = id;
        if (titleEl) titleEl.textContent = 'Katalog Ürün Fiyatı Güncelle';
    }

    modal.classList.add('is-open');
    modal.setAttribute('aria-hidden', 'false');
    if (priceInput) setTimeout(function () { priceInput.focus(); priceInput.select(); }, 60);
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

    if (isNaN(newPrice) || newPrice <= 0) {
        showAppToast('Lütfen geçerli bir birim fiyat giriniz.', false);
        return;
    }

    var submitBtn = event.target.querySelector('button[type="submit"]');
    var originalText = submitBtn ? submitBtn.innerHTML : 'Fiyatı Güncelle';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '⏳ Güncelleniyor...';
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
            showAppToast('✓ ' + (result.message || 'Ürün fiyat ve stok bilgisi başarıyla güncellendi.'), true);

            // ==================== ASENKRON DOM GÜNCELLEMESİ ====================
            // 1. Listings.cshtml tablosundaki satırı canlı güncelle
            var listingRow = document.getElementById('listing-row-' + itemId);
            if (listingRow) {
                // Fiyat hücresi
                var priceCell = listingRow.querySelector('td:nth-child(4) strong');
                if (priceCell) {
                    priceCell.textContent = Number(newPrice).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' ₺';
                    priceCell.classList.add('realtime-price-flash');
                    setTimeout(function() { priceCell.classList.remove('realtime-price-flash'); }, 2000);
                }

                // Stok hücresi ve rozeti
                var stockCol = listingRow.querySelector('td:nth-child(3)');
                if (stockCol) {
                    var stockStrong = stockCol.querySelector('strong');
                    var statusBadge = stockCol.querySelector('.status-badge');
                    var isOut = newStock <= 0;
                    var isCrit = newStock > 0 && newStock <= 10;
                    var statusText = isOut ? 'Tükendi' : (isCrit ? 'Kritik' : 'Stokta');

                    if (stockStrong) {
                        stockStrong.textContent = newStock + ' Adet';
                        stockStrong.className = isOut ? 'text-danger' : (isCrit ? 'text-warning' : '');
                    }
                    if (statusBadge) {
                        statusBadge.className = 'status-badge ' + (isOut ? 'badge-coral' : (isCrit ? 'badge-amber' : 'badge-emerald'));
                        statusBadge.innerHTML = '<span class="status-dot"></span> ' + statusText;
                    }
                }

                // Düzenle butonundaki parametreleri güncelle
                var editBtn = listingRow.querySelector('.btn-table-action');
                if (editBtn) {
                    editBtn.setAttribute('onclick', "openPriceModal(" + itemId + ", '" + escapeHtml(productName).replace(/'/g, "\\'") + "', " + newPrice + ", " + newStock + ")");
                }

                listingRow.classList.add('realtime-updated');
                setTimeout(function() { listingRow.classList.remove('realtime-updated'); }, 2400);
            }

            // 2. Supplier.cshtml tablosundaki satırı canlı güncelle (varsa)
            var suppRow = document.getElementById('featured-prod-row-' + itemId);
            if (suppRow) {
                var sPrice = suppRow.querySelector('.featured-price-val');
                var sStock = suppRow.querySelector('.featured-stock-val');
                if (sPrice) {
                    sPrice.textContent = Number(newPrice).toLocaleString('tr-TR') + ' ₺';
                    sPrice.classList.add('realtime-price-flash');
                }
                if (sStock) sStock.textContent = newStock + ' Adet';
            }

            // 3. Option data niteliklerini güncelle
            if (opt) {
                opt.setAttribute('data-price', newPrice);
                opt.setAttribute('data-stock', newStock);
            }
        } else {
            showAppToast('Hata: ' + (result.message || 'Fiyat güncellenemedi.'), false);
        }
    } catch (err) {
        showAppToast('Sunucu bağlantı hatası: ' + err.message, false);
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
    }
}

// --- TEDARİKÇİ (SUPPLIER) ÇOKLU KALEM TEKLİF VERME MODALI ---
var activeBidRequestData = null;

async function openOfferModal(rfqCodeOrId, productName, currentBid, currentDays, currentNotes) {
    // 1. Sayısal ID veya REQ-0001 formatını ayıkla
    var reqId = 0;
    if (typeof rfqCodeOrId === 'string') {
        var digits = rfqCodeOrId.replace(/\D/g, '');
        reqId = parseInt(digits, 10) || 0;
    } else {
        reqId = parseInt(rfqCodeOrId, 10) || 0;
    }

    if (reqId <= 0) {
        showAppToast('Hata: Geçersiz talep numarası.', false);
        return;
    }

    var modal = document.getElementById('supplierBidModal');
    if (!modal) {
        // Fallback: Eski modal varsa
        var oldModal = document.getElementById('offerModal');
        if (oldModal) {
            oldModal.classList.add('is-open');
            oldModal.setAttribute('aria-hidden', 'false');
        }
        return;
    }

    var reqInput = document.getElementById('bidRequestID');
    var prodTitle = document.getElementById('bidProductTitle');
    var reqBadge = document.getElementById('bidReqCodeBadge');
    var buyerSub = document.getElementById('bidBuyerInfoSub');
    var deadlineSub = document.getElementById('bidDeadlineBudgetSub');
    var daysInput = document.getElementById('bidDeliveryDays');
    var notesInput = document.getElementById('bidSupplierNotes');
    var titleText = document.getElementById('bidModalTitleText');
    var submitBtn = document.getElementById('bidSubmitBtn');
    var tableBody = document.getElementById('bidItemsTableBody');
    var countBadge = document.getElementById('bidItemsCountBadge');

    if (reqInput) reqInput.value = reqId;
    if (prodTitle) prodTitle.textContent = productName || ('Talep #' + reqId);
    if (daysInput) daysInput.value = currentDays ? parseInt(currentDays, 10) : 7;
    if (notesInput) notesInput.value = currentNotes || '';

    if (currentBid && currentBid !== '' && parseFloat(currentBid) > 0) {
        if (titleText) titleText.textContent = 'Teklifi Kalem Kalem Revize Et / Düzenle';
        if (submitBtn) submitBtn.textContent = 'Teklifi Güncelle & Gönder';
    } else {
        if (titleText) titleText.textContent = 'Talebe Kalem Bazlı Fiyat Teklifi Ver';
        if (submitBtn) submitBtn.textContent = 'Teklifi İlet';
    }

    if (tableBody) {
        tableBody.innerHTML = '<tr><td colspan="7" style="text-align: center; padding: 25px; color: #64748B;">Kalem bilgileri yükleniyor...</td></tr>';
    }

    modal.classList.add('is-open');
    modal.setAttribute('aria-hidden', 'false');

    try {
        var res = await fetch('/Requests/GetRequestDetails/' + reqId);
        var data = await res.json();

        if (!data.success) {
            showAppToast('Talep detayları alınamadı: ' + data.message, false);
            closeBidModal();
            return;
        }

        activeBidRequestData = data;

        if (reqBadge) reqBadge.textContent = data.requestCode + ' · ' + data.requestDate;
        if (prodTitle) prodTitle.textContent = data.productName;
        if (buyerSub) buyerSub.textContent = 'Alıcı: ' + data.buyerInfo + ' (' + data.requestType + ')' + (data.notes ? ' · Şartname: ' + data.notes : '');
        if (deadlineSub) deadlineSub.textContent = 'Termin: ' + data.targetDeliveryDate + ' · Hedef Bütçe: ' + data.estimatedBudgetFormatted;
        if (countBadge) countBadge.textContent = (data.items ? data.items.length : 0) + ' Kalem';

        if (data.myOffer) {
            if (daysInput && data.myOffer.deliveryDays) daysInput.value = data.myOffer.deliveryDays;
            if (notesInput && data.myOffer.supplierNotes) notesInput.value = data.myOffer.supplierNotes;
            var valSelect = document.getElementById('bidValidityDays');
            if (valSelect && data.myOffer.validityDays) valSelect.value = data.myOffer.validityDays;
        }

        // Kalem tablosunu oluştur
        var myOfferItems = (data.myOffer && data.myOffer.offerItems) ? data.myOffer.offerItems : [];
        var html = '';

        if (data.items && data.items.length > 0) {
            data.items.forEach(function (it, idx) {
                var prevOi = myOfferItems.find(function (oi) { return oi.requestItemId === it.requestItemId; });
                var prevPrice = prevOi ? prevOi.unitPrice : '';
                var prevNote = prevOi ? (prevOi.notes || '') : '';
                var buyerRef = it.estimatedUnitPriceFormatted ? it.estimatedUnitPriceFormatted : '—';
                var itemDesc = it.itemDescription ? '<div style="font-size: 0.75rem; color: #64748B; margin-top: 2px;">' + escapeHtml(it.itemDescription) + '</div>' : '';

                html += '<tr class="bid-item-row" data-req-item-id="' + it.requestItemId + '" data-qty="' + it.quantity + '" style="border-bottom: 1px solid #F1F5F9;">';
                html += '  <td style="text-align: center; font-weight: 700; color: #64748B; padding: 8px;">' + (it.itemOrder || (idx + 1)) + '</td>';
                html += '  <td style="padding: 8px;">';
                html += '    <strong style="color: #0F172A; font-size: 0.86rem;">' + escapeHtml(it.itemName) + '</strong>';
                html +=      itemDesc;
                html += '  </td>';
                html += '  <td style="padding: 8px;"><span style="font-weight: 700; color: #2563EB;">' + Number(it.quantity).toLocaleString('tr-TR') + '</span> ' + escapeHtml(it.unit) + '</td>';
                html += '  <td style="font-size: 0.78rem; color: #64748B; padding: 8px;">' + buyerRef + '</td>';
                html += '  <td style="padding: 8px;">';
                html += '    <input type="number" class="bid-unit-price-input form-control" style="font-weight: 600; padding: 4px 8px; font-size: 0.85rem;" min="0" step="any" placeholder="0,00" value="' + (prevPrice !== '' ? prevPrice : '') + '" required oninput="recalculateBidTotals()" />';
                html += '  </td>';
                html += '  <td style="padding: 8px;">';
                html += '    <span class="bid-line-total-cell" style="font-weight: 700; color: #059669;">0,00 ₺</span>';
                html += '  </td>';
                html += '  <td style="padding: 8px;">';
                html += '    <input type="text" class="bid-item-note form-control" style="font-size: 0.80rem; padding: 4px 8px;" value="' + escapeHtml(prevNote) + '" placeholder="Marka / Stok notu" />';
                html += '  </td>';
                html += '</tr>';
            });
        } else {
            html = '<tr><td colspan="7" style="text-align: center; padding: 20px; color: #64748B;">Bu talep için malzeme kalemi bulunamadı.</td></tr>';
        }

        if (tableBody) tableBody.innerHTML = html;
        recalculateBidTotals();

        // İlk inputa odaklan
        var firstInput = tableBody ? tableBody.querySelector('.bid-unit-price-input') : null;
        if (firstInput) setTimeout(function () { firstInput.focus(); }, 80);

    } catch (e) {
        if (tableBody) tableBody.innerHTML = '<tr><td colspan="7" style="color: red; text-align: center; padding: 20px;">Hata: ' + e.message + '</td></tr>';
    }
}

function recalculateBidTotals() {
    var tableBody = document.getElementById('bidItemsTableBody');
    if (!tableBody) return;

    var rows = tableBody.querySelectorAll('.bid-item-row');
    var totalBid = 0;
    var pricedCount = 0;
    var totalRowsCount = rows.length;

    rows.forEach(function (row) {
        var qty = parseFloat(row.getAttribute('data-qty') || '1');
        var priceInput = row.querySelector('.bid-unit-price-input');
        var totalCell = row.querySelector('.bid-line-total-cell');
        var price = priceInput ? parseFloat(priceInput.value || '0') : 0;

        if (!isNaN(price) && price > 0) {
            var lineTotal = qty * price;
            totalBid += lineTotal;
            pricedCount++;
            if (totalCell) totalCell.textContent = Number(lineTotal).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
        } else {
            if (totalCell) totalCell.textContent = '0,00 ₺';
        }
    });

    var hiddenTotalInput = document.getElementById('bidOfferedPrice');
    var bigTotalDisplay = document.getElementById('bidCalculatedTotal');
    var pricedSummary = document.getElementById('bidPricedItemsSummary');

    if (hiddenTotalInput) hiddenTotalInput.value = totalBid;
    if (bigTotalDisplay) bigTotalDisplay.textContent = Number(totalBid).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
    if (pricedSummary) pricedSummary.textContent = pricedCount + ' / ' + totalRowsCount + ' Kalem Fiyatlandırıldı';
}

function closeBidModal() {
    var modal = document.getElementById('supplierBidModal');
    if (modal) {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
    }
    var oldModal = document.getElementById('offerModal');
    if (oldModal) {
        oldModal.classList.remove('is-open');
        oldModal.setAttribute('aria-hidden', 'true');
    }
}

function closeOfferModal() {
    closeBidModal();
}

async function handleBidSubmit(event) {
    event.preventDefault();
    var form = event.target;
    var submitBtn = document.getElementById('bidSubmitBtn') || form.querySelector('button[type="submit"]');
    var originalText = submitBtn ? submitBtn.innerHTML : 'Teklifi İlet';

    var rawReqId = document.getElementById('bidRequestID')?.value || '0';
    var reqId = parseInt(rawReqId.replace(/\D/g, ''), 10) || 0;
    var days = parseInt(document.getElementById('bidDeliveryDays')?.value || '7', 10);
    var valDays = parseInt(document.getElementById('bidValidityDays')?.value || '30', 10);
    var notes = document.getElementById('bidSupplierNotes')?.value || '';

    // Kalem bazlı teklifleri topla
    var tableBody = document.getElementById('bidItemsTableBody');
    var rows = tableBody ? tableBody.querySelectorAll('.bid-item-row') : [];
    var offerItems = [];
    var grandTotal = 0;
    var unpricedCount = 0;

    rows.forEach(function (row) {
        var reqItemId = parseInt(row.getAttribute('data-req-item-id') || '0', 10);
        var qty = parseFloat(row.getAttribute('data-qty') || '1');
        var price = parseFloat(row.querySelector('.bid-unit-price-input')?.value || '0');
        var itemNote = row.querySelector('.bid-item-note')?.value?.trim() || null;

        if (isNaN(price) || price <= 0) {
            unpricedCount++;
        } else {
            offerItems.push({
                requestItemID: reqItemId,
                unitPrice: price,
                notes: itemNote
            });
            grandTotal += (price * qty);
        }
    });

    if (offerItems.length === 0) {
        showAppToast('Lütfen en az bir kalem için geçerli bir birim fiyat teklifi giriniz.', false);
        return;
    }

    if (unpricedCount > 0) {
        if (!confirm('Dikkat: ' + unpricedCount + ' adet kaleme fiyat girmediniz. Sadece fiyat girdiğiniz ' + offerItems.length + ' kalem üzerinden toplam ' + Number(grandTotal).toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + ' ₺ teklif vermek istiyor musunuz?')) {
            return;
        }
    }

    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '⏳ Kalem Teklifleri İletiliyor...';
    }

    try {
        var response = await fetch('/Requests/SubmitOfferAjax', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                requestId: reqId,
                offeredPrice: grandTotal,
                deliveryDays: days,
                validityDays: valDays,
                supplierNotes: notes,
                items: offerItems
            })
        });

        var result = await response.json();
        if (result.success) {
            closeBidModal();

            if (typeof updatePortalDomForOfferSubmitted === 'function') {
                updatePortalDomForOfferSubmitted({
                    requestId: reqId,
                    offeredPrice: grandTotal,
                    offeredPriceFormatted: Number(grandTotal).toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + ' ₺',
                    deliveryDays: days,
                    validityDays: valDays,
                    supplierNotes: notes,
                    isMyBid: true
                });
            }

            showAppToast('✓ ' + (result.message || 'Kalem bazlı teklifiniz başarıyla iletildi!'), true);

            // Eğer Talep Detay Modalı o an açıksa canlı güncelle
            var detailModal = document.getElementById('detailRequestModal');
            if (detailModal && detailModal.classList.contains('is-open') && typeof openDetailModal === 'function') {
                openDetailModal(reqId);
            }

            // Eğer Kıyaslama Modalı açıksa canlı güncelle
            var cmpModal = document.getElementById('priceCompareModal');
            if (cmpModal && cmpModal.classList.contains('is-open') && typeof openCompareModal === 'function') {
                openCompareModal(reqId);
            }
        } else {
            showAppToast('Teklif iletilemedi: ' + (result.message || 'Bilinmeyen hata.'), false);
        }
    } catch (err) {
        showAppToast('Sunucu bağlantı hatası: ' + err.message, false);
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
    }
}

async function handleOfferSubmit(event) {
    return handleBidSubmit(event);
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

function filterPurchasedTable(status, btn) {
    if (btn) {
        var parent = btn.parentElement;
        if (parent) {
            parent.querySelectorAll('.tab-pill, .year-pill').forEach(function (p) { p.classList.remove('is-active'); });
            btn.classList.add('is-active');
        }
    }

    var table = document.getElementById('buyerPurchasedProductsTable');
    if (!table) return;

    var rows = table.querySelectorAll('tbody tr[data-status]');
    var visibleCount = 0;
    rows.forEach(function (row) {
        var rowStatus = row.getAttribute('data-status') || '';
        if (status === 'all' || status === 'Tümü' || rowStatus === status || 
            (status === 'Kargoda' && (rowStatus === 'Kargoya Verildi' || rowStatus === 'Kargoda')) ||
            (status === 'Teslim Edildi' && (rowStatus === 'Teslim Edildi' || rowStatus === 'Tamamlandı'))) {
            row.style.display = '';
            visibleCount++;
        } else {
            row.style.display = 'none';
        }
    });

    var emptyRow = document.getElementById('buyerPurchasedEmptySearchRow');
    if (rows.length > 0 && visibleCount === 0) {
        if (!emptyRow) {
            emptyRow = document.createElement('tr');
            emptyRow.id = 'buyerPurchasedEmptySearchRow';
            emptyRow.innerHTML = '<td colspan="8" style="text-align: center; padding: 32px 16px; color: #64748b;">Seçilen duruma uygun satın alınan ürün bulunamadı.</td>';
            table.querySelector('tbody').appendChild(emptyRow);
        }
        emptyRow.style.display = '';
    } else if (emptyRow) {
        emptyRow.style.display = 'none';
    }
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

// --- DÖNEM / YIL ASENKRON DEĞİŞTİRME (AJAX) ---
async function switchDashboardYear(year, event) {
    if (event) {
        event.preventDefault();
    }

    var currentUrl = new URL(window.location.href);
    currentUrl.searchParams.set('yil', year);

    // Aktif buton görselini hemen güncelle
    var nav = (event && event.target) ? event.target.closest('.year-filter') : document.querySelector('.year-filter');
    if (nav) {
        nav.querySelectorAll('.year-pill').forEach(function (p) { p.classList.remove('is-active'); });
        if (event && event.target) {
            var targetBtn = event.target.classList.contains('year-pill') ? event.target : event.target.closest('.year-pill');
            if (targetBtn) targetBtn.classList.add('is-active');
        }
    }

    var mainContent = document.querySelector('.dash-content') || document.querySelector('main') || document.querySelector('#dashboardMainWrap');
    if (!mainContent) {
        window.location.href = currentUrl.toString();
        return;
    }

    mainContent.style.opacity = '0.5';
    mainContent.style.transition = 'opacity 0.2s ease';

    try {
        var response = await fetch(currentUrl.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        if (!response.ok) throw new Error('HTTP ' + response.status);

        var htmlText = await response.text();
        var parser = new DOMParser();
        var newDoc = parser.parseFromString(htmlText, 'text/html');

        var newContent = newDoc.querySelector('.dash-content') || newDoc.querySelector('main') || newDoc.querySelector('#dashboardMainWrap');
        if (newContent) {
            mainContent.innerHTML = newContent.innerHTML;
            window.history.pushState({ year: year }, '', currentUrl.toString());

            // Tooltipleri ve grafikleri yeniden bağla
            initDashboardTooltips();
        } else {
            window.location.href = currentUrl.toString();
        }
    } catch (err) {
        console.error('Asenkron yıl yükleme hatası:', err);
        window.location.href = currentUrl.toString();
    } finally {
        mainContent.style.opacity = '1';
    }
}

function initDashboardTooltips() {
    var targets = document.querySelectorAll('[data-tip-title]');
    var tooltip = document.querySelector('.viz-tooltip');
    if (!tooltip) return;

    targets.forEach(function (element) {
        element.addEventListener('mouseenter', function (event) {
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
            tooltip.classList.add('is-visible');
        });
        element.addEventListener('mouseleave', function () {
            tooltip.classList.remove('is-visible');
        });
    });
}

// --- BİLDİRİM AÇILIR MENÜSÜ ---
function toggleNotificationDropdown() {
    showAppToast('🔔 2 yeni bildiriminiz bulunmaktadır: 1 yeni teklif onaylandı, 1 RFQ süresi dolmak üzere.');
}

// --- KLAVYE ESC TUŞU İLE MODAL KAPATMA ---
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        if (typeof closeRequestModal === 'function') closeRequestModal();
        if (typeof closePriceModal === 'function') closePriceModal();
        var openModals = document.querySelectorAll('.app-modal.is-open, .custom-modal-backdrop.is-open');
        openModals.forEach(function (m) {
            m.classList.remove('is-open');
            m.setAttribute('aria-hidden', 'true');
        });
    }
});
