/**
 * B2B Kurumsal Tedarik Portalı - 'İhtiyacınız Olabilecek Ürünler' Vitrini
 * 6 Sütun x 8 Satır (48 Ürün) Profesyonel Kurumsal Ürün & Tedarikçi Motoru
 */

// Tedarikçi Firma İsimleri ve Veritabanı ID Eşleme Haritası
const B2B_SELLER_ID_MAP = {
    "atlas metal": 1,
    "sac sanayi": 1,
    "lazer kesim": 1,
    "ege plastik": 2,
    "ambalaj": 2,
    "nova elektronik": 3,
    "otomasyon": 3,
    "sunucu": 3,
    "ağ & altyapı": 3,
    "barkod": 3,
    "karadeniz kereste": 4,
    "ahşap": 4,
    "palet": 4,
    "zirve kimya": 5,
    "kuzey kimya": 5,
    "polimer": 5,
    "yapıştırıcı": 5,
    "birlik rulman": 6,
    "toros": 6,
    "hırdavat": 6,
    "güvenlik": 6,
    "yıldız lojistik": 7,
    "tekno enerji": 8,
    "kablo": 8,
    "şalt": 8,
    "pano": 8,
    "başak cıvata": 9,
    "bağlantı elemanları": 9,
    "vizyon endüstriyel": 10,
    "vasıflı çelik": 10,
    "freze": 10
};

function getSellerCompanyId(item) {
    if (!item) return 1;
    if (typeof item.sellerCompanyId === 'number' && item.sellerCompanyId > 0) {
        return item.sellerCompanyId;
    }
    const raw = (item.seller || item.brand || '').toLowerCase();
    for (const [key, id] of Object.entries(B2B_SELLER_ID_MAP)) {
        if (raw.includes(key)) return id;
    }
    return (item.id ? ((Math.abs(item.id) % 10) + 1) : 1);
}

