/**
 * Satın Alma Portalı - Gerçek Zamanlı (SignalR) İstemci Yöneticisi
 * Sayfa yenilemelerine gerek kalmadan tüm veri değişimlerini ekrana anında yansıtır.
 */
(function () {
    'use strict';

    // 1. SignalR Kütüphane Kontrolü
    if (typeof signalR === 'undefined') {
        console.warn('[SignalR] signalR kütüphanesi yüklenmedi, CDN üzerinden yükleniyor...');
        var script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js';
        script.onload = initRealtimeHub;
        document.head.appendChild(script);
    } else {
        initRealtimeHub();
    }

    function initRealtimeHub() {
        if (typeof signalR === 'undefined') return;

        // Hub Bağlantısı Oluştur (Otomatik Yeniden Bağlanma Özellikli)
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/portalHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Global erişim için window'a bağla
        window.portalHubConnection = connection;

        // =========================================================================
        // 2. SIGNALR EVENT DİNLEYİCİLERİ
        // =========================================================================

        // A) YENİ TALEP OLUŞTURULDUĞUNDA (OnRequestCreated)
        connection.on("OnRequestCreated", function (data) {
            console.log("[SignalR] Yeni Talep Alındı:", data);

            // Toast Bildirimi Göster
            showRealtimeToast('📋 Yeni Satın Alma Talebi: ' + data.requestCode + ' - ' + data.productName, 'info');

            // 1. Requests/Index Tablosuna Satır Ekle
            var reqTbody = document.getElementById('requestsTableBody') || 
                           (document.querySelector('.requests-table tbody'));
            if (reqTbody) {
                var newTr = document.createElement('tr');
                newTr.className = 'realtime-updated';
                newTr.setAttribute('data-request-id', data.requestId);
                newTr.setAttribute('data-status', data.status);
                newTr.setAttribute('data-category', data.category);

                newTr.innerHTML = 
                    '<td><span class="req-id-badge">' + escapeHtml(data.requestCode) + '</span></td>' +
                    '<td><div class="req-title-wrap"><strong class="req-name">' + escapeHtml(data.productName) + '</strong>' +
                    (data.notes ? '<small class="req-desc-sub">' + escapeHtml(data.notes.substring(0, 55)) + (data.notes.length > 55 ? '...' : '') + '</small>' : '') +
                    '</div></td>' +
                    '<td><span class="category-badge-pill">' + escapeHtml(data.category) + '</span></td>' +
                    '<td><div class="company-badge-wrap"><span class="company-name-text">' + escapeHtml(data.buyerInfo) + '</span></div></td>' +
                    '<td><strong class="qty-text">' + Number(data.quantity).toLocaleString('tr-TR') + ' Adet</strong></td>' +
                    '<td><span class="price-val text-muted">' + (data.estimatedBudgetFormatted || 'Belirtilmedi') + '</span></td>' +
                    '<td><span class="offer-count-badge is-zero"><span class="dot"></span>0 Teklif</span></td>' +
                    '<td><span class="badge-status is-pending">⏳ ' + escapeHtml(data.status) + '</span></td>' +
                    '<td><span class="date-text">' + escapeHtml(data.requestDate) + '</span></td>' +
                    '<td class="text-right"><div class="action-btn-group">' +
                    '<button type="button" class="btn-action-primary" onclick="openDetailModal(' + data.requestId + ')">İncele & Teklif</button>' +
                    '</div></td>';

                reqTbody.insertBefore(newTr, reqTbody.firstChild);
            }

            // 2. Buyer Dashboard Tablosuna Ekle
            var buyerTbody = document.querySelector('#buyerRequestsTable tbody');
            if (buyerTbody) {
                var bTr = document.createElement('tr');
                bTr.className = 'realtime-updated';
                bTr.setAttribute('data-status', data.status);
                bTr.innerHTML = 
                    '<td><div class="req-title-cell"><span class="req-code">' + escapeHtml(data.requestCode) + '</span>' +
                    '<strong class="req-name">' + escapeHtml(data.productName) + '</strong>' +
                    '<small class="req-date">' + escapeHtml(data.requestDate) + '</small></div></td>' +
                    '<td><span class="category-tag">' + escapeHtml(data.category) + '</span></td>' +
                    '<td><strong class="budget-amount">' + (data.estimatedBudgetFormatted || 'Belirtilmedi') + '</strong></td>' +
                    '<td><span class="status-badge badge-blue"><span class="status-dot"></span>' + escapeHtml(data.status) + '</span></td>' +
                    '<td class="text-right"><a href="/Requests" class="btn-table-action">İncele</a></td>';
                buyerTbody.insertBefore(bTr, buyerTbody.firstChild);
            }

            // 3. Supplier RFQ Tablosuna Ekle
            var suppTbody = document.querySelector('#supplierRfqTable tbody');
            if (suppTbody) {
                var sTr = document.createElement('tr');
                sTr.className = 'realtime-updated';
                sTr.setAttribute('data-status', data.status);
                sTr.innerHTML = 
                    '<td><div class="req-title-cell"><span class="req-code">' + escapeHtml(data.requestCode) + '</span>' +
                    '<strong class="req-name">' + escapeHtml(data.productName) + '</strong>' +
                    '<small class="req-date">' + escapeHtml(data.buyerInfo) + '</small></div></td>' +
                    '<td><span class="category-tag">' + escapeHtml(data.category) + '</span></td>' +
                    '<td><strong class="budget-amount">' + (data.estimatedBudgetFormatted || 'Belirtilmedi') + '</strong></td>' +
                    '<td><span class="status-badge badge-blue"><span class="status-dot"></span>Açık İhale</span></td>' +
                    '<td class="text-right"><button type="button" class="btn-table-action" onclick="openOfferModal(\'' + data.requestCode + '\', \'' + escapeHtml(data.productName) + '\')">Teklif Ver</button></td>';
                suppTbody.insertBefore(sTr, suppTbody.firstChild);
            }

            // 4. Sayaçları (KPI & Tab Badge) Artır
            incrementCounter('totalKpiCount');
            incrementCounter('pendingKpiCount');
            incrementCounter('allRequestsTabCount');
        });

        // B) YENİ TEKLİF VERİLDİĞİNDE VEYA REVİZE EDİLDİĞİNDE (OnOfferSubmitted)
        connection.on("OnOfferSubmitted", function (data) {
            console.log("[SignalR] Yeni Teklif Düştü:", data);
            updatePortalDomForOfferSubmitted(data);

            // Eğer Talep Detay Modalı O Anda Bu Talep İçin Açıksa, Listeyi Dinamik Tazele
            var modal = document.getElementById('detailRequestModal');
            if (modal && modal.classList.contains('is-open')) {
                var currentModalReqId = modal.getAttribute('data-active-request-id');
                if (currentModalReqId == data.requestId) {
                    if (typeof openDetailModal === 'function') {
                        openDetailModal(data.requestId);
                    }
                }
            }

            // Eğer Fiyat Kıyaslama Modalı O Anda Bu Talep İçin Açıksa, Kıyaslama Tablosunu & Grafiği Tazele
            var cmpModal = document.getElementById('priceCompareModal');
            if (cmpModal && cmpModal.classList.contains('is-open')) {
                if (typeof openCompareModal === 'function') {
                    openCompareModal(data.requestId);
                }
            }
        });

        connection.on("OnRequestOfferCountUpdated", function (data) {
            console.log("[SignalR] Talep Teklif Sayacı Güncellendi:", data);
            updatePortalDomForOfferSubmitted(data);
        });

        // C) BİR FİRMAYA ÖZEL TEKLİF BİLDİRİMİ GELDİĞİNDE (OnOfferReceivedNotification)
        connection.on("OnOfferReceivedNotification", function (data) {
            var msg = data.isRevised 
                ? '🔄 ' + data.supplierName + ' firmanızın #' + data.requestId + ' (' + data.productName + ') talebi için teklifini ' + data.offeredPriceFormatted + ' olarak REVİZE ETTİ!'
                : '💰 ' + data.supplierName + ' firmanızın #' + data.requestId + ' (' + data.productName + ') talebine ' + data.offeredPriceFormatted + ' tutarında yeni teklif sundu!';
            showRealtimeToast(msg, 'success');
        });

        // D) TEKLİF KABUL EDİLDİĞİNDE / KAZANILDIĞINDA (OnOfferWonNotification)
        connection.on("OnOfferWonNotification", function (data) {
            showRealtimeToast(data.message, 'success');
            updatePortalDomForOfferApproved(data);
        });

        // E) TEKLİF REDDEDİLDİĞİNDE (OnOfferLostNotification)
        connection.on("OnOfferLostNotification", function (data) {
            showRealtimeToast('ℹ️ ' + data.requestCode + ' (' + data.productName + ') talebine verdiğiniz teklif güncellendi: ' + data.rejectionReason, 'info');
            updatePortalDomForOfferRejected(data);
        });

        // F) TALEP ONAYLANDIĞINDA (OnRequestApproved & OnRequestStatusChanged)
        connection.on("OnRequestApproved", function (data) {
            updatePortalDomForOfferApproved(data);
        });

        connection.on("OnRequestStatusChanged", function (data) {
            updatePortalDomForOfferApproved(data);
        });

        connection.on("OnOfferRejected", function (data) {
            updatePortalDomForOfferRejected(data);
        });

        // G) TALEP SİLİNDİĞİNDE (OnRequestDeleted)
        connection.on("OnRequestDeleted", function (data) {
            var row = document.querySelector('tr[data-request-id="' + data.requestId + '"]');
            if (row) {
                row.style.transition = 'all 0.4s ease';
                row.style.opacity = '0';
                row.style.transform = 'scale(0.95)';
                setTimeout(function () {
                    row.remove();
                }, 400);
                showRealtimeToast('🗑️ #' + data.requestId + ' numaralı talep sistemden kaldırıldı.', 'info');
            }
        });

        // H) ÜRÜN FİYATI VEYA STOK GÜNCELLENDİĞİNDE (OnProductPriceChanged)
        connection.on("OnProductPriceChanged", function (data) {
            console.log("[SignalR] Ürün Fiyat/Stok Güncellendi:", data);

            // 1. İlanlar Tablosundaki Fiyatı & Stoğu Güncelle
            var priceCells = document.querySelectorAll('[data-item-price-id="' + data.itemId + '"], tr[data-item-id="' + data.itemId + '"] .item-price-cell');
            priceCells.forEach(function (el) {
                el.textContent = data.newUnitPriceFormatted;
                el.classList.remove('realtime-price-flash');
                void el.offsetWidth;
                el.classList.add('realtime-price-flash');
            });

            var stockCells = document.querySelectorAll('[data-item-stock-id="' + data.itemId + '"], tr[data-item-id="' + data.itemId + '"] .item-stock-cell');
            stockCells.forEach(function (el) {
                el.textContent = Number(data.newStock).toLocaleString('tr-TR') + ' Adet';
            });

            var statusBadges = document.querySelectorAll('[data-item-status-id="' + data.itemId + '"], tr[data-item-id="' + data.itemId + '"] .item-status-badge');
            statusBadges.forEach(function (badge) {
                badge.textContent = data.status;
                if (data.status === 'Stokta') badge.className = 'status-badge badge-green item-status-badge';
                else if (data.status === 'Kritik') badge.className = 'status-badge badge-orange item-status-badge';
                else badge.className = 'status-badge badge-red item-status-badge';
            });

            // 2. Vitrin Kartlarındaki Fiyatı Güncelle
            var vitrinCards = document.querySelectorAll('.b2b-product-card[data-product-id="' + data.itemId + '"], [data-item-card-id="' + data.itemId + '"]');
            vitrinCards.forEach(function (card) {
                var pVal = card.querySelector('.b2b-price-value, .product-price-val');
                if (pVal) {
                    pVal.textContent = data.newUnitPriceFormatted;
                    pVal.classList.add('realtime-price-flash');
                }
            });
        });

        // I) ÜRÜN İLANI SİLİNDİĞİNDE (OnProductDeleted)
        connection.on("OnProductDeleted", function (data) {
            var row = document.querySelector('tr[data-item-id="' + data.itemId + '"]');
            if (row) {
                row.style.opacity = '0';
                setTimeout(function () { row.remove(); }, 350);
            }
        });

        // J) YENİ SİPARİŞ VERİLDİĞİNDE (OnOrderPlaced)
        connection.on("OnOrderPlaced", function (data) {
            console.log("[SignalR] Yeni Sipariş Geldi:", data);
            showRealtimeToast('🎉 Yeni Sipariş: ' + data.orderNumber + ' (' + data.grandTotalFormatted + ') - ' + data.buyerCompanyName, 'success');

            // Tedarikçi Siparişler Tablosuna Satır Ekle
            var suppOrdersTbody = document.querySelector('#supplierOrdersTable tbody');
            if (suppOrdersTbody) {
                var tr = document.createElement('tr');
                tr.className = 'realtime-updated';
                tr.setAttribute('data-order-id', data.orderId);
                tr.setAttribute('data-status', data.orderStatus);
                tr.innerHTML = 
                    '<td><div class="order-number-cell"><strong class="order-code">' + escapeHtml(data.orderNumber) + '</strong><small class="order-date">Bugün</small></div></td>' +
                    '<td><strong class="buyer-name">' + escapeHtml(data.buyerCompanyName) + '</strong></td>' +
                    '<td><span class="first-item-name">' + escapeHtml(data.firstItemTitle) + (data.itemCount > 1 ? ' <small>(+' + (data.itemCount - 1) + ' kalem)</small>' : '') + '</span></td>' +
                    '<td><strong class="grand-total-amount">' + escapeHtml(data.grandTotalFormatted) + '</strong></td>' +
                    '<td><span class="status-badge badge-orange"><span class="status-dot"></span>' + escapeHtml(data.orderStatus) + '</span></td>' +
                    '<td><span class="badge-payment is-paid">' + escapeHtml(data.paymentStatus) + '</span></td>' +
                    '<td class="text-right"><a href="/Dashboard/SupplierOrders" class="btn-table-action">Yönet</a></td>';
                suppOrdersTbody.insertBefore(tr, suppOrdersTbody.firstChild);
            }
        });

        // K) SİPARİŞ DURUMU & KARGO TAKİP NO GÜNCELLENDİĞİNDE (OnOrderStatusChanged)
        connection.on("OnOrderStatusChanged", function (data) {
            console.log("[SignalR] Sipariş Durumu Güncellendi:", data);

            var rows = document.querySelectorAll('tr[data-order-id="' + data.orderId + '"]');
            rows.forEach(function (row) {
                row.classList.add('realtime-updated');
                var statusBadge = row.querySelector('.status-badge, .badge-status, [data-order-status-badge]');
                if (statusBadge) {
                    statusBadge.textContent = data.status;
                    if (data.status === 'Kargoya Verildi' || data.status === 'Teslim Edildi' || data.status === 'Tamamlandı') {
                        statusBadge.className = 'status-badge badge-green';
                    } else if (data.status === 'Hazırlanıyor') {
                        statusBadge.className = 'status-badge badge-blue';
                    } else {
                        statusBadge.className = 'status-badge badge-orange';
                    }
                }

                var trackingCell = row.querySelector('.tracking-number-cell, [data-tracking-number]');
                if (trackingCell && data.trackingNumber) {
                    trackingCell.textContent = (data.shippingCarrier ? data.shippingCarrier + ' - ' : '') + data.trackingNumber;
                    trackingCell.classList.add('realtime-price-flash');
                }
            });
        });

        // L) KATEGORİ DEĞİŞİMİ (OnCategoryCreated, OnCategoryUpdated, OnCategoryDeleted)
        connection.on("OnCategoryCreated", function (data) {
            console.log("[SignalR] Yeni Kategori:", data);
            // Dropdown'lara yeni opsiyon ekle
            var selects = document.querySelectorAll('select[name="CategoryID"], select#reqCategory, select#categoryFilter');
            selects.forEach(function (sel) {
                var opt = document.createElement('option');
                opt.value = data.categoryId;
                opt.textContent = data.categoryName;
                sel.appendChild(opt);
            });
        });

        // =========================================================================
        // 3. BAĞLANTIYI BAŞLAT
        // =========================================================================
        connection.start()
            .then(function () {
                console.log("⚡ [SignalR] Satın Alma Portalı Gerçek Zamanlı Hub Bağlantısı Başarılı!");
            })
            .catch(function (err) {
                console.error("❌ [SignalR] Bağlantı Hatası:", err.toString());
            });
    }

    // =============================================================================
    // YARDIMCI BİLDİRİM VE SAYAÇ FONKSİYONLARI
    // =============================================================================

    var realtimeToastTimer = null;

    function showRealtimeToast(message, type) {
        if (!message) return;
        type = type || 'info';

        var toast = document.getElementById('appToast') || document.getElementById('realtimeToast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'realtimeToast';
            toast.className = 'app-toast';
            toast.setAttribute('role', 'alert');
            toast.setAttribute('aria-live', 'polite');
            document.body.appendChild(toast);
        }

        if (realtimeToastTimer) {
            clearTimeout(realtimeToastTimer);
            realtimeToastTimer = null;
        }

        toast.textContent = message;
        toast.style.background = (type === 'success') ? '#059669' : ((type === 'error') ? '#DC2626' : '#0F172A');
        toast.style.color = '#FFFFFF';
        toast.style.border = '1px solid ' + ((type === 'success') ? '#10B981' : ((type === 'error') ? '#EF4444' : '#334155'));
        toast.style.boxShadow = '0 10px 30px rgba(0, 0, 0, 0.35)';
        toast.classList.remove('is-show');
        void toast.offsetWidth; // force reflow
        toast.classList.add('is-show');

        realtimeToastTimer = setTimeout(function () {
            toast.classList.remove('is-show');
        }, 3800);
    }

    // =============================================================================
    // DİNAMİK BUTON, TABLO VE DURUM GÜNCELLEYİCİ FONKSİYONLAR
    // =============================================================================

    function updatePortalDomForOfferSubmitted(data) {
        if (!data || !data.requestId) return;
        var reqId = data.requestId;

        // 1. Requests/Index Tablosundaki Satırlar
        var rows = document.querySelectorAll('tr[data-request-id="' + reqId + '"]');
        rows.forEach(function (row) {
            row.classList.remove('realtime-updated');
            void row.offsetWidth;
            row.classList.add('realtime-updated');

            // A) Teklif Butonu / Sayısı (offers-cell-wrap)
            var offerWrap = row.querySelector('.offers-cell-wrap');
            var offerCount = data.totalOffersCount || data.offerCount || 1;
            var bestPriceFormatted = data.bestOfferPrice 
                ? Number(data.bestOfferPrice).toLocaleString('tr-TR', { maximumFractionDigits: 2 }) + ' ₺' 
                : (data.offeredPriceFormatted || '');

            if (offerWrap) {
                var bestPriceSmall = bestPriceFormatted ? ' <small class="pill-best-price-text" style="opacity: 0.85; font-weight: 700;">(' + bestPriceFormatted + ')</small>' : '';
                offerWrap.innerHTML = 
                    '<button type="button" class="offers-interactive-pill" data-offer-pill-id="' + reqId + '" onclick="openDetailModal(' + reqId + ')" title="Teklifleri İncele">' +
                    '  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"></polyline></svg>' +
                    '  <span class="pill-offer-count-text">' + offerCount + ' Teklif</span>' +
                    bestPriceSmall +
                    '</button>';
            }

            // B) Durum Rozeti (status-cell-wrap)
            var currentStatus = row.getAttribute('data-status');
            if (currentStatus !== 'Onaylandı' && currentStatus !== 'Kabul Edildi') {
                var statusWrap = row.querySelector('.status-cell-wrap');
                if (statusWrap) {
                    statusWrap.innerHTML = 
                        '<span class="status-dot-badge is-received" data-status-badge-id="' + reqId + '">' +
                        '  <span class="status-dot"></span>' +
                        '  Teklif Alındı' +
                        '</span>';
                }
                row.setAttribute('data-status', 'Teklif Alındı');
            }

            // C) Fiyat Kıyaslama Butonu (Alıcı / Talep Sahibi İçin)
            if (row.getAttribute('data-is-owner') === 'true') {
                var cluster = row.querySelector('.actions-cluster');
                if (cluster && !cluster.querySelector('[data-compare-btn-id]')) {
                    var cmpBtn = document.createElement('button');
                    cmpBtn.type = 'button';
                    cmpBtn.className = 'btn-action-view';
                    cmpBtn.setAttribute('data-compare-btn-id', reqId);
                    cmpBtn.style.cssText = 'background: #EFF6FF; color: #2563EB; border: 1px solid #BFDBFE; font-weight: 700; display: inline-flex; align-items: center; gap: 4px;';
                    cmpBtn.setAttribute('onclick', 'openCompareModal(' + reqId + ')');
                    cmpBtn.setAttribute('title', 'Tedarikçi Fiyatlarını Kıyasla');
                    cmpBtn.innerHTML = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="18" y1="20" x2="18" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line><line x1="12" y1="20" x2="12" y2="10"></line></svg> Fiyat Kıyasla';
                    cluster.insertBefore(cmpBtn, cluster.firstChild);
                }
            }

            // D) Tedarikçi Teklif Butonu (Teklif Ver -> Teklifi Düzenle)
            if (data.isMyBid) {
                var bidBtn = row.querySelector('[data-bid-btn-id="' + reqId + '"]') || row.querySelector('.btn-action-bid');
                if (bidBtn) {
                    bidBtn.textContent = 'Teklifi Düzenle';
                    bidBtn.style.background = '';
                    bidBtn.style.borderColor = '';
                    bidBtn.style.color = '';
                    var prodName = row.getAttribute('data-product-name') || data.productName || '';
                    var price = data.offeredPrice || '';
                    var days = data.deliveryDays || 7;
                    var notes = data.supplierNotes || '';
                    bidBtn.setAttribute('onclick', 'openOfferModal(' + reqId + ', "' + escapeHtml(prodName).replace(/"/g, '&quot;') + '", "' + price + '", "' + days + '", "' + escapeHtml(notes).replace(/"/g, '&quot;') + '")');
                    row.setAttribute('data-has-my-offer', 'true');
                }
            }
        });

        // 2. Dashboard Supplier RFQ Tablosu
        var rfqRows = document.querySelectorAll('#supplierRfqTable tr[data-request-id="' + reqId + '"], #supplierRfqTable tr[data-rfq-code="REQ-' + reqId + '"]');
        rfqRows.forEach(function (rfqRow) {
            rfqRow.classList.add('realtime-updated');

            var badge = rfqRow.querySelector('[data-rfq-status-badge="' + reqId + '"]') || rfqRow.querySelector('.status-badge');
            if (badge) {
                badge.className = 'status-badge badge-blue';
                badge.innerHTML = '<span class="status-dot"></span>Teklif Verildi';
            }

            if (data.isMyBid) {
                var actionCell = rfqRow.querySelector('[data-rfq-action-cell="' + reqId + '"]') || rfqRow.querySelector('td:last-child');
                if (actionCell) {
                    var prodName = data.productName || rfqRow.querySelector('.req-name')?.textContent || '';
                    actionCell.innerHTML = 
                        '<button type="button" class="btn-table-action is-secondary" onclick="openOfferModal(\'REQ-' + reqId + '\', \'' + escapeHtml(prodName).replace(/'/g, "\\'") + '\', \'' + data.offeredPrice + '\')">' +
                        'Düzenle' +
                        '</button>';
                }
            }
        });

        // 3. Sayaçları Güncelle
        if (data.isMyBid) {
            incrementCounter('myBidsTabCount');
        }
        incrementCounter('incomingOffersTabCount');
    }

    function updatePortalDomForOfferApproved(data) {
        if (!data || !data.requestId) return;
        var reqId = data.requestId;

        var rows = document.querySelectorAll('tr[data-request-id="' + reqId + '"]');
        rows.forEach(function (row) {
            row.classList.add('realtime-updated');
            row.setAttribute('data-status', 'Onaylandı');

            // Durum rozeti
            var statusWrap = row.querySelector('.status-cell-wrap');
            if (statusWrap) {
                statusWrap.innerHTML = 
                    '<span class="status-dot-badge is-approved" data-status-badge-id="' + reqId + '">' +
                    '  <span class="status-dot"></span>' +
                    '  Onaylandı' +
                    '</span>';
            }

            // Aksiyon butonları -> Teklif butonunu kaldır, Onaylandı etiketi koy
            var bidBtn = row.querySelector('[data-bid-btn-id="' + reqId + '"]') || row.querySelector('.btn-action-bid');
            if (bidBtn) {
                var approvedSpan = document.createElement('span');
                approvedSpan.style.cssText = 'color: #059669; font-weight: 700; font-size: 0.8rem; display: inline-block; padding: 4px 8px;';
                approvedSpan.textContent = '✓ Onaylandı';
                if (bidBtn.parentNode) {
                    bidBtn.parentNode.replaceChild(approvedSpan, bidBtn);
                }
            }
        });

        var rfqRows = document.querySelectorAll('#supplierRfqTable tr[data-request-id="' + reqId + '"], #supplierRfqTable tr[data-rfq-code="REQ-' + reqId + '"]');
        rfqRows.forEach(function (rfqRow) {
            var actionCell = rfqRow.querySelector('[data-rfq-action-cell="' + reqId + '"]') || rfqRow.querySelector('td:last-child');
            if (actionCell) {
                actionCell.innerHTML = '<span style="display: inline-block; padding: 4px 8px; font-size: 0.75rem; font-weight: 700; color: #059669; background: #ECFDF5; border-radius: 6px; border: 1px solid rgba(16, 185, 129, 0.2);">✓ Onaylandı</span>';
            }
            var badge = rfqRow.querySelector('[data-rfq-status-badge="' + reqId + '"]') || rfqRow.querySelector('.status-badge');
            if (badge) {
                badge.className = 'status-badge badge-emerald';
                badge.innerHTML = '<span class="status-dot"></span>Onaylandı';
            }
        });

        incrementCounter('kpiApprovedRequests');
    }

    function updatePortalDomForOfferRejected(data) {
        if (!data || !data.requestId) return;
        var reqId = data.requestId;

        var rows = document.querySelectorAll('tr[data-request-id="' + reqId + '"]');
        rows.forEach(function (row) {
            var bidBtn = row.querySelector('[data-bid-btn-id="' + reqId + '"]') || row.querySelector('.btn-action-bid');
            if (bidBtn) {
                bidBtn.className = 'btn-action-bid';
                bidBtn.style.cssText = 'background: #EFF6FF; color: #2563EB; border: 1px solid #93C5FD; display: inline-flex; align-items: center; gap: 4px;';
                bidBtn.title = 'Teklifiniz reddedildi. Talep devam ettiği sürece revize teklif gönderebilirsiniz.';
                bidBtn.innerHTML = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M12 20h9"></path><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path></svg> Revize Et';
            }
        });

        var rfqRows = document.querySelectorAll('#supplierRfqTable tr[data-request-id="' + reqId + '"]');
        rfqRows.forEach(function (rfqRow) {
            var actionCell = rfqRow.querySelector('[data-rfq-action-cell="' + reqId + '"]') || rfqRow.querySelector('td:last-child');
            if (actionCell) {
                actionCell.innerHTML = '<span style="display: inline-block; padding: 4px 8px; font-size: 0.75rem; font-weight: 700; color: #DC2626; background: #FEF2F2; border-radius: 6px; border: 1px solid rgba(239, 68, 68, 0.2);">✕ Reddedildi</span>';
            }
        });
    }

    window.updatePortalDomForOfferSubmitted = updatePortalDomForOfferSubmitted;
    window.updatePortalDomForOfferApproved = updatePortalDomForOfferApproved;
    window.updatePortalDomForOfferRejected = updatePortalDomForOfferRejected;

    window.showRealtimeToast = showRealtimeToast;
    window.showAppToast = function(message, isSuccess) {
        showRealtimeToast(message, isSuccess === false ? 'error' : 'success');
    };

    function incrementCounter(elementId) {
        var el = document.getElementById(elementId);
        if (el) {
            var current = parseInt(el.textContent.replace(/[^\d]/g, ''), 10) || 0;
            el.textContent = (current + 1).toLocaleString('tr-TR');
            el.classList.add('realtime-badge-pulse');
            setTimeout(function () { el.classList.remove('realtime-badge-pulse'); }, 1800);
        }
    }

    function escapeHtml(text) {
        if (!text) return '';
        var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(text).replace(/[&<>"']/g, function (m) { return map[m]; });
    }

})();
