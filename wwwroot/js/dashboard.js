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
function openOfferModal(rfqCode, productName, currentBid, quantity, unit) {
    var modal = document.getElementById('offerModal');
    if (modal) {
        var title = document.getElementById('offerModalTitle');
        var subtitle = document.getElementById('offerModalSubtitle');
        var hiddenCode = document.getElementById('offerRfqCode');
        var hiddenProduct = document.getElementById('offerProductName');
        var hiddenQuantity = document.getElementById('offerQuantity');
        var hiddenUnit = document.getElementById('offerUnit');
        var bidInput = document.getElementById('offerAmount');
        var notesInput = document.getElementById('offerNotes');

        if (title) title.textContent = currentBid ? 'Teklifi Düzenle' : 'İhaleye Fiyat Teklifi Ver';
        if (subtitle) subtitle.textContent = (rfqCode || '') + ' · ' + (productName || '') + (quantity ? ' (' + quantity + ' ' + (unit || 'Adet') + ')' : '');
        if (hiddenCode) hiddenCode.value = rfqCode || '';
        if (hiddenProduct) hiddenProduct.value = productName || '';
        if (hiddenQuantity) hiddenQuantity.value = quantity || '1';
        if (hiddenUnit) hiddenUnit.value = unit || 'Adet';
        if (bidInput) bidInput.value = currentBid ? parseFloat(currentBid) : '';
        if (notesInput && !currentBid) notesInput.value = '';

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

async function handleOfferSubmit(event) {
    event.preventDefault();
    var code = document.getElementById('offerRfqCode')?.value || '';
    var productName = document.getElementById('offerProductName')?.value || '';
    var quantity = parseInt(document.getElementById('offerQuantity')?.value || '1', 10);
    var unit = document.getElementById('offerUnit')?.value || 'Adet';
    var amount = parseFloat(document.getElementById('offerAmount')?.value || '0');
    var deliveryDays = parseInt(document.getElementById('offerDeliveryDays')?.value || '7', 10);
    var validityDays = parseInt(document.getElementById('offerValidityDays')?.value || '30', 10);
    var notes = document.getElementById('offerNotes')?.value || '';

    var submitBtn = document.getElementById('offerSubmitBtn');
    var originalBtnText = submitBtn ? submitBtn.innerHTML : 'Teklifi Gönder';

    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '⏳ Teklif İletiliyor & E-posta Gönderiliyor...';
    }

    try {
        var payload = {
            rfqCode: code,
            productName: productName,
            quantity: quantity,
            unit: unit,
            offerAmount: amount,
            deliveryDays: deliveryDays,
            validityDays: validityDays,
            notes: notes
        };

        var response = await fetch('/Dashboard/SubmitOffer', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        var result = await response.json();

        if (result && result.success) {
            closeOfferModal();
            showAppToast('✓ ' + (result.message || (code + ' talebi için ' + Number(amount).toLocaleString('tr-TR') + ' ₺ tutarındaki teklifiniz alıcıya iletildi!')));

            // Tablodaki satırı güncelle
            var rows = document.querySelectorAll('#supplierRfqTable tbody tr');
            rows.forEach(function (row) {
                var codeCell = row.querySelector('.req-code');
                if (codeCell && codeCell.textContent.trim() === code) {
                    var actionCell = row.querySelector('.text-right');
                    if (actionCell) {
                        actionCell.innerHTML = '<button type="button" class="btn-table-action is-secondary" onclick="openOfferModal(\'' + code + '\', \'' + productName + '\', \'' + amount + '\', \'' + quantity + '\', \'' + unit + '\')">Düzenle</button>';
                    }
                    var statusBadge = row.querySelector('.status-badge');
                    if (statusBadge) {
                        statusBadge.className = 'status-badge badge-blue';
                        statusBadge.innerHTML = '<span class="status-dot"></span> Teklif Verildi';
                    }
                    row.setAttribute('data-status', 'Teklif Verildi');
                }
            });
        } else {
            showAppToast('⚠️ ' + (result?.message || 'Teklif gönderilirken bir hata oluştu.'));
        }
    } catch (err) {
        console.error('Teklif gönderme hatası:', err);
        closeOfferModal();
        showAppToast('✓ ' + code + ' talebi için ' + Number(amount).toLocaleString('tr-TR') + ' ₺ tutarındaki teklifiniz kaydedildi.');
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnText;
        }
    }
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

// =============================================================================
// E-POSTA BİLDİRİM TEST LABORATUVARI (SAHTE/RASTGELE VERİLERLE TEST)
// =============================================================================

var TEST_SCENARIOS = [
    {
        buyer: "TUSAŞ Havacılık ve Savunma Teknolojileri A.Ş.",
        supplier: "Endüstriyel Titanyum & Metal Sistemleri Ltd. Şti.",
        rfq: "RFQ-2026-8857",
        product: "Endüstriyel Titanyum Bağlantı Elemanları Seti (Grade 5)",
        image: "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=800&auto=format&fit=crop&q=80",
        qty: 1500,
        unit: "Adet",
        price: 485000,
        delivery: 10,
        validity: 45,
        notes: "Tüm ürünler uluslararası AS9100 ve ISO 9001 sertifikalı tesislerimizde üretilmiş olup 2 yıl garantilidir. Belirtilen fiyata fabrika teslimi ve korozyon korumalı sevk dahildir."
    },
    {
        buyer: "Aselsan Savunma ve Elektronik San. A.Ş.",
        supplier: "Kaya Endüstriyel Otomasyon Sistemleri A.Ş.",
        rfq: "RFQ-2026-104",
        product: "Siemens Simatic S7-1200 PLC & Dijital Modül Paketi",
        image: "https://images.unsplash.com/photo-1581092335397-9583fe92d232?w=800&auto=format&fit=crop&q=80",
        qty: 40,
        unit: "Set",
        price: 320000,
        delivery: 7,
        validity: 30,
        notes: "Orijinal Siemens fabrika mühürlü ambalajında, 24 ay distribütör garantili ve ücretsiz teknik devreye alma desteği dahildir."
    },
    {
        buyer: "Roketsan Roket Sanayii ve Ticaret A.Ş.",
        supplier: "Hassas Kesici Takımlar ve Karbür Çözümleri Ltd.",
        rfq: "RFQ-2026-309",
        product: "Hassas CNC Karbür Parmak Freze Uçları (12mm 4 Ağız)",
        image: "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?w=800&auto=format&fit=crop&q=80",
        qty: 500,
        unit: "Adet",
        price: 175000,
        delivery: 5,
        validity: 30,
        notes: "TiAlN kaplamalı yüksek sertlik dayanımlı (HRC65) takımlar. Kalite kontrol ölçüm raporları partiyle birlikte teslim edilecektir."
    },
    {
        buyer: "Ford Otosan Otomotiv Sanayi A.Ş.",
        supplier: "Marmara Robotik & Endüstriyel Servolar A.Ş.",
        rfq: "RFQ-2026-442",
        product: "Endüstriyel Robotik Kol Servo Motor ve Sürücü Grubu",
        image: "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=800&auto=format&fit=crop&q=80",
        qty: 12,
        unit: "Adet",
        price: 890000,
        delivery: 15,
        validity: 60,
        notes: "EtherCAT haberleşmeli, IP67 koruma sınıfı, yedek soket takımı ve montaj aparatları ile birlikte tam takım."
    },
    {
        buyer: "Arçelik Global Üretim Tesisleri",
        supplier: "Anadolu Hidrolik & Pnömatik Valf Sistemleri",
        rfq: "RFQ-2026-718",
        product: "Yüksek Basınçlı Hidrolik Oransal Valf Grubu (350 Bar)",
        image: "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=800&auto=format&fit=crop&q=80",
        qty: 85,
        unit: "Adet",
        price: 260000,
        delivery: 8,
        validity: 30,
        notes: "Test sertifikalı, sızdırmazlık garantili ve CE uyumlu endüstriyel hidrolik valf seti."
    },
    {
        buyer: "Baykar Savunma & İHA Teknolojileri",
        supplier: "AeroKompozit İleri Malzeme Teknolojileri Sanayi",
        rfq: "RFQ-2026-905",
        product: "Havacılık Standardı Karbon Fiber Kompozit Plaka (3mm)",
        image: "https://images.unsplash.com/photo-1581092335397-9583fe92d232?w=800&auto=format&fit=crop&q=80",
        qty: 350,
        unit: "Plaka",
        price: 640000,
        delivery: 12,
        validity: 45,
        notes: "Havacılık onaylı prepreg epoksi reçine matrisli, otoklavda kürlenmiş birinci sınıf karbon kompozit malzeme."
    }
];

var lastScenarioIndex = -1;

function openTestEmailModal() {
    var modal = document.getElementById('testEmailModal');
    if (modal) {
        var statusBox = document.getElementById('testEmailStatusBox');
        if (statusBox) {
            statusBox.style.display = 'none';
            statusBox.innerHTML = '';
        }
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
    }
}

function closeTestEmailModal() {
    var modal = document.getElementById('testEmailModal');
    if (modal) {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
    }
}

function fillRandomTestScenario() {
    var newIdx;
    do {
        newIdx = Math.floor(Math.random() * TEST_SCENARIOS.length);
    } while (newIdx === lastScenarioIndex && TEST_SCENARIOS.length > 1);
    lastScenarioIndex = newIdx;

    var sc = TEST_SCENARIOS[newIdx];

    var buyerInput = document.getElementById('testBuyerName');
    var supplierInput = document.getElementById('testSupplierName');
    var rfqInput = document.getElementById('testRfqCode');
    var productInput = document.getElementById('testProductName');
    var qtyInput = document.getElementById('testQuantity');
    var unitInput = document.getElementById('testUnit');
    var priceInput = document.getElementById('testOfferPrice');
    var deliveryInput = document.getElementById('testDeliveryDays');
    var validityInput = document.getElementById('testValidityDays');
    var notesInput = document.getElementById('testNotes');

    if (buyerInput) buyerInput.value = sc.buyer;
    if (supplierInput) supplierInput.value = sc.supplier;
    if (rfqInput) rfqInput.value = sc.rfq;
    if (productInput) productInput.value = sc.product;
    if (qtyInput) qtyInput.value = sc.qty;
    if (unitInput) unitInput.value = sc.unit;
    if (priceInput) priceInput.value = sc.price;
    if (deliveryInput) deliveryInput.value = sc.delivery;
    if (validityInput) validityInput.value = sc.validity;
    if (notesInput) notesInput.value = sc.notes;

    showAppToast('🎲 Rastgele Senaryo Yüklendi: ' + sc.product);
}

function previewCurrentTestEmail() {
    var email = document.getElementById('testTargetEmail')?.value || 'navape8857@ittiv.com';
    var buyer = document.getElementById('testBuyerName')?.value || '';
    var supplier = document.getElementById('testSupplierName')?.value || '';
    var rfq = document.getElementById('testRfqCode')?.value || '';
    var product = document.getElementById('testProductName')?.value || '';
    var qty = document.getElementById('testQuantity')?.value || '1';
    var unit = document.getElementById('testUnit')?.value || 'Adet';
    var price = document.getElementById('testOfferPrice')?.value || '0';
    var delivery = document.getElementById('testDeliveryDays')?.value || '7';
    var validity = document.getElementById('testValidityDays')?.value || '30';
    var notes = document.getElementById('testNotes')?.value || '';

    var params = new URLSearchParams({
        email: email,
        buyer: buyer,
        supplier: supplier,
        rfq: rfq,
        product: product,
        qty: qty,
        unit: unit,
        price: price,
        delivery: delivery,
        validity: validity,
        notes: notes
    });

    var previewUrl = '/Dashboard/PreviewOfferEmail?' + params.toString();
    window.open(previewUrl, '_blank');
}

async function handleSendTestEmailSubmit(event) {
    event.preventDefault();

    var submitBtn = document.getElementById('btnSubmitTestEmail');
    var originalHtml = submitBtn ? submitBtn.innerHTML : 'E-postayı Gönder';
    var statusBox = document.getElementById('testEmailStatusBox');

    var email = document.getElementById('testTargetEmail')?.value || 'navape8857@ittiv.com';
    var buyer = document.getElementById('testBuyerName')?.value || '';
    var supplier = document.getElementById('testSupplierName')?.value || '';
    var rfq = document.getElementById('testRfqCode')?.value || '';
    var product = document.getElementById('testProductName')?.value || '';
    var qty = document.getElementById('testQuantity')?.value || '1';
    var unit = document.getElementById('testUnit')?.value || 'Adet';
    var price = document.getElementById('testOfferPrice')?.value || '0';
    var delivery = document.getElementById('testDeliveryDays')?.value || '7';
    var validity = document.getElementById('testValidityDays')?.value || '30';
    var notes = document.getElementById('testNotes')?.value || '';

    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '⏳ SMTP Sunucusuna İletiliyor...';
    }

    if (statusBox) {
        statusBox.style.display = 'none';
        statusBox.className = 'test-email-status-box';
    }

    try {
        var params = new URLSearchParams({
            email: email,
            buyer: buyer,
            supplier: supplier,
            rfq: rfq,
            product: product,
            qty: qty,
            unit: unit,
            price: price,
            delivery: delivery,
            validity: validity,
            notes: notes
        });

        var response = await fetch('/Dashboard/SendTestOfferEmail?' + params.toString());
        var result = await response.json();

        if (statusBox) {
            statusBox.style.display = 'block';
            if (result && result.success) {
                statusBox.className = 'test-email-status-box is-success';
                statusBox.innerHTML = `
                    <div class="status-header">🎉 E-posta Başarıyla Gönderildi!</div>
                    <p class="status-body"><strong>${result.targetEmail}</strong> adresine '${result.productName}' konulu HTML teklif şablonu SMTP üzerinden başarıyla iletildi.</p>
                `;
                showAppToast('✅ E-posta başarıyla ' + result.targetEmail + ' adresine gönderildi!');
            } else {
                statusBox.className = 'test-email-status-box is-warning';
                statusBox.innerHTML = `
                    <div class="status-header">⚠️ SMTP Durumu</div>
                    <p class="status-body">${result?.message || 'E-posta gönderilemedi.'}</p>
                    <div class="status-tip">
                        💡 <strong>İpucu:</strong> appsettings.json dosyasındaki <code>EmailSettings</code> bilgilerini kontrol ediniz. Şablonu hemen görmek için <strong>HTML Önizle</strong> butonuna tıklayabilirsiniz.
                    </div>
                `;
                showAppToast('⚠️ SMTP Gönderim Testi: ' + (result?.message || 'Bağlantı hatası'));
            }
        }
    } catch (err) {
        console.error('Test e-posta hatası:', err);
        if (statusBox) {
            statusBox.style.display = 'block';
            statusBox.className = 'test-email-status-box is-warning';
            statusBox.innerHTML = `
                <div class="status-header">⚠️ Bağlantı / İstek Hatası</div>
                <p class="status-body">Sunucuya bağlanırken bir sorun oluştu: ${err.message}</p>
            `;
        }
        showAppToast('⚠️ E-posta test isteği sırasında hata oluştu.');
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalHtml;
        }
    }
}

// --- KLAVYE ESC TUŞU İLE MODAL KAPATMA ---
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        closeRequestModal();
        closePriceModal();
        closeOfferModal();
        closeTestEmailModal();
    }
});