// Zengin B2B Endüstriyel Ürün Kataloğu (Kurumsal Şirket Odaklı)
const B2B_PRODUCT_CATALOG = [
    // Bilişim & IT Altyapı
    {
        id: 101,
        sku: "SRV-DELL-R750",
        title: "Dell PowerEdge R750xs Rack Sunucu (2x Xeon Silver 4314, 128GB ECC RAM, 2x 960GB SAS SSD)",
        category: "it",
        categoryName: "Bilişim & IT",
        brand: "Dell Technologies",
        seller: "Nova Elektronik & Sunucu Sistemleri A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Yetkili Distribütör",
        sellerLocation: "İstanbul (Ümraniye)",
        moq: "1 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 198000,
        price: 168500,
        discount: 15,
        rating: 4.9,
        reviewsCount: 148,
        badge: "ISO 9001 Belgeli",
        badgeType: "corporate",
        stockStatus: "8 Adet (Merkez Depo)",
        leadTime: "1-2 İş Günü",
        tierPricing: "1-2 Adet: 168.500 ₺ | 3+ Adet: 159.000 ₺",
        image: "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 102,
        sku: "NET-CS-9200L-48P",
        title: "Cisco Catalyst 9200L 48-Port PoE+ Gigabit Yönetilebilir Layer-3 Ağ Anahtarı",
        category: "it",
        categoryName: "Bilişim & IT",
        brand: "Cisco Enterprise",
        seller: "Ağ & Altyapı Çözümleri San. Tic. A.Ş.",
        sellerScore: 9.8,
        sellerBadge: "Platin Çözüm Ortağı",
        sellerLocation: "Ankara (ODTÜ Teknokent)",
        moq: "1 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 94000,
        price: 79900,
        discount: 15,
        rating: 4.9,
        reviewsCount: 92,
        badge: "Kurumsal Faturalı",
        badgeType: "certified",
        stockStatus: "18 Adet Hazır Stok",
        leadTime: "Aynı Gün Sevk",
        tierPricing: "1-4 Adet: 79.900 ₺ | 5+ Adet: 74.500 ₺",
        image: "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 103,
        sku: "UPS-APC-5000RT",
        title: "Schneider APC Smart-UPS RT 5000VA 230V Kesintisiz Güç Kaynağı (Online Çift Çevrim)",
        category: "it",
        categoryName: "Bilişim & IT",
        brand: "APC by Schneider",
        seller: "Enerji & Güç Donanımları A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Yetkili Servis & Satış",
        sellerLocation: "Kocaeli (Gebze OSB)",
        moq: "1 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 118000,
        price: 99500,
        discount: 16,
        rating: 4.9,
        reviewsCount: 54,
        badge: "TSE & CE Belgeli",
        badgeType: "corporate",
        stockStatus: "12 Adet Stokta",
        leadTime: "24 Saat İçinde Sevk",
        tierPricing: "1-2 Adet: 99.500 ₺ | 3+ Adet: 92.000 ₺",
        image: "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 104,
        sku: "PRN-ZBR-ZT411",
        title: "Zebra ZT411 203 DPI Endüstriyel Barkod & Termal Transfer Etiket Yazıcı",
        category: "it",
        categoryName: "Bilişim & IT",
        brand: "Zebra Technologies",
        seller: "Barkod & Otomasyon Sistemleri Ltd.",
        sellerScore: 9.8,
        sellerBadge: "Onaylı B2B Tedarikçi",
        sellerLocation: "İzmir (Bornova)",
        moq: "1 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 49500,
        price: 42800,
        discount: 14,
        rating: 4.8,
        reviewsCount: 110,
        badge: "Sanayi Tipi",
        badgeType: "industrial",
        stockStatus: "24 Adet Stok",
        leadTime: "Hızlı Gönderi",
        tierPricing: "1-3 Adet: 42.800 ₺ | 4+ Adet: 39.500 ₺",
        image: "https://images.unsplash.com/photo-1612815154858-60aa4c59eaa6?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 105,
        sku: "WIFI-UB-U6PRO",
        title: "Ubiquiti UniFi 6 Pro WiFi-6 AX5400 Kurumsal Tavan Tipi Access Point (10'lu Pro Paket)",
        category: "it",
        categoryName: "Bilişim & IT",
        brand: "Ubiquiti Networks",
        seller: "Ağ & Altyapı Çözümleri San. Tic. A.Ş.",
        sellerScore: 9.8,
        sellerBadge: "Platin Dağıtıcı",
        sellerLocation: "İstanbul (Kartal)",
        moq: "1 Paket",
        unit: "Paket (10 Adet)",
        taxText: "+ %20 KDV",
        oldPrice: 78000,
        price: 66500,
        discount: 15,
        rating: 4.9,
        reviewsCount: 76,
        badge: "Toplu Paket Avantajı",
        badgeType: "wholesale",
        stockStatus: "15 Paket Stokta",
        leadTime: "Aynı Gün Kargo",
        tierPricing: "1-2 Pk: 66.500 ₺ | 3+ Pk: 62.000 ₺",
        image: "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?w=500&auto=format&fit=crop&q=80"
    },

    // Makine, Sac & Metal Sanayi
    {
        id: 201,
        sku: "MTL-DKP-ST37-2MM",
        title: "Erdemir Kalite ST37 DKP Sac Plaka 2.00mm x 1250mm x 2500mm (Lazer Kesim Uyumlu)",
        category: "metal",
        categoryName: "Makine & Metal",
        brand: "Erdemir / Atlas Çelik",
        seller: "Atlas Metal & Sac Sanayi A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Üretici & Fabrika Satış",
        sellerLocation: "Kocaeli (Dilovası İMES OSB)",
        moq: "10 Plaka",
        unit: "Plaka (~49 Kg)",
        taxText: "+ %20 KDV",
        oldPrice: 2850,
        price: 2390,
        discount: 16,
        rating: 4.9,
        reviewsCount: 340,
        badge: "Doğrudan Üreticiden",
        badgeType: "manufacturer",
        stockStatus: "1.400 Plaka Stok",
        leadTime: "Fabrikadan Sevkiyat",
        tierPricing: "10-49 Plaka: 2.390 ₺ | 50+ Plaka: 2.180 ₺",
        image: "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 202,
        sku: "BORU-INOX-304-42",
        title: "Paslanmaz Krom 304 Kalite Dikişsiz Sanayi Borusu Ø42.4 x 2.0mm (6 Metre Boy)",
        category: "metal",
        categoryName: "Makine & Metal",
        brand: "Vizyon Paslanmaz",
        seller: "Vizyon Endüstriyel Çelik Ltd. Şti.",
        sellerScore: 9.8,
        sellerBadge: "ISO 9001 Onaylı",
        sellerLocation: "Bursa (NOSAB)",
        moq: "5 Boy",
        unit: "Boy (6 Metre)",
        taxText: "+ %20 KDV",
        oldPrice: 2400,
        price: 1980,
        discount: 17,
        rating: 4.9,
        reviewsCount: 185,
        badge: "Test Sertifikalı (EN 10204)",
        badgeType: "certified",
        stockStatus: "650 Boy Stokta",
        leadTime: "1-2 İş Günü",
        tierPricing: "5-19 Boy: 1.980 ₺ | 20+ Boy: 1.820 ₺",
        image: "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 203,
        sku: "PRF-ALU-4040-K8",
        title: "Eloksallı Alüminyum Sigma Profil 40x40mm Kanal 8 Ağır Tip (6063 T6 / 6 Metre Boy)",
        category: "metal",
        categoryName: "Makine & Metal",
        brand: "SigmaTech Makine",
        seller: "Atlas Metal & Sac Sanayi A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Entegre İmalatçı",
        sellerLocation: "İstanbul (Dudullu OSB)",
        moq: "10 Boy",
        unit: "Boy (6 Metre)",
        taxText: "+ %20 KDV",
        oldPrice: 1450,
        price: 1180,
        discount: 19,
        rating: 5.0,
        reviewsCount: 420,
        badge: "Hassas Ekstrüzyon",
        badgeType: "corporate",
        stockStatus: "850 Boy Hazır",
        leadTime: "24 Saatte Kargo / Ambar",
        tierPricing: "10-49 Boy: 1.180 ₺ | 50+ Boy: 1.050 ₺",
        image: "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 204,
        sku: "MIL-TRN-1040-30",
        title: "Ç1040 Transmisyon Mili Taşlanmış h9 Tolerans Ø30mm x 3000mm (İmalat Çeliği)",
        category: "metal",
        categoryName: "Makine & Metal",
        brand: "Vizyon Çelik",
        seller: "Vizyon Endüstriyel Çelik Ltd. Şti.",
        sellerScore: 9.8,
        sellerBadge: "Toptan Dağıtım",
        sellerLocation: "Kocaeli (Gebze)",
        moq: "5 Adet",
        unit: "Adet (3 Metre)",
        taxText: "+ %20 KDV",
        oldPrice: 1750,
        price: 1420,
        discount: 19,
        rating: 4.8,
        reviewsCount: 130,
        badge: "h9 Hassas Taşlama",
        badgeType: "industrial",
        stockStatus: "400 Adet Stok",
        leadTime: "Aynı Gün Sevk",
        tierPricing: "5-19 Adet: 1.420 ₺ | 20+ Adet: 1.290 ₺",
        image: "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?w=500&auto=format&fit=crop&q=80"
    },

    // Endüstriyel Donanım & Hırdavat & Bağlantı Elemanları
    {
        id: 301,
        sku: "RLM-SKF-6205-2RSH",
        title: "SKF 6205-2RSH Derin Oluklu Bilyalı Rulman Çift Kauçuk Kapaklı (Koli İçi 50 Adet)",
        category: "hardware",
        categoryName: "Endüstriyel & Hırdavat",
        brand: "SKF Bearings",
        seller: "Birlik Rulman & Endüstriyel Tedarik Ltd.",
        sellerScore: 9.9,
        sellerBadge: "Yetkili Ana Distribütör",
        sellerLocation: "İstanbul (Karaköy Perpa)",
        moq: "1 Koli",
        unit: "Koli (50 Adet)",
        taxText: "+ %20 KDV",
        oldPrice: 8500,
        price: 6850,
        discount: 19,
        rating: 5.0,
        reviewsCount: 510,
        badge: "%100 Orijinal SKF",
        badgeType: "certified",
        stockStatus: "120 Koli Stokta",
        leadTime: "Hızlı Sevkiyat",
        tierPricing: "1-4 Koli: 6.850 ₺ | 5+ Koli: 6.200 ₺",
        image: "https://images.unsplash.com/photo-1537462715879-360eeb61a0ad?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 302,
        sku: "CVT-DIN933-A2-M8",
        title: "DIN 933 Paslanmaz Çelik A2-70 İnox Tam Diş Altıköşe Başlı Cıvata M8x30 (2.500 Adet Koli)",
        category: "hardware",
        categoryName: "Endüstriyel & Hırdavat",
        brand: "Başak Bağlantı",
        seller: "Başak Cıvata & Bağlantı Elemanları A.Ş.",
        sellerScore: 9.8,
        sellerBadge: "Üretici Tedarikçi",
        sellerLocation: "Konya (Büsan Sanayi)",
        moq: "1 Koli",
        unit: "Koli (2.500 Adet)",
        taxText: "+ %20 KDV",
        oldPrice: 3800,
        price: 2950,
        discount: 22,
        rating: 4.9,
        reviewsCount: 380,
        badge: "TSE & DIN Normu",
        badgeType: "manufacturer",
        stockStatus: "350 Koli Stok",
        leadTime: "Aynı Gün Kargoda",
        tierPricing: "1-4 Koli: 2.950 ₺ | 5+ Koli: 2.650 ₺",
        image: "https://images.unsplash.com/photo-1586864387967-d02ef85d93e8?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 303,
        sku: "ELK-MGW-ESR13-325",
        title: "Magmaweld ESR 13 Rutil Kaynak Elektrodu 3.25x350mm (Koli İçi 3 Paket / 15 Kg)",
        category: "hardware",
        categoryName: "Endüstriyel & Hırdavat",
        brand: "Magmaweld",
        seller: "Atlas Metal & Sac Sanayi A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Ana Bayi",
        sellerLocation: "Manisa (MOSB)",
        moq: "2 Koli",
        unit: "Koli (15 Kg)",
        taxText: "+ %20 KDV",
        oldPrice: 2600,
        price: 2150,
        discount: 17,
        rating: 4.9,
        reviewsCount: 720,
        badge: "TSE / Lloyd Onaylı",
        badgeType: "certified",
        stockStatus: "500 Koli Stokta",
        leadTime: "Hemen Sevk",
        tierPricing: "2-9 Koli: 2.150 ₺ | 10+ Koli: 1.940 ₺",
        image: "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 304,
        sku: "TAKIM-IZL-110P",
        title: "İzeltaş 110 Parça Endüstriyel Dolu Takım Çantası & Lokma Kombine Anahtar Seti",
        category: "hardware",
        categoryName: "Endüstriyel & Hırdavat",
        brand: "İzeltaş",
        seller: "Sanayi Takımları ve Donanım A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Yetkili Endüstriyel Bayi",
        sellerLocation: "İzmir (Işıkkent)",
        moq: "1 Set",
        unit: "Set",
        taxText: "+ %20 KDV",
        oldPrice: 15800,
        price: 12900,
        discount: 18,
        rating: 5.0,
        reviewsCount: 640,
        badge: "Ömür Boyu Garanti",
        badgeType: "corporate",
        stockStatus: "45 Set Stokta",
        leadTime: "24 Saatte Teslimat",
        tierPricing: "1-4 Set: 12.900 ₺ | 5+ Set: 11.800 ₺",
        image: "https://images.unsplash.com/photo-1581244277943-fe4a9c777189?w=500&auto=format&fit=crop&q=80"
    },

    // Elektrik, Enerji & Otomasyon
    {
        id: 401,
        sku: "KBL-OZN-NYY-4X6",
        title: "Öznur NYY 0.6/1kV 4x6mm² Ağır Sanayi Yeraltı Enerji Kablosu (500 Metre Makara)",
        category: "energy",
        categoryName: "Elektrik & Enerji",
        brand: "Öznur Kablo",
        seller: "Tekno Enerji & Kablo Sanayi A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Fabrika Distribütörü",
        sellerLocation: "Gaziantep (4. OSB)",
        moq: "1 Makara",
        unit: "Makara (500m)",
        taxText: "+ %20 KDV",
        oldPrice: 48500,
        price: 39800,
        discount: 18,
        rating: 4.9,
        reviewsCount: 290,
        badge: "TSE / CE / VDE Belgeli",
        badgeType: "certified",
        stockStatus: "85 Makara Stok",
        leadTime: "Özel Sevk & Ambar",
        tierPricing: "1-2 Makara: 39.800 ₺ | 3+ Makara: 36.500 ₺",
        image: "https://images.unsplash.com/photo-1558346490-a72e53ae2d4f?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 402,
        sku: "OTM-SMS-3RT2026",
        title: "Siemens SIRIUS 3RT2026-1BB40 24V DC 25A 11kW 3-Kutuplu Güç Kontaktörü",
        category: "energy",
        categoryName: "Elektrik & Enerji",
        brand: "Siemens Automation",
        seller: "Nova Elektronik & Otomasyon A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Siemens Çözüm Ortağı",
        sellerLocation: "İstanbul (Kadıköy)",
        moq: "5 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 2800,
        price: 2280,
        discount: 19,
        rating: 5.0,
        reviewsCount: 310,
        badge: "Alman Endüstri Standartı",
        badgeType: "industrial",
        stockStatus: "220 Adet Stok",
        leadTime: "Aynı Gün Kargo",
        tierPricing: "5-19 Adet: 2.280 ₺ | 20+ Adet: 2.050 ₺",
        image: "https://images.unsplash.com/photo-1558346490-a72e53ae2d4f?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 403,
        sku: "AYD-PHL-HB-150W",
        title: "Philips CoreLine HighBay 150W 20.000 Lümen IP65 Sanayi Fabrika Tavan LED Armatür",
        category: "energy",
        categoryName: "Elektrik & Enerji",
        brand: "Philips Lighting",
        seller: "Aydınlatma & Enerji Sistemleri A.Ş.",
        sellerScore: 9.8,
        sellerBadge: "Kurumsal Proje Bayi",
        sellerLocation: "Ankara (İvedik OSB)",
        moq: "4 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 4200,
        price: 3450,
        discount: 18,
        rating: 4.9,
        reviewsCount: 220,
        badge: "5 Yıl Proje Garantisi",
        badgeType: "corporate",
        stockStatus: "350 Adet Stokta",
        leadTime: "Hızlı Gönderi",
        tierPricing: "4-19 Adet: 3.450 ₺ | 20+ Adet: 3.100 ₺",
        image: "https://images.unsplash.com/photo-1558346490-a72e53ae2d4f?w=500&auto=format&fit=crop&q=80"
    },

    // Ambalaj, Palet & Endüstriyel Depolama
    {
        id: 501,
        sku: "AMB-STR-50CM-17M",
        title: "Endüstriyel Otomatik / Manuel Palet Streç Film 50cm 17 Mikron 2.3 Kg (Koli İçi 6 Rulo)",
        category: "packaging",
        categoryName: "Ambalaj & Lojistik",
        brand: "Ege Pack Sanayi",
        seller: "Ege Plastik & Ambalaj Ltd. Şti.",
        sellerScore: 9.9,
        sellerBadge: "Üretici Fabrika",
        sellerLocation: "İzmir (Kemalpaşa OSB)",
        moq: "10 Koli",
        unit: "Koli (6 Rulo)",
        taxText: "+ %20 KDV",
        oldPrice: 1350,
        price: 1040,
        discount: 23,
        rating: 4.9,
        reviewsCount: 1100,
        badge: "Yüksek Esneklik (%300)",
        badgeType: "manufacturer",
        stockStatus: "2.500 Koli Stok",
        leadTime: "Tır / Kamyon / Ambar Sevk",
        tierPricing: "10-49 Koli: 1.040 ₺ | 50+ Koli: 920 ₺",
        image: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 502,
        sku: "PLT-EPAL-EURO-80120",
        title: "EPAL Euro Ahşap Palet 80x120cm ISPM 15 Isıl İşlemli (İhracat ve Ağır Yük Uyumlu)",
        category: "packaging",
        categoryName: "Ambalaj & Lojistik",
        brand: "Karadeniz Palet",
        seller: "Karadeniz Kereste & Ahşap San. A.Ş.",
        sellerScore: 9.8,
        sellerBadge: "EPAL Lisanslı İmalatçı",
        sellerLocation: "Trabzon (Arsin OSB)",
        moq: "20 Adet",
        unit: "Adet (Palet)",
        taxText: "+ %20 KDV",
        oldPrice: 480,
        price: 390,
        discount: 19,
        rating: 4.8,
        reviewsCount: 430,
        badge: "ISPM-15 Lisanslı",
        badgeType: "certified",
        stockStatus: "4.000 Adet Stok",
        leadTime: "Paletli Kamyon Sevkiyat",
        tierPricing: "20-99 Adet: 390 ₺ | 100+ Adet: 345 ₺",
        image: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 503,
        sku: "BNT-TSA-4024-36LI",
        title: "Tesa 4024 Sessiz Açılan Endüstriyel Ağır Koli Bandı 45mm x 100m Şeffaf (36'lı Koli)",
        category: "packaging",
        categoryName: "Ambalaj & Lojistik",
        brand: "Tesa Tape",
        seller: "Bant & Yapıştırıcı Sanayi Ltd.",
        sellerScore: 9.9,
        sellerBadge: "Toptan Dağıtıcı",
        sellerLocation: "İstanbul (İkitelli OSB)",
        moq: "2 Koli",
        unit: "Koli (36 Rulo)",
        taxText: "+ %20 KDV",
        oldPrice: 1950,
        price: 1540,
        discount: 21,
        rating: 5.0,
        reviewsCount: 1800,
        badge: "Yüksek Yapışma Gücü",
        badgeType: "corporate",
        stockStatus: "800 Koli Stok",
        leadTime: "Aynı Gün Sevk",
        tierPricing: "2-9 Koli: 1.540 ₺ | 10+ Koli: 1.380 ₺",
        image: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 504,
        sku: "LIFT-TRP-2500KG",
        title: "Manuel Hidrolik Transpalet 2.5 Ton Kapasiteli (Ağır Hizmet Poliüretan Çift Tekerlek)",
        category: "packaging",
        categoryName: "Ambalaj & Lojistik",
        brand: "Toros Makine",
        seller: "Yıldız Lojistik & Depo Ekipmanları",
        sellerScore: 9.8,
        sellerBadge: "2 Yıl Garantili",
        sellerLocation: "Mersin (Akdeniz)",
        moq: "1 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 18500,
        price: 14900,
        discount: 19,
        rating: 4.9,
        reviewsCount: 310,
        badge: "CE & TÜV Belgeli",
        badgeType: "industrial",
        stockStatus: "60 Adet Stok",
        leadTime: "Özel Ambar Gönderi",
        tierPricing: "1-2 Adet: 14.900 ₺ | 3+ Adet: 13.600 ₺",
        image: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=500&auto=format&fit=crop&q=80"
    },

    // Kimya, Hammadde & Sanayi Sarfı
    {
        id: 601,
        sku: "KMY-IPA-999-20L",
        title: "İzopropil Alkol (IPA) %99.9 Saf Teknik & Sanayi Sınıfı Çözücü (20 Litre Mühürlü Bidon)",
        category: "chemical",
        categoryName: "Kimya & Hammadde",
        brand: "Zirve Kimya",
        seller: "Zirve Kimya & Hammadde San. Tic. A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "T.C. Bakanlık Onaylı",
        sellerLocation: "Kocaeli (Tuzla / Dilovası)",
        moq: "2 Bidon",
        unit: "Bidon (20L)",
        taxText: "+ %20 KDV",
        oldPrice: 2550,
        price: 1980,
        discount: 22,
        rating: 4.9,
        reviewsCount: 750,
        badge: "MSDS & Analiz Sertifikalı",
        badgeType: "certified",
        stockStatus: "600 Bidon Stok",
        leadTime: "ADR'li Tehlikeli Madde Sevk",
        tierPricing: "2-9 Bidon: 1.980 ₺ | 10+ Bidon: 1.760 ₺",
        image: "https://images.unsplash.com/photo-1532187863486-abf9dbad1b69?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 602,
        sku: "KMY-EPX-ZMN-20KG",
        title: "Endüstriyel Self-Leveling Epoksi Zemin Kaplama Reçine Seti 20 Kg (A+B Komponent)",
        category: "chemical",
        categoryName: "Kimya & Hammadde",
        brand: "EpoxyTech Pro",
        seller: "Zirve Kimya & Hammadde San. Tic. A.Ş.",
        sellerScore: 9.9,
        sellerBadge: "Üretici Tedarikçi",
        sellerLocation: "İstanbul (Tuzla Kimyacılar OSB)",
        moq: "2 Set",
        unit: "Set (20 Kg)",
        taxText: "+ %20 KDV",
        oldPrice: 6200,
        price: 4950,
        discount: 20,
        rating: 4.9,
        reviewsCount: 160,
        badge: "Yüksek Aşınma Direnci",
        badgeType: "industrial",
        stockStatus: "180 Set Stok",
        leadTime: "24 Saatte Teslimat",
        tierPricing: "2-5 Set: 4.950 ₺ | 6+ Set: 4.400 ₺",
        image: "https://images.unsplash.com/photo-1532187863486-abf9dbad1b69?w=500&auto=format&fit=crop&q=80"
    },

    // Kurumsal Ofis & Kağıtçılık
    {
        id: 701,
        sku: "KGT-CPB-A4-80GR-PALET",
        title: "Copier Bond A4 80gr 1. Hamur Fotokopi Kağıdı (1 Palet / 40 Koli / 200 Paket)",
        category: "office",
        categoryName: "Ofis & Kırtasiye",
        brand: "Copier Bond / Vege",
        seller: "Merkez Ofis & Kağıtçılık Sanayi Ltd.",
        sellerScore: 9.9,
        sellerBadge: "Kurumsal Ana Dağıtıcı",
        sellerLocation: "İstanbul (Topkapı Matbaacılar)",
        moq: "1 Palet",
        unit: "Palet (40 Koli)",
        taxText: "+ %20 KDV",
        oldPrice: 32000,
        price: 24800,
        discount: 22,
        rating: 4.9,
        reviewsCount: 2900,
        badge: "Palet Bazlı Toptan Fiyat",
        badgeType: "wholesale",
        stockStatus: "140 Palet Stok",
        leadTime: "Aynı Gün Sevk",
        tierPricing: "1-2 Palet: 24.800 ₺ | 3+ Palet: 22.900 ₺",
        image: "https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=500&auto=format&fit=crop&q=80"
    },
    {
        id: 702,
        sku: "KLT-SDN-THUNDER-PRO",
        title: "Seduna Thunder Pro Ergonomik Fileli Kurumsal Ofis & Personel Çalışma Koltuğu",
        category: "office",
        categoryName: "Ofis & Kırtasiye",
        brand: "Seduna Mobilya",
        seller: "Ofis Mobilya & Tasarım A.Ş.",
        sellerScore: 9.7,
        sellerBadge: "Doğrudan İmalat",
        sellerLocation: "Bursa (İnegöl Mobilya OSB)",
        moq: "5 Adet",
        unit: "Adet",
        taxText: "+ %20 KDV",
        oldPrice: 5800,
        price: 4650,
        discount: 20,
        rating: 4.8,
        reviewsCount: 940,
        badge: "TSE / 3 Yıl Garanti",
        badgeType: "corporate",
        stockStatus: "200 Adet Stokta",
        leadTime: "2-3 İş Günü",
        tierPricing: "5-19 Adet: 4.650 ₺ | 20+ Adet: 4.150 ₺",
        image: "https://images.unsplash.com/photo-1580481077195-730da4b8705a?w=500&auto=format&fit=crop&q=80"
    }
];

// Doğrulanmış Kurumsal Tedarikçi Havuzu
const CORPORATE_VERIFIED_SUPPLIERS = [
    { name: "Atlas Metal & Sac Sanayi A.Ş.", score: 9.9, badge: "Entegre Üretici Fabrika", location: "Kocaeli (Dilovası İMES OSB)", iso: "ISO 9001 / ISO 14001" },
    { name: "Nova Elektronik & Sunucu Sistemleri A.Ş.", score: 9.9, badge: "Yetkili Distribütör", location: "İstanbul (Ümraniye)", iso: "ISO 27001 / TSE Belgeli" },
    { name: "Ege Plastik & Ambalaj Ltd. Şti.", score: 9.9, badge: "Sanayi Tipi Üretici", location: "İzmir (Kemalpaşa OSB)", iso: "BRC / ISO 9001 Onaylı" },
    { name: "Tekno Enerji & Kablo Sanayi A.Ş.", score: 9.9, badge: "TSE & CE Lisanslı Üretici", location: "Gaziantep (4. OSB)", iso: "TSE / VDE / ISO 9001" },
    { name: "Zirve Kimya & Hammadde San. Tic. A.Ş.", score: 9.9, badge: "Sertifikalı Kimyasal Distribütörü", location: "Kocaeli (Tuzla Kimyacılar)", iso: "MSDS / ADR / ISO 9001" },
    { name: "Birlik Rulman & Endüstriyel Tedarik Ltd.", score: 9.8, badge: "Yetkili Ana Distribütör", location: "İstanbul (Perpa Ticaret)", iso: "Sanayi ve Ticaret Odası" },
    { name: "Başak Cıvata & Bağlantı Elemanları A.Ş.", score: 9.8, badge: "Bağlantı Elemanları İmalatı", location: "Konya (Büsan Sanayi)", iso: "DIN / ISO Standartı" },
    { name: "Karadeniz Kereste & Ahşap San. A.Ş.", score: 9.8, badge: "EPAL Lisanslı Üretici", location: "Trabzon (Arsin OSB)", iso: "ISPM-15 / EPAL Lisansı" },
    { name: "Vizyon Endüstriyel Çelik Ltd. Şti.", score: 9.8, badge: "Vasıflı Çelik ve Metal Tedariki", location: "Bursa (NOSAB)", iso: "EN 10204 3.1 Belgeli" },
    { name: "Ağ & Altyapı Çözümleri San. Tic. A.Ş.", score: 9.8, badge: "Kurumsal Çözüm Ortağı", location: "Ankara (ODTÜ Teknokent)", iso: "Cisco & Ubiquiti Partner" },
    { name: "Merkez Ofis & Kağıtçılık Sanayi Ltd.", score: 9.9, badge: "Toptan Kağıt ve Sarf Deposu", location: "İstanbul (Topkapı)", iso: "FSC / ISO Sertifikalı" }
];

// Vitrin State
let currentFilter = 'all';
let currentSort = 'recommended';
let currentSearchQuery = '';
let activeProducts = [];
let favoriteIds = [];

// LocalStorage Favoriler
try {
    const saved = localStorage.getItem('b2b_portal_favorites');
    if (saved) favoriteIds = JSON.parse(saved);
} catch (e) {
    favoriteIds = [];
}

/**
 * 6 Sütun x 8 Satır = 48 Ürünü Kurumsal Havuzdan Dinamik Seç ve Rastgele Çeşitlendir
 */
function generateCorporate48Products() {
    activeProducts = [...B2B_PRODUCT_CATALOG];
    return activeProducts;
}

/**
 * Para Birimi Formatlayıcı (Türk Lirası)
 */
function formatB2BMoney(amount) {
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 }).format(amount || 0);
}

/**
 * Sayfa Yüklendiğinde Vitrini ve Sepet Senkronizasyonunu Başlat
 */
document.addEventListener('DOMContentLoaded', function () {
    // Kurumsal Sepet Durumunu Her Sayfada Güncelle
    updateCartUi();
    // Sunucu ile Sepeti Senkronize Et (Çoklu cihaz/tarayıcı için)
    initB2BCartSync();

    const container = document.getElementById('trendyolProductGrid');
    if (!container) return;

    // Sahte verileri sıfırla ve veritabanındaki gerçek ürünleri yükle
    B2B_PRODUCT_CATALOG.length = 0;
    activeProducts = [];
    setupCorporateEvents();
    loadDbVitrinProducts();
});

/**
 * Veritabanında (items tablosunda) kayıtlı gerçek ürünleri vitrine dahil et
 */
async function loadDbVitrinProducts() {
    const grid = document.getElementById('trendyolProductGrid');
    try {
        const response = await fetch('/Home/GetVitrinProductsJson');
        const data = await response.json();
        if (data.success && Array.isArray(data.products)) {
            B2B_PRODUCT_CATALOG.length = 0;
            data.products.forEach(p => B2B_PRODUCT_CATALOG.push(p));
            activeProducts = [...B2B_PRODUCT_CATALOG];
            renderCorporateVitrin();
        }
    } catch (err) {
        console.warn('DB vitrin ürünleri yüklenirken hata:', err);
        activeProducts = [];
        renderCorporateVitrin();
    }
}

// ==================== B2B KURUMSAL KATEGORİ AĞACI FİLTRELEME MOTORU ====================
let selectedVitrinCategoryId = 0; // 0 = Tüm Kategoriler
let selectedVitrinCategoryName = 'Tüm Kategoriler';
let selectedVitrinCategoryPath = '';

/**
 * Vitrin Kategori Ağacı Panelini Açar/Kapatır (Açılır/Kapanır Kompakt Tasarım)
 */
function toggleVitrinCatTreePanel() {
    const collapseEl = document.getElementById('vitrinCatTreeCollapse');
    const barEl = document.getElementById('vitrinCategoryTreeBar');
    const toggleText = document.getElementById('vitrinToggleText');
    if (!collapseEl) return;

    if (collapseEl.style.display === 'none' || !collapseEl.style.display) {
        collapseEl.style.display = 'block';
        if (barEl) barEl.classList.add('is-expanded');
        if (toggleText) toggleText.textContent = 'Kategori Ağacını Kapat';
    } else {
        collapseEl.style.display = 'none';
        if (barEl) barEl.classList.remove('is-expanded');
        if (toggleText) toggleText.textContent = 'Kategori Ağacını Aç';
    }
}

/**
 * Kategori Ağacındaki Bir Dalı Açar/Kapatır
 */
function toggleVitrinCatTreeBranch(btn, branchId) {
    const target = document.getElementById(branchId);
    if (!target) return;
    if (target.classList.contains('is-hidden')) {
        target.classList.remove('is-hidden');
        btn.classList.remove('is-collapsed');
    } else {
        target.classList.add('is-hidden');
        btn.classList.add('is-collapsed');
    }
}

/**
 * Tüm Kategori Dallarını Genişletir
 */
function expandAllVitrinTree() {
    document.querySelectorAll('#vitrinCategoryTreeList .cat-children-wrap').forEach(el => {
        el.classList.remove('is-hidden');
    });
    document.querySelectorAll('#vitrinCategoryTreeList .cat-toggle-btn').forEach(btn => {
        btn.classList.remove('is-collapsed');
    });
}

/**
 * Tüm Kategori Dallarını Daraltır
 */
function collapseAllVitrinTree() {
    document.querySelectorAll('#vitrinCategoryTreeList .cat-children-wrap').forEach(el => {
        el.classList.add('is-hidden');
    });
    document.querySelectorAll('#vitrinCategoryTreeList .cat-toggle-btn').forEach(btn => {
        btn.classList.add('is-collapsed');
    });
}

/**
 * Kategori Ağacında Canlı Arama Yapar
 */
function filterVitrinTreeNodes(query) {
    const q = (query || '').trim().toLowerCase();
    const nodes = document.querySelectorAll('#vitrinCategoryTreeList .cat-tree-node:not(.is-all-node)');

    if (!q) {
        nodes.forEach(n => n.style.display = '');
        return;
    }

    expandAllVitrinTree();

    nodes.forEach(node => {
        const catName = node.getAttribute('data-cat-name') || '';
        const catPath = (node.getAttribute('data-cat-path') || '').toLowerCase();
        const matchesSelf = catName.includes(q) || catPath.includes(q);

        let hasMatchingChild = false;
        node.querySelectorAll('.cat-tree-node').forEach(child => {
            const childName = child.getAttribute('data-cat-name') || '';
            const childPath = (child.getAttribute('data-cat-path') || '').toLowerCase();
            if (childName.includes(q) || childPath.includes(q)) {
                hasMatchingChild = true;
            }
        });

        if (matchesSelf || hasMatchingChild) {
            node.style.display = '';
        } else {
            node.style.display = 'none';
        }
    });
}

/**
 * Belirli bir kategorinin tüm alt kategorilerinin ID listesini döndürür (Recursive Matching)
 */
function getDescendantCategoryIds(catId) {
    const ids = new Set([catId]);
    if (catId <= 0) return ids;

    const node = document.querySelector(`#vitrinCategoryTreeList .cat-tree-node[data-cat-id="${catId}"]`);
    if (node) {
        node.querySelectorAll('.cat-tree-node').forEach(child => {
            const cid = parseInt(child.getAttribute('data-cat-id'), 10);
            if (!isNaN(cid) && cid > 0) ids.add(cid);
        });
    }
    return ids;
}

/**
 * Kategori Ağacından Bir Kategori Seçildiğinde Çalışır
 */
function selectVitrinCategory(catId, catName, catPath) {
    selectedVitrinCategoryId = parseInt(catId, 10) || 0;
    selectedVitrinCategoryName = catName || 'Tüm Kategoriler';
    selectedVitrinCategoryPath = catPath || '';

    // Active Pill & Rozet Güncellemesi
    const badge = document.getElementById('vitrinActiveCatBadge');
    const badgeName = document.getElementById('vitrinActiveCatName');
    const clearBtn = document.getElementById('btnClearVitrinCatFilter');
    const summaryText = document.getElementById('vitrinCatSummaryText');

    if (badge && badgeName) {
        if (selectedVitrinCategoryId === 0) {
            badge.className = 'b2b-active-cat-pill is-all';
            badgeName.textContent = 'Tüm Kategoriler';
            if (clearBtn) clearBtn.style.display = 'none';
            if (summaryText) summaryText.textContent = 'Hiyerarşik kurumsal kategori sistemimizden seçim yaparak ürünleri anlık filtreleyin.';
        } else {
            badge.className = 'b2b-active-cat-pill is-selected';
            badgeName.textContent = catName;
            if (clearBtn) clearBtn.style.display = 'inline-flex';
            if (summaryText) summaryText.innerHTML = `Seçili Kategori: <strong>${catName}</strong> ${catPath && catPath !== catName ? `(${catPath})` : ''} ve bağlı alt dallar`;
        }
    }

    // Ağaç Satır Vurguları
    document.querySelectorAll('#vitrinCategoryTreeList .cat-node-row').forEach(row => {
        row.classList.remove('is-selected');
    });

    const targetRow = document.getElementById(`vitrinNodeRow-${selectedVitrinCategoryId}`);
    if (targetRow) {
        targetRow.classList.add('is-selected');
    }

    // Vitrini Yeniden Çiz
    renderCorporateVitrin();
}

/**
 * Kategori Filtresini Sıfırlar (Tüm Kategorilere Döner)
 */
function clearVitrinCategoryFilter() {
    selectVitrinCategory(0, 'Tüm Kategoriler', '');
}

/**
 * 6x8 B2B Ürün Izgarasını Render Et
 */
function renderCorporateVitrin() {
    const grid = document.getElementById('trendyolProductGrid');
    const badge = document.getElementById('vitrinProductCount');
    if (!grid) return;

    // Seçili kategoriye ait tüm alt kategorilerin ID seti
    let validCatIds = null;
    if (selectedVitrinCategoryId > 0) {
        validCatIds = getDescendantCategoryIds(selectedVitrinCategoryId);
    }

    // Filtreleme
    let filtered = activeProducts.filter(item => {
        let matchesCategory = true;
        if (selectedVitrinCategoryId > 0) {
            const itemCatId = parseInt(item.categoryId || item.categoryID || 0, 10);
            const itemCatName = (item.categoryName || '').toLowerCase();
            const itemCatPath = (item.categoryPath || '').toLowerCase();
            const targetName = selectedVitrinCategoryName.toLowerCase();

            const idMatch = (itemCatId > 0 && validCatIds && validCatIds.has(itemCatId));
            const nameMatch = itemCatName.includes(targetName) || itemCatPath.includes(targetName);
            matchesCategory = idMatch || nameMatch;
        }

        const matchesSearch = !currentSearchQuery || 
            item.title.toLowerCase().includes(currentSearchQuery.toLowerCase()) ||
            item.sku.toLowerCase().includes(currentSearchQuery.toLowerCase()) ||
            item.brand.toLowerCase().includes(currentSearchQuery.toLowerCase()) ||
            (item.categoryName && item.categoryName.toLowerCase().includes(currentSearchQuery.toLowerCase())) ||
            (item.categoryPath && item.categoryPath.toLowerCase().includes(currentSearchQuery.toLowerCase())) ||
            item.seller.toLowerCase().includes(currentSearchQuery.toLowerCase());

        return matchesCategory && matchesSearch;
    });

    // Sıralama
    if (currentSort === 'price-asc') {
        filtered.sort((a, b) => a.price - b.price);
    } else if (currentSort === 'price-desc') {
        filtered.sort((a, b) => b.price - a.price);
    } else if (currentSort === 'score') {
        filtered.sort((a, b) => b.sellerScore - a.sellerScore);
    } else if (currentSort === 'discount') {
        filtered.sort((a, b) => b.discount - a.discount);
    }

    if (badge) {
        badge.textContent = `${filtered.length} Ürün Listeleniyor`;
    }

    if (filtered.length === 0) {
        grid.innerHTML = `
            <div class="b2b-empty-state" style="grid-column: 1 / -1; text-align: center; padding: 48px 20px; background: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 16px;">
                <div class="empty-icon" style="font-size: 2.8rem; margin-bottom: 12px;">📁</div>
                <h3 style="font-size: 1.15rem; font-weight: 800; color: #0f172a; margin-bottom: 6px;">Seçili kategori veya aramaya uygun kurumsal ürün bulunamadı</h3>
                <p style="color: #64748b; font-size: 0.85rem; max-width: 420px; margin: 0 auto 16px auto;">
                    Farklı bir kategori seçebilir veya tüm kurumsal kataloğu görüntülemek için filtreyi temizleyebilirsiniz.
                </p>
                <button type="button" class="btn-primary" onclick="resetB2BFilters()" style="display: inline-flex; align-items: center; gap: 6px; padding: 9px 18px;">
                    Tüm Ürünleri Listele
                </button>
            </div>
        `;
        return;
    }

    let html = '';
    filtered.forEach((item) => {
        const isFav = favoriteIds.includes(item.id);
        const sellerId = getSellerCompanyId(item);
        const scoreNum = typeof item.sellerScore === 'number' ? item.sellerScore.toFixed(1) : (item.sellerScore || '10.0');
        const isOutOfStock = (typeof item.stock === 'number' && item.stock <= 0) || item.isOutOfStock || (item.stockStatus && item.stockStatus.toLowerCase().includes('tükendi'));
        const cleanStock = isOutOfStock ? 'Stok Tükenmiştir' : (item.stockStatus || 'Hazır Stok').replace(/📦|📦|📦/g, '').trim();
        const cleanCategory = (item.categoryName || '').replace(/[^\w\sğüşıöçĞÜŞİÖÇ\/-]/gi, '').trim();

        html += `
            <article class="b2b-product-card ${isOutOfStock ? 'out-of-stock' : ''}" data-id="${item.id}" data-category-id="${item.categoryId || 0}" data-category="${item.categoryName || ''}">
                <!-- Kart Üst Barı: Tedarikçi (Sol) & Değerlendirme Puanı (Sağ) -->
                <div class="b2b-card-supplier-bar">
                    <a href="/Seller/${sellerId}" target="_blank" class="b2b-verified-badge" title="${item.seller} - Kurumsal Tedarikçi Profili" style="text-decoration: none; color: inherit; display: inline-flex; align-items: center; gap: 4px;">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="#059669" stroke="currentColor" stroke-width="1.5">
                            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                            <polyline points="22 4 12 14.01 9 11.01" stroke="#fff" stroke-width="2.5"></polyline>
                        </svg>
                        <span class="supplier-name-text">${item.seller}</span>
                    </a>
                    <a href="/Seller/${sellerId}" target="_blank" class="b2b-score-badge" title="${item.seller} - ${item.sellerReviewCount || 0} Adet Kurumsal Değerlendirme" style="text-decoration: none; display: inline-flex; align-items: center; gap: 2px; background: rgba(245,158,11,0.12); color: #b45309; border: 1px solid rgba(245,158,11,0.25); padding: 1px 6px; border-radius: 4px; font-weight: 800; font-size: 0.72rem;">
                        <span style="color: #d97706; font-size: 0.76rem;">★</span>
                        <span>${scoreNum}</span>
                        ${item.sellerReviewCount ? `<span style="font-weight: 600; opacity: 0.85; margin-left: 2px;">(${item.sellerReviewCount})</span>` : ''}
                    </a>
                </div>

                <!-- Görsel ve Kategori Rozeti -->
                <div class="b2b-img-container">
                    <a href="/Product/${item.id}" style="display:block; width:100%; height:100%; text-decoration:none;">
                        <img src="${item.image || '/images/default-product.svg'}" alt="${item.title}" loading="lazy" class="b2b-img" onerror="this.src='/images/default-product.svg'" />
                    </a>
                    
                    <div class="b2b-badges-stack">
                        ${isOutOfStock ? `<span class="b2b-badge" style="background: #dc2626; color: #ffffff; font-weight: 800;">Tükendi</span>` : ''}
                        ${cleanCategory ? `<span class="b2b-badge badge-corp" style="background: rgba(15,23,42,0.85); color: #ffffff;">${cleanCategory}</span>` : ''}
                    </div>

                    <!-- Favori / Takip Butonu -->
                    <button type="button" class="b2b-fav-btn ${isFav ? 'active' : ''}" onclick="toggleB2BFavorite(event, ${item.id})" title="${isFav ? 'Takip Listesinden Çıkar' : 'Takip Listesine Ekle'}">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="${isFav ? '#e11d48' : 'none'}" stroke="${isFav ? '#e11d48' : '#64748b'}" stroke-width="2">
                            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                        </svg>
                    </button>

                    <!-- Ürün Detay Sayfası Butonu -->
                    <a href="/Product/${item.id}" class="b2b-quick-look-btn" style="text-decoration:none;">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                            <circle cx="12" cy="12" r="3"></circle>
                        </svg>
                        Ürün Detayı →
                    </a>
                </div>

                <!-- Ürün Gövdesi -->
                <div class="b2b-body">
                    <!-- SKU & Marka -->
                    <div class="b2b-meta-line">
                        <span class="b2b-brand-label">${item.brand}</span>
                        <span class="b2b-sku-code" title="Stok Kodu / OEM">${item.sku}</span>
                    </div>

                    <!-- Ürün Başlığı -->
                    <h3 class="b2b-title" title="${item.title}">
                        <a href="/Product/${item.id}" style="text-decoration:none; color:inherit;">
                            ${item.title}
                        </a>
                    </h3>

                    <!-- Stok Durumu -->
                    <div class="b2b-spec-bar" style="justify-content: flex-start;">
                        <span class="b2b-stock-chip" style="display: inline-flex; align-items: center; gap: 5px; color: ${isOutOfStock ? '#dc2626' : '#059669'}; font-weight: 700; font-size: 0.73rem; max-width: none;">
                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="${isOutOfStock ? '#dc2626' : '#059669'}" stroke-width="2" style="flex-shrink: 0;">
                                <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
                                <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
                                <line x1="12" y1="22.08" x2="12" y2="12"></line>
                            </svg>
                            <span>${cleanStock}</span>
                        </span>
                    </div>

                    <!-- Fiyat Bloğu -->
                    <div class="b2b-pricing-box" style="margin-bottom: 10px; padding-top: 6px; border-top: 1px dashed #e2e8f0;">
                        <div class="b2b-current-price-line">
                            <span class="b2b-price-val">${formatB2BMoney(item.price)}</span>
                            <span class="b2b-tax-text">${item.taxText || '+ %20 KDV'}</span>
                        </div>
                    </div>

                    <!-- Aksiyon Butonları (Satın Al & Sepete Ekle) -->
                    <div class="b2b-actions">
                        ${isOutOfStock ? `
                            <button type="button" class="b2b-btn-buy" disabled style="background: #cbd5e1; color: #64748b; cursor: not-allowed; opacity: 0.85;" title="Bu ürünün stoğu tükenmiştir">
                                Tükendi
                            </button>
                            <button type="button" class="b2b-btn-cart" disabled style="background: #f1f5f9; color: #94a3b8; cursor: not-allowed; opacity: 0.6;" title="Stok tükenmiştir">
                                ✕
                            </button>
                        ` : `
                            <button type="button" class="b2b-btn-buy" onclick="directBuy(${item.id})" title="Bu ürünü doğrudan kurumsal siparişle satın alın">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                                    <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
                                </svg>
                                ⚡ Hemen Al
                            </button>
                            <button type="button" class="b2b-btn-cart" onclick="addToCart(${item.id})" title="Sepete Ekle">
                                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <circle cx="9" cy="21" r="1"></circle>
                                    <circle cx="20" cy="21" r="1"></circle>
                                    <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                                </svg>
                            </button>
                        `}
                        <a href="/Product/${item.id}" class="b2b-btn-contact" title="Ürün Detay Sayfası ve Şartnameleri Gör" style="display:inline-flex; align-items:center; justify-content:center; text-decoration:none;">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="12" cy="12" r="10"></circle>
                                <line x1="12" y1="16" x2="12" y2="12"></line>
                                <line x1="12" y1="8" x2="12.01" y2="8"></line>
                            </svg>
                        </a>
                    </div>
                </div>
            </article>
        `;
    });

    grid.innerHTML = html;
}

/**
 * Olay Dinleyicileri (Event Listeners)
 */
function setupCorporateEvents() {
    // Canlı Arama
    const searchInput = document.getElementById('vitrinSearchInput');
    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            currentSearchQuery = e.target.value.trim();
            renderCorporateVitrin();
        });
    }

    // Sıralama Seçimi
    const sortSelect = document.getElementById('vitrinSortSelect');
    if (sortSelect) {
        sortSelect.addEventListener('change', function () {
            currentSort = this.value;
            renderCorporateVitrin();
        });
    }

    // Dinamik Karıştırma / Yenileme Butonu
    const shuffleBtn = document.getElementById('btnShuffleProducts');
    if (shuffleBtn) {
        shuffleBtn.addEventListener('click', function () {
            this.classList.add('spinning');
            setTimeout(() => this.classList.remove('spinning'), 600);

            generateCorporate48Products();
            renderCorporateVitrin();
            showB2BToast("🏢 48 yeni doğrulanmış kurumsal ürün ve fabrika ilanı yüklendi.", "info");
        });
    }
}

/**
 * Filtreleri Sıfırla
 */
function resetB2BFilters() {
    selectedVitrinCategoryId = 0;
    selectedVitrinCategoryName = 'Tüm Kategoriler';
    selectedVitrinCategoryPath = '';
    currentSearchQuery = '';
    currentSort = 'recommended';

    const searchInput = document.getElementById('vitrinSearchInput');
    if (searchInput) searchInput.value = '';

    const sortSelect = document.getElementById('vitrinSortSelect');
    if (sortSelect) sortSelect.value = 'recommended';

    selectVitrinCategory(0, 'Tüm Kategoriler', '');
}

/**
 * Takip / Favori Listesi Değiştirme
 */
function toggleB2BFavorite(e, id) {
    e.stopPropagation();
    const idx = favoriteIds.indexOf(id);
    const btn = e.currentTarget;

    if (idx > -1) {
        favoriteIds.splice(idx, 1);
        btn.classList.remove('active');
        const svg = btn.querySelector('svg');
        if (svg) {
            svg.setAttribute('fill', 'none');
            svg.setAttribute('stroke', '#64748b');
        }
        showB2BToast("Ürün kurumsal takip listenizden çıkarıldı.", "default");
    } else {
        favoriteIds.push(id);
        btn.classList.add('active');
        const svg = btn.querySelector('svg');
        if (svg) {
            svg.setAttribute('fill', '#e11d48');
            svg.setAttribute('stroke', '#e11d48');
        }
        showB2BToast("📌 Ürün kurumsal satınalma listenize eklendi.", "success");
    }

    try {
        localStorage.setItem('b2b_portal_favorites', JSON.stringify(favoriteIds));
    } catch (err) {}
}

/**
 * Ürün Detay Sayfasına Yönlendirir (Eski modal yerine direkt müstakil ürün sayfasına gider)
 */
function openProductQuickView(productId) {
    if (productId) {
        window.location.href = `/Product/${productId}`;
    }
}

function _legacyOpenProductQuickView(productId) {
    if (qvSellerLink) {
        qvSellerLink.href = `/Seller/${getSellerCompanyId(product)}`;
        qvSellerLink.textContent = `${product.seller} Sayfası & Değerlendirmeleri Gör →`;
    }
    document.getElementById('qvModalPrice').textContent = formatB2BMoney(product.price);
    const oldPriceEl = document.getElementById('qvModalOldPrice');
    if (oldPriceEl) oldPriceEl.textContent = '';
    const discountEl = document.getElementById('qvModalDiscount');
    if (discountEl) discountEl.textContent = '';

    document.getElementById('qvModalCategory').textContent = (product.categoryName || '').replace(/[^\w\sğüşıöçĞÜŞİÖÇ\/-]/gi, '').trim();
    document.getElementById('qvModalStock').textContent = (product.stockStatus || 'Hazır Stok').replace(/📦|📦|📦/g, '').trim();
    
    const moqEl = document.getElementById('qvModalMoq');
    if (moqEl) moqEl.textContent = '';
    const leadTimeEl = document.getElementById('qvModalLeadTime');
    if (leadTimeEl) leadTimeEl.textContent = (product.leadTime || '1-2 İş Günü').replace(/🚚/g, '').trim();
    const tierPricingEl = document.getElementById('qvModalTierPricing');
    if (tierPricingEl) tierPricingEl.style.display = 'none';

    const modalQuoteBtn = document.getElementById('qvModalQuoteBtn');
    if (modalQuoteBtn) {
        modalQuoteBtn.innerHTML = `
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
            </svg>
            ⚡ Hemen Al
        `;
        modalQuoteBtn.onclick = function() {
            closeTyModal('tyQuickViewModal');
            directBuy(productId);
        };
    }

    const modalSellBtn = document.getElementById('qvModalSellBtn');
    if (modalSellBtn) {
        modalSellBtn.innerHTML = `
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="9" cy="21" r="1"></circle>
                <circle cx="20" cy="21" r="1"></circle>
                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
            </svg>
            Sepete Ekle
        `;
        modalSellBtn.onclick = function() {
            closeTyModal('tyQuickViewModal');
            addToCart(productId, 1, true);
        };
    }

    modal.style.display = 'flex';
    modal.style.pointerEvents = 'auto';
    requestAnimationFrame(() => {
        modal.classList.add('show');
        modal.classList.add('is-open');
    });
}

/**
 * Kurumsal Teklif (RFQ) İsteme Modalı
 */
function openQuoteModal(productId) {
    const product = activeProducts.find(p => p.id === productId) || (typeof B2B_PRODUCT_CATALOG !== 'undefined' ? B2B_PRODUCT_CATALOG.find(p => p.id === productId) : null);
    if (!product) return;

    const modal = document.getElementById('tyQuoteModal');
    if (!modal) return;

    document.getElementById('quoteModalProductTitle').textContent = product.title;
    document.getElementById('quoteModalProductSku').textContent = `Stok Kodu (SKU): ${product.sku} | Marka: ${product.brand}`;
    document.getElementById('quoteModalSellerName').textContent = product.seller;
    document.getElementById('quoteModalUnitPrice').textContent = `${formatB2BMoney(product.price)} ${product.taxText || '+ KDV'}`;
    document.getElementById('quoteModalHiddenProductId').value = product.id;

    modal.style.display = 'flex';
    modal.style.pointerEvents = 'auto';
    requestAnimationFrame(() => {
        modal.classList.add('show');
        modal.classList.add('is-open');
    });
}

/**
 * Tedarikçi Olarak Ürün Satışı Başlatma Modalı
 */
function openSellerOfferModal(productId) {
    const product = productId ? (activeProducts.find(p => p.id === productId) || (typeof B2B_PRODUCT_CATALOG !== 'undefined' ? B2B_PRODUCT_CATALOG.find(p => p.id === productId) : null)) : null;
    const modal = document.getElementById('tySellerOfferModal');
    if (!modal) return;

    const nameInput = document.getElementById('sellerOfferProductName');
    const priceInput = document.getElementById('sellerOfferPrice');
    const stockInput = document.getElementById('sellerOfferStock');

    if (product) {
        document.getElementById('sellerModalProductInfo').innerHTML = `
            <strong>Referans Alınan Ürün:</strong> ${product.title} <br/>
            <span style="font-size:0.78rem; color:#64748b;">SKU: ${product.sku} | Mevcut Kategori: ${product.categoryName}</span>
        `;
        if (nameInput) nameInput.value = product.title;
        if (priceInput) priceInput.value = product.price;
        if (stockInput) stockInput.value = '100';

        // Kategori ağacında eşleşen düğümü bul ve seç
        autoSelectCatInTree(product.categoryName);
    } else {
        document.getElementById('sellerModalProductInfo').innerHTML = `
            <strong>Yeni Kurumsal Portföy & Toptan Satış İlanı</strong> <br/>
            <span style="font-size:0.78rem; color:#64748b;">Doğrudan B2B satın alma yöneticilerine ve fabrikalara ulaşın.</span>
        `;
        if (nameInput) nameInput.value = '';
        if (priceInput) priceInput.value = '';
        if (stockInput) stockInput.value = '50';
        clearCatTreeSelection();
    }

    modal.style.display = 'flex';
    modal.style.pointerEvents = 'auto';
    requestAnimationFrame(() => {
        modal.classList.add('show');
        modal.classList.add('is-open');
    });
}

/**
 * Kompakt Kategori Ağacı Seçici Fonksiyonları (SuperAdmin Esintili)
 */
function toggleCatTreeBranch(btn, targetId) {
    const target = document.getElementById(targetId);
    if (!target) return;
    if (target.classList.contains('is-hidden')) {
        target.classList.remove('is-hidden');
        btn.classList.remove('is-collapsed');
    } else {
        target.classList.add('is-hidden');
        btn.classList.add('is-collapsed');
    }
}

function expandAllCatTreePicker() {
    document.querySelectorAll('#catTreePickerList .cat-children-wrap').forEach(el => {
        el.classList.remove('is-hidden');
    });
    document.querySelectorAll('#catTreePickerList .cat-toggle-btn').forEach(btn => {
        btn.classList.remove('is-collapsed');
    });
}

function collapseAllCatTreePicker() {
    document.querySelectorAll('#catTreePickerList .cat-children-wrap').forEach(el => {
        el.classList.add('is-hidden');
    });
    document.querySelectorAll('#catTreePickerList .cat-toggle-btn').forEach(btn => {
        btn.classList.add('is-collapsed');
    });
}

function filterCatTreePicker(query) {
    const q = (query || '').trim().toLowerCase();
    const nodes = document.querySelectorAll('#catTreePickerList .cat-tree-node');

    if (!q) {
        nodes.forEach(n => n.style.display = '');
        return;
    }

    expandAllCatTreePicker();

    nodes.forEach(node => {
        const catName = node.getAttribute('data-cat-name') || '';
        const catPath = (node.getAttribute('data-cat-path') || '').toLowerCase();
        const matchesSelf = catName.includes(q) || catPath.includes(q);

        let hasMatchingChild = false;
        node.querySelectorAll('.cat-tree-node').forEach(child => {
            const childName = child.getAttribute('data-cat-name') || '';
            const childPath = (child.getAttribute('data-cat-path') || '').toLowerCase();
            if (childName.includes(q) || childPath.includes(q)) {
                hasMatchingChild = true;
            }
        });

        if (matchesSelf || hasMatchingChild) {
            node.style.display = '';
        } else {
            node.style.display = 'none';
        }
    });
}

function selectCatFromTree(catId, catName, breadcrumbPath) {
    const hiddenCatInput = document.getElementById('sellerOfferCategory');
    const hiddenIdInput = document.getElementById('sellerOfferCategoryId');
    const displayBox = document.getElementById('catTreeSelectedDisplay');
    const displayText = document.getElementById('catTreeSelectedText');
    const displayIcon = document.getElementById('catTreeSelectedIcon');

    if (hiddenCatInput) hiddenCatInput.value = catName;
    if (hiddenIdInput) hiddenIdInput.value = catId;

    if (displayBox && displayText) {
        displayBox.classList.remove('is-empty');
        displayBox.style.background = '#eff6ff';
        displayBox.style.borderColor = '#93c5fd';
        displayBox.style.color = '#1d4ed8';
        displayText.innerHTML = `<strong style="color: #1d4ed8;">${catName}</strong> <span style="font-size:0.74rem; opacity:0.85; margin-left:4px;">(${breadcrumbPath || catName})</span>`;
        if (displayIcon) displayIcon.innerHTML = '<span style="color: #059669; font-weight: 800; margin-right: 2px;">✓</span>';
    }

    // Seçilen satırı görsel olarak vurgula
    document.querySelectorAll('#catTreePickerList .cat-node-row').forEach(row => {
        row.classList.remove('is-selected');
    });

    const targetNode = document.querySelector(`#catTreePickerList .cat-tree-node[data-cat-id="${catId}"] > .cat-node-row`);
    if (targetNode) {
        targetNode.classList.add('is-selected');
    }
}

function autoSelectCatInTree(catName) {
    if (!catName) return;
    const searchCat = catName.toLowerCase();
    const allNodes = document.querySelectorAll('#catTreePickerList .cat-tree-node');
    let matched = false;

    for (let i = 0; i < allNodes.length; i++) {
        const node = allNodes[i];
        const name = (node.getAttribute('data-cat-name') || '').toLowerCase();
        const path = (node.getAttribute('data-cat-path') || '').toLowerCase();
        const id = node.getAttribute('data-cat-id');

        if (name === searchCat || path.includes(searchCat) || searchCat.includes(name)) {
            const rawName = node.querySelector('.cat-node-title')?.textContent?.trim() || catName;
            selectCatFromTree(id, rawName, node.getAttribute('data-cat-path') || rawName);
            matched = true;
            break;
        }
    }

    if (!matched) {
        selectCatFromTree(1, catName, catName);
    }
}

function clearCatTreeSelection() {
    const hiddenCatInput = document.getElementById('sellerOfferCategory');
    const hiddenIdInput = document.getElementById('sellerOfferCategoryId');
    const displayBox = document.getElementById('catTreeSelectedDisplay');
    const displayText = document.getElementById('catTreeSelectedText');
    const displayIcon = document.getElementById('catTreeSelectedIcon');

    if (hiddenCatInput) hiddenCatInput.value = '';
    if (hiddenIdInput) hiddenIdInput.value = '';

    if (displayBox && displayText) {
        displayBox.classList.add('is-empty');
        displayBox.style.background = '';
        displayBox.style.borderColor = '';
        displayBox.style.color = '';
        displayText.textContent = 'Lütfen aşağıdaki listeden bir kategori seçiniz...';
        if (displayIcon) displayIcon.innerHTML = '';
    }

    document.querySelectorAll('#catTreePickerList .cat-node-row').forEach(row => {
        row.classList.remove('is-selected');
    });
}

/**
 * Modal Kapat
 */
function closeTyModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.remove('show');
        modal.classList.remove('is-open');
        modal.style.display = 'none';
        modal.style.pointerEvents = 'none';
    }
}

/**
 * RFQ ve Tedarikçi Form Gönderimleri (Backend Entegrasyonlu)
 */
async function submitQuoteRequest(e) {
    if (e) {
        if (typeof e.preventDefault === 'function') e.preventDefault();
        if (typeof e.stopPropagation === 'function') e.stopPropagation();
    }
    const productId = document.getElementById('quoteModalHiddenProductId')?.value || '0';
    const qty = parseInt(document.getElementById('quoteQuantity')?.value || '50', 10);
    const targetPrice = document.getElementById('quoteTargetPrice')?.value ? parseFloat(document.getElementById('quoteTargetPrice').value) : null;
    const terms = document.getElementById('quotePaymentTerms')?.value || 'Peşin İskontolu';
    const buyerCompany = document.getElementById('quoteBuyerCompany')?.value || 'Kurumsal Müşteri';
    const buyerTax = document.getElementById('quoteBuyerTax')?.value || '';
    const deliveryCity = document.getElementById('quoteDeliveryCity')?.value || '';
    const notes = document.getElementById('quoteNotes')?.value || '';
    const sampleChecked = document.getElementById('quoteSampleCheck')?.checked || false;

    const formEl = document.getElementById('quoteRequestForm') || (e && e.target ? e.target : null);
    const submitBtn = formEl ? formEl.querySelector('button[type="submit"]') : null;
    const originalBtnHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span>⏳ Gönderiliyor...</span>';
    }

    try {
        const response = await fetch('/Home/SubmitQuoteRequest', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                productID: parseInt(productId, 10) || 0,
                buyerCompany: buyerCompany,
                buyerTax: buyerTax,
                quantity: qty,
                targetPrice: targetPrice,
                paymentTerms: terms,
                deliveryCity: deliveryCity,
                notes: notes,
                sampleRequested: sampleChecked
            })
        });

        const result = await response.json();
        if (result.success) {
            closeTyModal('tyQuoteModal');
            showB2BToast(result.message, "success");
            e.target.reset();
        } else {
            showB2BToast("⚠️ " + (result.message || "Teklif talebi iletilemedi."), "danger");
        }
    } catch (err) {
        showB2BToast("⚠️ Sunucu bağlantı hatası: " + err.message, "danger");
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnHtml;
        }
    }

    return false;
}

async function submitSellerOffer(e) {
    if (e) {
        if (typeof e.preventDefault === 'function') e.preventDefault();
        if (typeof e.stopPropagation === 'function') e.stopPropagation();
    }
    const productName = document.getElementById('sellerOfferProductName')?.value || '';
    const category = document.getElementById('sellerOfferCategory')?.value || '';
    const categoryId = document.getElementById('sellerOfferCategoryId')?.value || '';
    const price = parseFloat(document.getElementById('sellerOfferPrice')?.value || '0');
    const stock = document.getElementById('sellerOfferStock')?.value || '';
    const desc = document.getElementById('sellerOfferDesc')?.value || '';

    if (!productName.trim()) {
        showB2BToast("⚠️ Lütfen ürün adını giriniz.", "danger");
        document.getElementById('sellerOfferProductName')?.focus();
        return false;
    }

    if (!category) {
        showB2BToast("⚠️ Lütfen ürünün dahil olduğu kategoriyi kategori ağacından seçiniz.", "info");
        document.getElementById('catTreePickerSearch')?.focus();
        return false;
    }

    if (isNaN(price) || price <= 0) {
        showB2BToast("⚠️ Lütfen geçerli bir birim fiyat giriniz.", "danger");
        document.getElementById('sellerOfferPrice')?.focus();
        return false;
    }

    const formEl = document.getElementById('sellerOfferForm') || (e && e.target ? e.target : null);
    const submitBtn = formEl ? formEl.querySelector('button[type="submit"]') : null;
    const originalBtnHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span>⏳ Kaydediliyor ve Yayına Alınıyor...</span>';
    }

    try {
        const response = await fetch('/Home/SubmitSellerOffer', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                productName: productName.trim(),
                categoryID: categoryId ? parseInt(categoryId, 10) : null,
                categoryName: category,
                unitPrice: price,
                stockQuantity: stock,
                itemDescription: desc
            })
        });

        const result = await response.json();
        if (result.success) {
            closeTyModal('tySellerOfferModal');
            showB2BToast(result.message + " İlanınızı 'Satıştaki Ürünlerim' panelinden yönetebilirsiniz.", "success");

            // Formu sıfırla
            if (formEl && typeof formEl.reset === 'function') formEl.reset();
            clearCatTreeSelection();
        } else if (result.requireLogin) {
            showB2BToast("🔒 " + (result.message || "Lütfen kurumsal satıcı hesabınızla giriş yapınız."), "warning");
            setTimeout(() => {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            }, 1500);
        } else if (result.requireIyzico) {
            closeTyModal('tySellerOfferModal');
            showB2BToast("🛡️ " + (result.message || "Satış yapabilmek için iyzico satıcı kaydınızı tamamlayınız."), "warning");
            setTimeout(() => {
                window.location.href = '/Dashboard/SupplierOrders';
            }, 1800);
        } else {
            showB2BToast("⚠️ " + (result.message || "İlan kaydedilemedi."), "danger");
        }
    } catch (err) {
        showB2BToast("⚠️ Sunucu bağlantı hatası: " + err.message, "danger");
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnHtml;
        }
    }

    return false;
}

/**
 * Kurumsal Toast Bildirimleri
 */
function showB2BToast(message, type = "default") {
    let container = document.getElementById('tyToastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'tyToastContainer';
        container.className = 'ty-toast-container';
        document.body.appendChild(container);
    }

    // Modalların ve her katmanın en üstünde olmasını garanti et
    container.style.setProperty('z-index', '2147483647', 'important');
    container.style.setProperty('position', 'fixed', 'important');
    container.style.setProperty('pointer-events', 'none', 'important');

    const toast = document.createElement('div');
    toast.className = `ty-toast ty-toast-${type}`;
    toast.style.setProperty('z-index', '2147483647', 'important');
    toast.style.setProperty('pointer-events', 'auto', 'important');
    toast.innerHTML = `
        <div class="ty-toast-content">
            <span class="ty-toast-msg">${message}</span>
        </div>
        <button class="ty-toast-close" onclick="this.parentElement.remove()">&times;</button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.add('ty-toast-fadeout');
        setTimeout(() => toast.remove(), 400);
    }, 4800);
}

/* =========================================================================
   =================== B2B KURUMSAL SEPET VE CHECKOUT SİSTEMİ ===================
   ========================================================================= */

/**
 * LocalStorage üzerinden kayıtlı kurumsal sepeti getirir
 */
function getB2BCart() {
    try {
        const stored = localStorage.getItem('b2b_portal_cart');
        return stored ? JSON.parse(stored) : [];
    } catch (e) {
        console.error("Sepet okunurken hata:", e);
        return [];
    }
}

/**
 * Sepeti LocalStorage'a kaydeder ve UI'ı günceller
 */
function saveB2BCart(cart) {
    try {
        localStorage.setItem('b2b_portal_cart', JSON.stringify(cart));
    } catch (e) {
        console.error("Sepet kaydedilirken hata:", e);
    }
    updateCartUi();
}

/**
 * Sunucu ile sepet senkronizasyonu (Farklı cihaz ve tarayıcılardan giriş yapıldığında sepeti korur)
 */
async function initB2BCartSync() {
    try {
        const localCart = getB2BCart();
        const response = await fetch('/Cart/SyncCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(localCart.map(i => ({
                productID: i.productId || i.productID,
                title: i.title,
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
            // Sunucudaki güncel ve senkronize edilmiş sepeti yerel hafızaya kaydet
            try {
                localStorage.setItem('b2b_portal_cart', JSON.stringify(data.items));
            } catch (e) {}
            updateCartUi();
            if (typeof renderFullCartPage === 'function') {
                renderFullCartPage();
            }
            if (document.getElementById('b2bCartDrawerOverlay') && document.getElementById('b2bCartDrawerOverlay').style.display === 'flex') {
                renderCartDrawer();
            }
        }
    } catch (err) {
        console.warn("Sepet senkronizasyon hatası:", err);
    }
}

/**
 * Sepete ürün ekler (Sunucu doğrulamalı)
 */
async function addToCart(productId, qty = 1, showDrawer = false) {
    const product = activeProducts.find(p => p.id === productId) || (typeof B2B_PRODUCT_CATALOG !== 'undefined' ? B2B_PRODUCT_CATALOG.find(p => p.id === productId) : null);
    if (!product) {
        showB2BToast("⚠️ Ürün bilgisi bulunamadı.", "danger");
        return false;
    }

    const availableStock = typeof product.stock === 'number' ? product.stock : (product.isOutOfStock ? 0 : 9999);
    if (availableStock <= 0 || product.isOutOfStock || (product.stockStatus && product.stockStatus.toLowerCase().includes('tükendi'))) {
        showB2BToast(`⚠️ "${product.title}" ürününün stoğu tükenmiştir, sepete eklenemez.`, "danger");
        return false;
    }

    let cart = getB2BCart();
    const existingIndex = cart.findIndex(item => item.productId === productId);
    const currentInCart = existingIndex > -1 ? cart[existingIndex].quantity : 0;
    const requestedTotal = currentInCart + qty;

    if (requestedTotal > availableStock) {
        showB2BToast(`⚠️ Yetersiz stok! Bu üründen en fazla ${availableStock} adet alabilirsiniz (Sepetinizde: ${currentInCart} adet).`, "danger");
        return false;
    }

    const itemToAdd = {
        productId: product.id,
        title: product.title,
        sku: product.sku || `SKU-${product.id}`,
        category: product.categoryName || product.category || 'Endüstriyel',
        price: product.price,
        quantity: qty,
        maxStock: availableStock,
        seller: product.seller || 'Onaylı Tedarikçi',
        image: product.image || ''
    };

    try {
        // Sunucu doğrulaması (Kendi ürününü satın alma ve stok engeli)
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                productID: itemToAdd.productId,
                title: itemToAdd.title,
                sku: itemToAdd.sku,
                category: itemToAdd.category,
                price: itemToAdd.price,
                quantity: qty,
                seller: itemToAdd.seller,
                image: itemToAdd.image
            })
        });

        const data = await response.json();
        if (data && data.requireLogin) {
            showB2BToast("🔒 " + (data.message || "Sepete ürün eklemek için lütfen kurumsal hesabınızla giriş yapınız."), "warning");
            setTimeout(() => {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            }, 1200);
            return false;
        }

        if (data && data.success === false) {
            showB2BToast("⚠️ " + (data.message || "Bu ürünü sepete ekleyemezsiniz."), "danger");
            return false;
        }

        if (existingIndex > -1) {
            cart[existingIndex].quantity = requestedTotal;
            cart[existingIndex].maxStock = availableStock;
        } else {
            cart.push(itemToAdd);
        }

        saveB2BCart(cart);
        showB2BToast(`"${product.title}" sepete eklendi!`, "success");

        if (showDrawer) {
            openCartDrawer();
        }
        return true;
    } catch (e) {
        console.warn("AddToCart error:", e);
        return false;
    }
}

/**
 * Doğrudan Satın Al (Hemen Satın Al): Ürünü sepete ekleyip direkt Checkout modalını açar
 */
async function directBuy(productId) {
    const product = activeProducts.find(p => p.id === productId) || (typeof B2B_PRODUCT_CATALOG !== 'undefined' ? B2B_PRODUCT_CATALOG.find(p => p.id === productId) : null);
    if (product) {
        const availableStock = typeof product.stock === 'number' ? product.stock : (product.isOutOfStock ? 0 : 9999);
        if (availableStock <= 0 || product.isOutOfStock || (product.stockStatus && product.stockStatus.toLowerCase().includes('tükendi'))) {
            showB2BToast(`⚠️ "${product.title}" ürününün stoğu tükenmiştir, satın alınamaz.`, "danger");
            return;
        }
    }

    const success = await addToCart(productId, 1, false);
    if (success) {
        openCheckoutModal();
    }
}

/**
 * Sepetten ürünü çıkarır
 */
function removeFromCart(productId) {
    let cart = getB2BCart();
    cart = cart.filter(item => item.productId !== productId);
    saveB2BCart(cart);
    renderCartDrawer();
    showB2BToast("Ürün sepetten kaldırıldı.", "info");

    fetch('/Cart/RemoveFromCart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productID: productId })
    }).catch(e => console.warn("RemoveFromCart server sync:", e));
}

/**
 * Sepetteki ürünün miktarını günceller
 */
async function updateCartQty(productId, newQty) {
    let cart = getB2BCart();
    const item = cart.find(i => i.productId === productId);
    if (item) {
        if (newQty <= 0) {
            removeFromCart(productId);
            return;
        }

        const product = activeProducts.find(p => p.id === productId) || (typeof B2B_PRODUCT_CATALOG !== 'undefined' ? B2B_PRODUCT_CATALOG.find(p => p.id === productId) : null);
        const maxStock = (product && typeof product.stock === 'number') ? product.stock : (item.maxStock || 9999);

        if (maxStock <= 0) {
            showB2BToast(`⚠️ "${item.title}" ürününün stoğu tükenmiştir.`, "danger");
            removeFromCart(productId);
            return;
        }

        if (newQty > maxStock) {
            showB2BToast(`⚠️ Yetersiz stok! Bu ürün için mevcut maksimum stok ${maxStock} adettir.`, "danger");
            item.quantity = maxStock;
            saveB2BCart(cart);
            renderCartDrawer();
            return;
        }

        try {
            const res = await fetch('/Cart/UpdateQuantity', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ productID: productId, quantity: newQty })
            });
            const data = await res.json();
            if (data && data.success === false) {
                showB2BToast("⚠️ " + (data.message || "Stok yetersiz."), "danger");
                if (typeof data.availableStock === 'number') {
                    if (data.availableStock <= 0) {
                        removeFromCart(productId);
                    } else {
                        item.quantity = data.availableStock;
                        saveB2BCart(cart);
                        renderCartDrawer();
                    }
                }
                return;
            }
        } catch (e) {
            console.warn("UpdateQuantity server sync:", e);
        }

        item.quantity = newQty;
        saveB2BCart(cart);
        renderCartDrawer();
    }
}

/**
 * Tüm sepeti boşaltır
 */
function clearB2BCart() {
    saveB2BCart([]);
    renderCartDrawer();
    if (typeof renderFullCartPage === 'function') {
        renderFullCartPage();
    }
    showB2BToast("Sepetiniz boşaltıldı.", "info");

    fetch('/Cart/ClearCart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
    }).catch(e => console.warn("ClearCart server sync:", e));
}

// Sekmeler ve pencereler arası anlık sepet eşitlemesi
window.addEventListener('storage', function (e) {
    if (e.key === 'b2b_portal_cart') {
        updateCartUi();
        if (typeof renderFullCartPage === 'function') {
            renderFullCartPage();
        }
        if (document.getElementById('b2bCartDrawerOverlay') && document.getElementById('b2bCartDrawerOverlay').style.display === 'flex') {
            renderCartDrawer();
        }
    }
});

/**
 * Sepet toplamlarını hesaplar
 */
function getCartTotals() {
    const cart = getB2BCart();
    let subtotal = 0;
    let totalCount = 0;

    cart.forEach(item => {
        subtotal += item.price * item.quantity;
        totalCount += item.quantity;
    });

    const tax = Math.round(subtotal * 0.20 * 100) / 100;
    const grandTotal = subtotal + tax;

    return {
        subtotal,
        tax,
        grandTotal,
        totalCount,
        uniqueItemCount: cart.length
    };
}

/**
 * Navbar ve arayüzdeki sepet rozetlerini günceller
 */
function updateCartUi() {
    const totals = getCartTotals();
    
    // Navbar rozeti
    const navBadge = document.getElementById('navCartBadge');
    if (navBadge) {
        navBadge.textContent = totals.uniqueItemCount;
        navBadge.style.display = totals.uniqueItemCount > 0 ? 'inline-block' : 'none';
    }

    // Vitrin başlık rozeti
    const vitrinBadge = document.getElementById('vitrinCartBadge');
    if (vitrinBadge) {
        vitrinBadge.textContent = totals.uniqueItemCount;
        vitrinBadge.style.display = totals.uniqueItemCount > 0 ? 'inline-block' : 'none';
    }

    // Drawer öğelerini güncelle
    const drawerCount = document.getElementById('cartDrawerItemCountText');
    if (drawerCount) {
        drawerCount.textContent = `${totals.uniqueItemCount} Kalem Ürün (${totals.totalCount} Adet)`;
    }

    // Eğer tam sepet sayfasındaysak (Cart/Index), sayfayı yenilemeden tabloyu canlı güncelle
    if (typeof renderFullCartPage === 'function') {
        renderFullCartPage();
    }
}

/**
 * Sepet çekmecesini açar ve listeyi çizer
 */
function openCartDrawer() {
    const drawerOverlay = document.getElementById('b2bCartDrawerOverlay');
    if (!drawerOverlay) return;

    renderCartDrawer();
    drawerOverlay.style.display = 'flex';
}

/**
 * Sepet çekmecesini kapatır
 */
function closeCartDrawer() {
    const drawerOverlay = document.getElementById('b2bCartDrawerOverlay');
    if (drawerOverlay) {
        drawerOverlay.style.display = 'none';
    }
}

/**
 * Sepet çekmecesindeki HTML içeriğini dinamik olarak üretir
 */
function renderCartDrawer() {
    const listContainer = document.getElementById('cartDrawerItemsList');
    if (!listContainer) return;

    const cart = getB2BCart();
    const totals = getCartTotals();

    // Fiyat özetlerini güncelle
    const subtotalEl = document.getElementById('cartDrawerSubtotal');
    const taxEl = document.getElementById('cartDrawerTax');
    const grandTotalEl = document.getElementById('cartDrawerGrandTotal');
    const footerEl = document.getElementById('cartDrawerFooter');

    if (subtotalEl) subtotalEl.textContent = formatB2BMoney(totals.subtotal);
    if (taxEl) taxEl.textContent = formatB2BMoney(totals.tax);
    if (grandTotalEl) grandTotalEl.textContent = formatB2BMoney(totals.grandTotal);

    if (cart.length === 0) {
        listContainer.innerHTML = `
            <div style="text-align: center; padding: 48px 16px; color: #64748b;">
                <div style="font-size: 3rem; margin-bottom: 12px;">🛒</div>
                <h4 style="font-size: 1.05rem; font-weight: 700; color: #0f172a; margin-bottom: 6px;">Sepetiniz Boş</h4>
                <p style="font-size: 0.82rem; margin-bottom: 16px;">Vitrinden ihtiyacınız olan endüstriyel ürünleri doğrudan sepetinize ekleyebilirsiniz.</p>
                <button type="button" class="btn-primary" onclick="closeCartDrawer()" style="display: inline-flex; margin: 0 auto; padding: 8px 16px; font-size: 0.85rem;">
                    Alışverişe Başla
                </button>
            </div>
        `;
        if (footerEl) footerEl.style.opacity = '0.5';
        return;
    }

    if (footerEl) footerEl.style.opacity = '1';

    let html = '';
    cart.forEach(item => {
        const itemLineTotal = item.price * item.quantity;
        html += `
            <div style="background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; display: flex; gap: 12px; align-items: center; box-shadow: 0 1px 3px rgba(0,0,0,0.05);">
                <img src="${item.image || '/images/default-product.svg'}" alt="${item.title}" style="width: 54px; height: 54px; object-fit: cover; border-radius: 6px; border: 1px solid #e2e8f0;" onerror="this.src='/images/default-product.svg'" />
                
                <div style="flex: 1; min-width: 0;">
                    <div style="font-size: 0.72rem; color: #0284c7; font-weight: 700;">${item.seller || 'Onaylı Tedarikçi'}</div>
                    <div style="font-size: 0.84rem; font-weight: 700; color: #0f172a; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" title="${item.title}">${item.title}</div>
                    <div style="font-size: 0.78rem; color: #64748b; margin-top: 2px;">
                        ${formatB2BMoney(item.price)} <small>+ KDV</small>
                    </div>
                </div>

                <div style="display: flex; flex-direction: column; align-items: flex-end; gap: 6px;">
                    <button type="button" onclick="removeFromCart(${item.productId})" style="background: transparent; border: none; color: #ef4444; font-size: 0.82rem; cursor: pointer; padding: 2px;" title="Sil">
                        🗑️
                    </button>
                    
                    <div style="display: flex; align-items: center; border: 1px solid #cbd5e1; border-radius: 4px; overflow: hidden;">
                        <button type="button" onclick="updateCartQty(${item.productId}, ${item.quantity - 1})" style="background: #f8fafc; border: none; padding: 2px 8px; font-weight: 700; cursor: pointer;">-</button>
                        <span style="padding: 2px 8px; font-size: 0.8rem; font-weight: 700; min-width: 24px; text-align: center;">${item.quantity}</span>
                        <button type="button" onclick="updateCartQty(${item.productId}, ${item.quantity + 1})" style="background: #f8fafc; border: none; padding: 2px 8px; font-weight: 700; cursor: pointer;">+</button>
                    </div>
                </div>
            </div>
        `;
    });

    listContainer.innerHTML = html;
}

// ==================== B2B KURUMSAL CHECKOUT & GİRDİ DOĞRULAMA MOTORU ====================

/**
 * TC Kimlik / VKN Giriş Kısıtlaması (Sadece rakam, en fazla 11 hane)
 */
function formatTaxNumberInput(input) {
    if (!input) return;
    input.value = input.value.replace(/\D/g, '').slice(0, 11);
    const counter = document.getElementById('checkoutTaxCounter');
    if (counter) {
        const len = input.value.length;
        counter.textContent = `${len}/11 Hane`;
        if (len === 10 || len === 11) {
            counter.style.color = '#059669';
            counter.style.fontWeight = '700';
        } else {
            counter.style.color = '#64748b';
            counter.style.fontWeight = '600';
        }
    }
}

/**
 * Telefon Numarası Giriş Kısıtlaması (Sadece rakam, en fazla 10 hane, başında 0 varsa temizlenir)
 */
function formatPhoneInput(input) {
    if (!input) return;
    let val = input.value.replace(/\D/g, '');
    if (val.startsWith('0')) {
        val = val.substring(1);
    }
    input.value = val.slice(0, 10);
    const counter = document.getElementById('checkoutPhoneCounter');
    if (counter) {
        const len = input.value.length;
        counter.textContent = `${len}/10 Hane`;
        if (len === 10) {
            counter.style.color = '#059669';
            counter.style.fontWeight = '700';
        } else {
            counter.style.color = '#64748b';
            counter.style.fontWeight = '600';
        }
    }
}

/**
 * Kart Numarası Formatlama (Sadece rakam, 16 hane, 4x4 boşluklu gruplama)
 */
function formatCardNumberInput(input) {
    if (!input) return;
    let val = input.value.replace(/\D/g, '').slice(0, 16);
    let parts = [];
    for (let i = 0; i < val.length; i += 4) {
        parts.push(val.substring(i, i + 4));
    }
    input.value = parts.join(' ');
    
    // Dinamik Kart Rozeti
    const badge = document.getElementById('cardTypeBadge');
    if (badge) {
        if (val.startsWith('4')) {
            badge.textContent = '💳 VISA';
            badge.style.background = '#1e40af';
        } else if (val.startsWith('51') || val.startsWith('52') || val.startsWith('53') || val.startsWith('54') || val.startsWith('55')) {
            badge.textContent = '💳 MASTERCARD';
            badge.style.background = '#dc2626';
        } else if (val.startsWith('9792')) {
            badge.textContent = '💳 TROY';
            badge.style.background = '#0284c7';
        } else {
            badge.textContent = '💳 KURUMSAL KART';
            badge.style.background = '#0f172a';
        }
    }
}

/**
 * Son Kullanma Tarihi Formatlama (AA/YY)
 */
function formatExpiryInput(input) {
    if (!input) return;
    let val = input.value.replace(/\D/g, '').slice(0, 4);
    if (val.length >= 2) {
        let month = parseInt(val.substring(0, 2), 10);
        if (month > 12) month = 12;
        if (month === 0) month = 1;
        let monthStr = month < 10 ? '0' + month : '' + month;
        input.value = monthStr + '/' + val.substring(2);
    } else {
        input.value = val;
    }
}

/**
 * CVV Formatlama (Sadece rakam, 3 hane)
 */
function formatCvvInput(input) {
    if (!input) return;
    input.value = input.value.replace(/\D/g, '').slice(0, 3);
}

/**
 * Kart Sahibi Adı (Sadece harfler ve boşluk)
 */
function formatCardHolderInput(input) {
    if (!input) return;
    input.value = input.value.replace(/[^a-zA-ZğüşıöçĞÜŞİÖÇ\s]/g, '').toUpperCase();
}

/**
 * Ödeme Yöntemi Kartı Seçimi
 */
function selectPaymentMethod(method, cardEl) {
    document.querySelectorAll('.b2b-paymethod-card').forEach(c => c.classList.remove('is-selected'));
    if (cardEl) {
        cardEl.classList.add('is-selected');
        const radio = cardEl.querySelector('input[type="radio"]');
        if (radio) radio.checked = true;
    }
    togglePaymentFields(method);
}

/**
 * Sipariş Numarasını Panoya Kopyalar
 */
function copyOrderNumber(btn, code) {
    if (!code) {
        const el = document.getElementById('successModalOrderNo');
        code = el ? el.textContent.trim() : '';
    }
    if (!code) return;
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(code).then(() => {
            if (btn) {
                const orig = btn.innerHTML;
                btn.innerHTML = '✓ Kopyalandı';
                setTimeout(() => { btn.innerHTML = orig; }, 2000);
            }
            showB2BToast('✓ Sipariş numarası panoya kopyalandı.', 'success');
        }).catch(() => {
            showB2BToast('Sipariş No: ' + code, 'info');
        });
    } else {
        showB2BToast('Sipariş No: ' + code, 'info');
    }
}

/**
 * Kurumsal Checkout modalını açar ve veritabanındaki profil bilgilerini otomatik doldurur
 */
async function openCheckoutModal() {
    const cart = getB2BCart();
    if (cart.length === 0) {
        showB2BToast("⚠️ Sepetinizde satın alınacak ürün bulunmamaktadır.", "danger");
        openCartDrawer();
        return;
    }

    // Kurumsal üyelik / giriş kontrolü
    let isAuthenticated = false;
    let profileData = null;
    try {
        const res = await fetch('/Order/GetCheckoutProfile');
        profileData = await res.json();
        if (profileData && profileData.isAuthenticated) {
            isAuthenticated = true;
        }
    } catch (e) {
        console.log('Checkout profil getirme hatası:', e);
    }

    if (!isAuthenticated) {
        showB2BToast("🔒 Satın alma işlemi yapabilmek için lütfen kurumsal hesabınızla giriş yapınız veya yeni bir firma hesabı oluşturunuz.", "warning");
        setTimeout(() => {
            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname === '/Cart' ? '/Cart' : window.location.pathname);
        }, 1200);
        return;
    }

    closeCartDrawer();

    const totals = getCartTotals();
    const grandTotalEl = document.getElementById('checkoutModalGrandTotalText');
    const countEl = document.getElementById('checkoutModalItemCount');

    if (grandTotalEl) grandTotalEl.textContent = formatB2BMoney(totals.grandTotal);
    if (countEl) countEl.textContent = `${totals.uniqueItemCount} Kalem (${totals.totalCount} Adet)`;

    // Veritabanındaki kayıtlı kurumsal profil bilgilerini dolduralım
    const compInput = document.getElementById('checkoutBuyerCompany');
    const taxNoInput = document.getElementById('checkoutBuyerTaxNo');
    const taxOfficeInput = document.getElementById('checkoutBuyerTaxOffice');
    const cityInput = document.getElementById('checkoutDeliveryCity');
    const phoneInput = document.getElementById('checkoutReceiverPhone');
    const addrInput = document.getElementById('checkoutDeliveryAddress');

    if (profileData && profileData.isAuthenticated) {
        if (compInput && !compInput.value) compInput.value = profileData.companyName || profileData.companyOfficial || '';
        if (taxNoInput && !taxNoInput.value) {
            taxNoInput.value = (profileData.taxNumber || '').replace(/\D/g, '').slice(0, 11);
            formatTaxNumberInput(taxNoInput);
        }
        if (taxOfficeInput && !taxOfficeInput.value) taxOfficeInput.value = profileData.taxOffice || '';
        if (cityInput && !cityInput.value) cityInput.value = profileData.deliveryCity || '';
        if (phoneInput && !phoneInput.value) {
            let p = (profileData.phone || '').replace(/\D/g, '');
            if (p.startsWith('0')) p = p.substring(1);
            phoneInput.value = p.slice(0, 10);
            formatPhoneInput(phoneInput);
        }
        if (addrInput && !addrInput.value) addrInput.value = profileData.deliveryAddress || '';
    }

    if (taxNoInput) formatTaxNumberInput(taxNoInput);
    if (phoneInput) formatPhoneInput(phoneInput);

    const modal = document.getElementById('b2bCheckoutModal');
    if (modal) {
        modal.style.display = 'flex';
        modal.classList.add('show');
        modal.classList.add('is-open');
    }
}

/**
 * Checkout modalını kapatır
 */
function closeCheckoutModal() {
    const modal = document.getElementById('b2bCheckoutModal');
    if (modal) {
        modal.classList.remove('show');
        modal.classList.remove('is-open');
        modal.style.display = 'none';
    }
}

/**
 * Ödeme yöntemi seçimine göre ilgili alanları gösterir/gizler
 */
function togglePaymentFields(method) {
    const ccFields = document.getElementById('paymentCreditCardFields');
    const bankFields = document.getElementById('paymentBankFields');
    const checkFields = document.getElementById('paymentCheckFields');

    if (ccFields) ccFields.style.display = (method === 'Kredi Kartı') ? 'block' : 'none';
    if (bankFields) bankFields.style.display = (method === 'Havale / EFT') ? 'block' : 'none';
    if (checkFields) checkFields.style.display = (method === 'Cari Hesap / Vadeli' || method === 'DBS') ? 'block' : 'none';
}

/**
 * Kurumsal Siparişi Sunucuya İletir (POST /Order/CreateOrder)
 */
async function submitCorporateOrder(e) {
    if (e) {
        if (typeof e.preventDefault === 'function') e.preventDefault();
        if (typeof e.stopPropagation === 'function') e.stopPropagation();
    }

    const cart = getB2BCart();
    if (cart.length === 0) {
        showB2BToast("⚠️ Sepetiniz boş.", "danger");
        return false;
    }

    const company = (document.getElementById('checkoutBuyerCompany')?.value || '').trim();
    const taxNo = (document.getElementById('checkoutBuyerTaxNo')?.value || '').replace(/\D/g, '');
    const taxOffice = (document.getElementById('checkoutBuyerTaxOffice')?.value || '').trim();
    const city = (document.getElementById('checkoutDeliveryCity')?.value || '').trim();
    const phone = (document.getElementById('checkoutReceiverPhone')?.value || '').replace(/\D/g, '');
    const address = (document.getElementById('checkoutDeliveryAddress')?.value || '').trim();
    const payMethod = document.querySelector('input[name="checkoutPaymentMethod"]:checked')?.value || 'Kredi Kartı';

    // 1. Temel Zorunlu Alan Kontrolleri
    if (!company) {
        showB2BToast("⚠️ Lütfen 'Satınalma Yapan Firma Ünvanı' alanını doldurunuz.", "danger");
        document.getElementById('checkoutBuyerCompany')?.focus();
        return false;
    }

    // 2. TC Kimlik / VKN Kontrolü (10 hane VKN veya 11 hane TC)
    if (taxNo && taxNo.length !== 10 && taxNo.length !== 11) {
        showB2BToast("⚠️ Vergi Kimlik Numarası 10 hane veya TC Kimlik Numarası 11 hane olmalıdır.", "danger");
        document.getElementById('checkoutBuyerTaxNo')?.focus();
        return false;
    }

    // 3. Telefon Kontrolü (Başında 0 olmadan 10 hane)
    if (phone && phone.length !== 10) {
        showB2BToast("⚠️ Telefon numarası başında 0 olmadan 10 haneli olmalıdır (Örn: 5321234567).", "danger");
        document.getElementById('checkoutReceiverPhone')?.focus();
        return false;
    }

    // 4. Teslimat Şehri ve Adresi
    if (!city) {
        showB2BToast("⚠️ Lütfen 'Teslimat İli / İlçe' alanını doldurunuz.", "danger");
        document.getElementById('checkoutDeliveryCity')?.focus();
        return false;
    }

    if (!address) {
        showB2BToast("⚠️ Lütfen 'Fabrika / Depo Açık Adresi' alanını doldurunuz.", "danger");
        document.getElementById('checkoutDeliveryAddress')?.focus();
        return false;
    }

    // 5. Kredi Kartı Seçilmişse Kart Bilgileri Kontrolü
    if (payMethod === 'Kredi Kartı') {
        const cardHolder = (document.getElementById('checkoutCardHolder')?.value || '').trim();
        const cardNumber = (document.getElementById('checkoutCardNumber')?.value || '').replace(/\D/g, '');
        const cardExpiry = (document.getElementById('checkoutCardExpiry')?.value || '').trim();
        const cardCvv = (document.getElementById('checkoutCardCvv')?.value || '').replace(/\D/g, '');

        if (!cardHolder) {
            showB2BToast("⚠️ Lütfen Kart Üzerindeki İsim alanını doldurunuz.", "danger");
            document.getElementById('checkoutCardHolder')?.focus();
            return false;
        }

        if (cardNumber.length !== 16) {
            showB2BToast("⚠️ Lütfen 16 haneli kredi kartı numaranızı eksiksiz giriniz.", "danger");
            document.getElementById('checkoutCardNumber')?.focus();
            return false;
        }

        if (cardExpiry.length < 5 || !cardExpiry.includes('/')) {
            showB2BToast("⚠️ Lütfen Son Kullanma Tarihini (AA/YY) formatında giriniz.", "danger");
            document.getElementById('checkoutCardExpiry')?.focus();
            return false;
        }

        if (cardCvv.length < 3) {
            showB2BToast("⚠️ Lütfen 3 haneli CVV güvenlik kodunu giriniz.", "danger");
            document.getElementById('checkoutCardCvv')?.focus();
            return false;
        }
    }

    const submitBtn = document.getElementById('btnSubmitOrder');
    const originalBtnHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span>⏳ Sipariş Onaylanıyor...</span>';
    }

    try {
        const orderPayload = {
            buyerCompany: company,
            buyerTaxNumber: taxNo,
            buyerTaxOffice: taxOffice,
            deliveryCity: city,
            deliveryAddress: address,
            receiverPhone: phone ? ('0' + phone) : '',
            paymentMethod: payMethod,
            items: cart.map(i => ({
                productID: i.productId,
                title: i.title,
                sku: i.sku,
                category: i.category,
                price: i.price,
                quantity: i.quantity,
                seller: i.seller,
                image: i.image
            }))
        };

        if (payMethod === 'Kredi Kartı') {
            const cardHolder = (document.getElementById('checkoutCardHolder')?.value || '').trim();
            const cardNumber = (document.getElementById('checkoutCardNumber')?.value || '').replace(/\D/g, '');
            const cardExpiry = (document.getElementById('checkoutCardExpiry')?.value || '').trim();
            const cardCvv = (document.getElementById('checkoutCardCvv')?.value || '').replace(/\D/g, '');

            const expiryParts = cardExpiry.split('/');
            const expMonth = expiryParts[0] ? expiryParts[0].trim() : '12';
            const expYear = expiryParts[1] ? expiryParts[1].trim() : '28';

            const payment3DPayload = {
                orderData: orderPayload,
                cardInfo: {
                    cardHolderName: cardHolder,
                    cardNumber: cardNumber,
                    expireMonth: expMonth,
                    expireYear: expYear,
                    cvc: cardCvv
                }
            };

            const response = await fetch('/Payment/Initialize3DCheckout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payment3DPayload)
            });

            const result = await response.json();
            if (result.requireLogin) {
                showB2BToast("🔒 " + (result.message || "Satın alma işlemi için lütfen kurumsal hesabınızla giriş yapınız."), "warning");
                setTimeout(() => {
                    window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname === '/Cart' ? '/Cart' : window.location.pathname);
                }, 1200);
                return;
            }

            if (result.success && result.htmlContent) {
                clearB2BCart();
                closeCheckoutModal();
                openIyzico3DModal(result.htmlContent);
                return;
            } else if (!result.success) {
                showB2BToast("❌ Ödeme Başarısız: " + (result.message || "Banka işlemi onaylamadı."), "danger");
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalBtnHtml;
                }
                return;
            }
        }

        const response = await fetch('/Order/CreateOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(orderPayload)
        });

        const result = await response.json();
        if (result.requireLogin) {
            showB2BToast("🔒 " + (result.message || "Satın alma işlemi için lütfen kurumsal hesabınızla giriş yapınız."), "warning");
            setTimeout(() => {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname === '/Cart' ? '/Cart' : window.location.pathname);
            }, 1200);
            return;
        }

        if (result.success) {
            // Sepeti boşalt
            clearB2BCart();
            closeCheckoutModal();

            // Başarılı modalını aç
            const successOrderNo = document.getElementById('successModalOrderNo');
            const successTotal = document.getElementById('successModalTotal');
            const successPayMethod = document.getElementById('successModalPayMethod');
            const successDate = document.getElementById('successModalDate');

            if (successOrderNo) successOrderNo.textContent = result.orderNumber;
            if (successTotal) successTotal.textContent = formatB2BMoney(result.grandTotal);
            if (successPayMethod) successPayMethod.textContent = payMethod;
            if (successDate) successDate.textContent = new Date().toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' });

            // Satıcı Değerlendirme Butonunu Yapılandır
            const btnSingle = document.getElementById('btnSuccessReviewSingle');
            const btnMultiple = document.getElementById('btnSuccessReviewMultiple');

            window.lastSuccessOrderData = {
                orderId: result.orderId,
                orderNumber: result.orderNumber,
                sellerCount: result.sellerCount || 1,
                singleSellerName: result.singleSellerName || 'Onaylı Tedarikçi',
                singleSellerCompanyId: result.singleSellerCompanyId || 0,
                sellers: result.sellers || []
            };

            if (result.sellerCount === 1) {
                if (btnSingle) {
                    btnSingle.style.display = 'flex';
                    btnSingle.textContent = `★ ${result.singleSellerName || 'Satıcıyı'} Değerlendir`;
                }
                if (btnMultiple) btnMultiple.style.display = 'none';
            } else {
                if (btnSingle) btnSingle.style.display = 'none';
                if (btnMultiple) {
                    btnMultiple.style.display = 'flex';
                    btnMultiple.textContent = `★ ${result.sellerCount || 'Çoklu'} Satıcıyı Değerlendir (Siparişlerim) →`;
                }
            }

            const successModal = document.getElementById('b2bOrderSuccessModal');
            if (successModal) {
                successModal.style.display = 'flex';
                successModal.classList.add('show');
                successModal.classList.add('is-open');
            }

            showB2BToast(result.message || "✓ Kurumsal siparişiniz başarıyla oluşturuldu.", "success");
        } else {
            showB2BToast("⚠️ " + (result.message || "Sipariş oluşturulamadı."), "danger");
        }
    } catch (err) {
        showB2BToast("⚠️ Sunucu bağlantı hatası: " + err.message, "danger");
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnHtml;
        }
    }

    return false;
}

/**
 * Sipariş Başarılı Ekranından 10 Yıldızlı Satıcı Değerlendirme Modalı Yönetimi
 */
let currentSuccessScore = 10;

window.openSuccessSellerReviewModal = function() {
    const data = window.lastSuccessOrderData;
    if (!data || !data.orderId) return;

    const modal = document.getElementById('orderSuccessRatingModal');
    if (!modal) return;

    document.getElementById('successRateModalOrderID').value = data.orderId;
    document.getElementById('successRateModalSupplierCompanyID').value = data.singleSellerCompanyId || '';
    document.getElementById('successRateModalSupplierName').value = data.singleSellerName || '';

    const title = document.getElementById('successRateModalSellerTitle');
    if (title) {
        title.textContent = `★ ${data.singleSellerName || 'Satıcıyı'} Değerlendir`;
    }

    window.pickSuccessScore(10);
    modal.style.setProperty('display', 'flex', 'important');
    modal.style.setProperty('opacity', '1', 'important');
    modal.style.setProperty('pointer-events', 'auto', 'important');
    modal.classList.add('show');
};

window.closeSuccessSellerReviewModal = function() {
    const modal = document.getElementById('orderSuccessRatingModal');
    if (modal) {
        modal.style.setProperty('display', 'none', 'important');
        modal.style.setProperty('opacity', '0', 'important');
        modal.style.setProperty('pointer-events', 'none', 'important');
        modal.classList.remove('show');
    }
};

window.pickSuccessScore = function(val) {
    currentSuccessScore = val;
    const inp = document.getElementById('successRateModalScore');
    if (inp) inp.value = val;
    window.updateSuccessStarsDisplay(val);
};

window.previewSuccessScore = function(val) {
    window.updateSuccessStarsDisplay(val);
};

window.restoreSuccessScore = function() {
    window.updateSuccessStarsDisplay(currentSuccessScore);
};

window.updateSuccessStarsDisplay = function(val) {
    const stars = document.querySelectorAll('#successStarPickerWrap .btn-star-pick-success');
    stars.forEach(btn => {
        const bVal = parseInt(btn.getAttribute('data-val'));
        if (bVal <= val) {
            btn.style.color = '#f59e0b';
            btn.style.opacity = '1';
        } else {
            btn.style.color = '#cbd5e1';
            btn.style.opacity = '0.7';
        }
    });

    const labels = {
        1: "★ 1.0 / 10 - Çok Zayıf",
        2: "★ 2.0 / 10 - Yetersiz",
        3: "★ 3.0 / 10 - Zayıf",
        4: "★ 4.0 / 10 - Geliştirilmeli",
        5: "★ 5.0 / 10 - Orta Seviye",
        6: "★ 6.0 / 10 - Ortalama",
        7: "★ 7.0 / 10 - Başarılı",
        8: "★ 8.0 / 10 - İyi",
        9: "★ 9.0 / 10 - Çok İyi",
        10: "★ 10.0 / 10 - Mükemmel"
    };
    const txt = document.getElementById('successScoreText');
    if (txt) txt.textContent = labels[val] || `★ ${val} / 10`;
};

window.submitSuccessSellerReview = async function(e) {
    e.preventDefault();
    const orderId = parseInt(document.getElementById('successRateModalOrderID').value);
    const sellerCompId = parseInt(document.getElementById('successRateModalSupplierCompanyID').value) || null;
    const sellerName = document.getElementById('successRateModalSupplierName').value || '';
    const score = parseFloat(document.getElementById('successRateModalScore').value);
    const delivery = parseFloat(document.getElementById('successRateDelivery').value);
    const quality = parseFloat(document.getElementById('successRateQuality').value);
    const comm = parseFloat(document.getElementById('successRateComm').value);
    const comment = document.getElementById('successRateComment').value;

    const btn = document.getElementById('btnSubmitSuccessReview');
    if (btn) {
        btn.disabled = true;
        btn.textContent = '⏳ Gönderiliyor...';
    }

    try {
        const res = await fetch('/Review/SubmitBuyerReview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                orderID: orderId,
                targetSupplierCompanyID: sellerCompId,
                targetSupplierCompanyName: sellerName,
                reviewScore: score,
                deliveryScore: delivery,
                qualityScore: quality,
                communicationScore: comm,
                reviewText: comment
            })
        });

        const data = await res.json();
        if (data.success) {
            window.closeSuccessSellerReviewModal();
            showB2BToast("✓ " + (data.message || "Değerlendirmeniz başarıyla kaydedildi."), "success");
            const btnSingle = document.getElementById('btnSuccessReviewSingle');
            if (btnSingle) {
                btnSingle.textContent = '✓ Satıcı Değerlendirildi';
                btnSingle.disabled = true;
                btnSingle.style.opacity = '0.7';
            }
        } else {
            showB2BToast("⚠️ " + (data.message || "Değerlendirme kaydedilemedi."), "danger");
        }
    } catch (err) {
        showB2BToast("⚠️ Bağlantı hatası: " + err.message, "danger");
    } finally {
        if (btn) {
            btn.disabled = false;
            btn.textContent = 'Değerlendir & Gönder';
        }
    }
};

/**
 * SignalR üzerinden anlık gelen yeni ürünü vitrine ekler
 */
window.addRealtimeProductToVitrin = function(item) {
    if (!item || !item.id) return;
    if (activeProducts.some(p => p.id === item.id)) return;

    activeProducts.unshift(item);
    if (typeof B2B_PRODUCT_CATALOG !== 'undefined') {
        B2B_PRODUCT_CATALOG.unshift(item);
    }
    renderCorporateVitrin();
};

/**
 * SignalR üzerinden sepet güncellendiğinde tüm sekmelerde sepeti senkronize eder
 */
window.syncCartFromSignalR = function() {
    initB2BCartSync();
};

// Global ESC tuşu ve overlay tıklama koruması
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape' || e.key === 'Esc') {
        closeTyModal('tyQuickViewModal');
        closeTyModal('tyQuoteModal');
        closeTyModal('tySellerOfferModal');
        closeCartDrawer();
        closeCheckoutModal();
        closeIyzico3DModal();
    }
});

// ==================== İYZİCO 3D SECURE GÜVENLİ DOĞRULAMA (PNA & POP-UP DESTEKLİ) ====================
let currentIyzicoPopup = null;
let cachedIyzicoHtml = null;
let iyzicoListenerAttached = false;

function setupIyzicoMessageListener() {
    if (iyzicoListenerAttached) return;
    iyzicoListenerAttached = true;

    window.addEventListener('message', function(event) {
        if (event && event.data && event.data.type === 'IYZICO_3D_RESULT') {
            closeIyzico3DModal();
            if (currentIyzicoPopup && !currentIyzicoPopup.closed) {
                try { currentIyzicoPopup.close(); } catch(e) {}
            }

            if (event.data.success) {
                try {
                    localStorage.removeItem('b2b_portal_cart');
                    if (typeof updateCartCountBadge === 'function') updateCartCountBadge(0);
                    const navCartBadge = document.getElementById('navCartBadge');
                    if (navCartBadge) navCartBadge.textContent = '0';
                } catch(e) {}

                if (typeof showB2BToast === 'function') {
                    showB2BToast("🎉 Ödeme başarıyla tamamlandı! Siparişiniz havuz korumasında oluşturuldu.", "success");
                }
            } else {
                if (typeof showB2BToast === 'function') {
                    showB2BToast("⚠️ Ödeme doğrulanamadı veya işlem iptal edildi.", "warning");
                }
            }

            setTimeout(() => {
                window.location.href = event.data.targetUrl || '/Dashboard/Orders';
            }, 600);
        }
    });
}

function launchIyzico3DPopup() {
    if (!cachedIyzicoHtml) return;
    const width = 640;
    const height = 740;
    const left = Math.max(0, Math.round((window.screen.width - width) / 2));
    const top = Math.max(0, Math.round((window.screen.height - height) / 2));

    currentIyzicoPopup = window.open('', 'iyzico_3d_secure_window', `width=${width},height=${height},top=${top},left=${left},scrollbars=yes,resizable=yes,status=no,toolbar=no,menubar=no`);
    if (currentIyzicoPopup) {
        try {
            currentIyzicoPopup.document.open();
            currentIyzicoPopup.document.write(cachedIyzicoHtml);
            currentIyzicoPopup.document.close();
            currentIyzicoPopup.focus();
        } catch(e) {
            console.warn("Pop-up belgesi yazılamadı:", e);
        }
    }
}

function loadIyzicoInIframeDirectly() {
    const container = document.getElementById('iyzico3DFrameContainer');
    if (!container || !cachedIyzicoHtml) return;

    container.innerHTML = '';
    const iframe = document.createElement('iframe');
    iframe.style.width = '100%';
    iframe.style.height = '540px';
    iframe.style.border = 'none';
    container.appendChild(iframe);

    const doc = iframe.contentDocument || iframe.contentWindow.document;
    doc.open();
    doc.write(cachedIyzicoHtml);
    doc.close();
}

/**
 * iyzico 3D Secure Doğrulama Modalını Açar ve Formu Yükler
 * Tarayıcı Private Network Access (PNA) kısıtlamasını aşmak için bağımsız güvenli pencere kullanır.
 */
function openIyzico3DModal(htmlContent) {
    cachedIyzicoHtml = htmlContent;
    setupIyzicoMessageListener();

    const modal = document.getElementById('iyzico3DModal');
    const container = document.getElementById('iyzico3DFrameContainer');

    // 1. Önce doğrudan bağımsız pop-up pencere açmayı dene (PNA engeline asla takılmaz)
    const width = 640;
    const height = 740;
    const left = Math.max(0, Math.round((window.screen.width - width) / 2));
    const top = Math.max(0, Math.round((window.screen.height - height) / 2));

    let popupOpened = false;
    try {
        currentIyzicoPopup = window.open('', 'iyzico_3d_secure_window', `width=${width},height=${height},top=${top},left=${left},scrollbars=yes,resizable=yes,status=no,toolbar=no,menubar=no`);
        if (currentIyzicoPopup && !currentIyzicoPopup.closed && typeof currentIyzicoPopup.closed !== 'undefined') {
            currentIyzicoPopup.document.open();
            currentIyzicoPopup.document.write(htmlContent);
            currentIyzicoPopup.document.close();
            currentIyzicoPopup.focus();
            popupOpened = true;
        }
    } catch(e) {
        popupOpened = false;
    }

    if (!modal || !container) {
        if (!popupOpened) {
            // Modal yoksa ve pop-up engellendiyse iframe fallback
            const iframe = document.createElement('iframe');
            iframe.style.position = 'fixed';
            iframe.style.inset = '0';
            iframe.style.width = '100%';
            iframe.style.height = '100%';
            iframe.style.zIndex = '9999999';
            iframe.style.border = 'none';
            document.body.appendChild(iframe);
            const doc = iframe.contentDocument || iframe.contentWindow.document;
            doc.open();
            doc.write(htmlContent);
            doc.close();
        }
        return;
    }

    container.innerHTML = `
        <div style="padding: 40px 24px; text-align: center; display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 480px;">
            <div style="width: 72px; height: 72px; border-radius: 50%; background: #eff6ff; display: flex; align-items: center; justify-content: center; font-size: 34px; margin-bottom: 20px; border: 2px solid #bfdbfe; box-shadow: 0 4px 14px rgba(37,99,235,0.18);">
                🛡️
            </div>
            <h3 style="font-size: 1.25rem; font-weight: 800; color: #0f172a; margin-bottom: 8px;">
                Banka 3D Secure Doğrulama Penceresi
            </h3>
            <p style="font-size: 0.92rem; color: #475569; max-width: 440px; line-height: 1.6; margin-bottom: 24px;">
                ${popupOpened 
                    ? 'Bankanızın güvenli SMS onay penceresi açıldı. Lütfen telefonunuza gelen şifreyi açılan pencerede onaylayınız.' 
                    : 'Tarayıcınız otomatik pencereyi engellemiş olabilir. Lütfen aşağıdaki butona tıklayarak doğrulama penceresini açınız.'}
            </p>

            <div style="display: inline-flex; align-items: center; gap: 10px; background: #f1f5f9; padding: 10px 22px; border-radius: 30px; border: 1px solid #cbd5e1; margin-bottom: 28px;">
                <div style="width: 14px; height: 14px; border: 2.5px solid #2563eb; border-top-color: transparent; border-radius: 50%; animation: spin3D 1s linear infinite;"></div>
                <span style="font-size: 0.86rem; font-weight: 700; color: #0f172a;">Banka onayı bekleniyor...</span>
            </div>

            <div style="display: flex; flex-direction: column; gap: 12px; width: 100%; max-width: 320px;">
                <button type="button" onclick="launchIyzico3DPopup()" class="btn-primary" style="padding: 13px 24px; font-weight: 750; font-size: 0.95rem; background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%); border: none; border-radius: 10px; box-shadow: 0 4px 14px rgba(37,99,235,0.35); cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 8px;">
                    <span>🔗</span> ${popupOpened ? 'Pencereyi Tekrar Aç / Öne Getir' : 'Banka Onay Ekranını Aç'}
                </button>
                <button type="button" onclick="loadIyzicoInIframeDirectly()" style="background: transparent; border: 1px dashed #cbd5e1; color: #64748b; padding: 8px 16px; border-radius: 8px; font-size: 0.8rem; cursor: pointer; font-weight: 600;">
                    Modal içinde açmayı dene (Yerel Iframe)
                </button>
            </div>
        </div>
        <style>
            @@keyframes spin3D { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
        </style>
    `;

    modal.style.display = 'flex';
    modal.classList.add('show');
    modal.classList.add('is-open');
}

function closeIyzico3DModal() {
    const modal = document.getElementById('iyzico3DModal');
    if (modal) {
        modal.style.display = 'none';
        modal.classList.remove('show');
        modal.classList.remove('is-open');
    }
}



