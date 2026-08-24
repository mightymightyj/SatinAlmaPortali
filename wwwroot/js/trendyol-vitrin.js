/**
 * B2B Kurumsal Tedarik Portalı - 'İhtiyacınız Olabilecek Ürünler' Vitrini
 * 6 Sütun x 8 Satır (48 Ürün) Profesyonel Kurumsal Ürün & Tedarikçi Motoru
 */

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
    let pool = [...B2B_PRODUCT_CATALOG];

    // Havuz 48 adede ulaşana kadar gerçekçi kurumsal varyasyonlar türet
    while (pool.length < 48) {
        const base = B2B_PRODUCT_CATALOG[Math.floor(Math.random() * B2B_PRODUCT_CATALOG.length)];
        const supplier = CORPORATE_VERIFIED_SUPPLIERS[Math.floor(Math.random() * CORPORATE_VERIFIED_SUPPLIERS.length)];
        const variationPrice = Math.floor((Math.random() * 0.16 - 0.08) * base.price);
        const netPrice = Math.max(150, base.price + variationPrice);
        const oldPrice = Math.round(netPrice * (1 + (Math.floor(Math.random() * 12 + 10) / 100)));
        const discRate = Math.round(((oldPrice - netPrice) / oldPrice) * 100);

        const newItem = {
            ...base,
            id: 2000 + pool.length,
            sku: base.sku + "-S" + (pool.length % 5 + 1),
            title: base.title + (pool.length % 2 === 0 ? " [Toptan Sanayi Paketi]" : " [Kurumsal Proje Serisi]"),
            seller: supplier.name,
            sellerScore: supplier.score,
            sellerBadge: supplier.badge,
            sellerLocation: supplier.location,
            price: netPrice,
            oldPrice: oldPrice,
            discount: discRate,
            rating: (4.6 + Math.random() * 0.4).toFixed(1),
            reviewsCount: Math.floor(Math.random() * 600 + 45),
            stockStatus: (Math.floor(Math.random() * 150 + 20)) + " " + base.unit + " (Merkez Stok)"
        };
        pool.push(newItem);
    }

    // Fisher-Yates Karıştırma (Her girişte farklı ürünler gelsin)
    for (let i = pool.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [pool[i], pool[j]] = [pool[j], pool[i]];
    }

    activeProducts = pool.slice(0, 48);
    return activeProducts;
}

/**
 * Para Birimi Formatlayıcı (Türk Lirası)
 */
function formatB2BMoney(amount) {
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 }).format(amount);
}

/**
 * Sayfa Yüklendiğinde Vitrini Başlat
 */
document.addEventListener('DOMContentLoaded', function () {
    // Kurumsal Sepet Durumunu Her Sayfada Güncelle
    updateCartUi();

    const container = document.getElementById('trendyolProductGrid');
    if (!container) return;

    generateCorporate48Products();
    renderCorporateVitrin();
    setupCorporateEvents();
    loadDbVitrinProducts();
});

/**
 * Veritabanında (items tablosunda) kayıtlı gerçek ürünleri vitrine dahil et
 */
async function loadDbVitrinProducts() {
    try {
        const response = await fetch('/Home/GetVitrinProductsJson');
        const data = await response.json();
        if (data.success && Array.isArray(data.products) && data.products.length > 0) {
            data.products.forEach(p => {
                if (!activeProducts.some(ap => ap.id === p.id)) {
                    activeProducts.unshift(p);
                }
                if (!B2B_PRODUCT_CATALOG.some(cp => cp.id === p.id)) {
                    B2B_PRODUCT_CATALOG.unshift(p);
                }
            });
            renderCorporateVitrin();
        }
    } catch (err) {
        console.warn('DB vitrin ürünleri yüklenirken hata:', err);
    }
}

/**
 * 6x8 B2B Ürün Izgarasını Render Et
 */
function renderCorporateVitrin() {
    const grid = document.getElementById('trendyolProductGrid');
    const badge = document.getElementById('vitrinProductCount');
    if (!grid) return;

    // Filtreleme
    let filtered = activeProducts.filter(item => {
        const matchesCategory = (currentFilter === 'all') || (item.category === currentFilter) || (currentFilter === 'flash' && item.discount >= 18);
        const matchesSearch = !currentSearchQuery || 
            item.title.toLowerCase().includes(currentSearchQuery.toLowerCase()) ||
            item.sku.toLowerCase().includes(currentSearchQuery.toLowerCase()) ||
            item.brand.toLowerCase().includes(currentSearchQuery.toLowerCase()) ||
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
        badge.textContent = `${filtered.length} Kurumsal Ürün Listeleniyor (6x8 Matris)`;
    }

    if (filtered.length === 0) {
        grid.innerHTML = `
            <div class="b2b-empty-state">
                <div class="empty-icon">🏢</div>
                <h3>Aradığınız kriterlere uygun kurumsal ürün bulunamadı</h3>
                <p>Farklı bir anahtar kelime, SKU kodu veya kategori seçebilirsiniz.</p>
                <button type="button" class="btn-b2b-refresh" onclick="resetB2BFilters()">
                    Tüm Ürünleri Listele
                </button>
            </div>
        `;
        return;
    }

    let html = '';
    filtered.forEach((item) => {
        const isFav = favoriteIds.includes(item.id);

        html += `
            <article class="b2b-product-card" data-id="${item.id}" data-category="${item.category}">
                <!-- Kart Üst Barı: Tedarikçi & Lokasyon -->
                <div class="b2b-card-supplier-bar" title="Tedarikçi: ${item.seller} (${item.sellerLocation})">
                    <span class="b2b-verified-badge" title="Doğrulanmış Kurumsal Tedarikçi">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="#059669" stroke="currentColor" stroke-width="1.5">
                            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                            <polyline points="22 4 12 14.01 9 11.01" stroke="#fff" stroke-width="2.5"></polyline>
                        </svg>
                        <span class="supplier-name-text">${item.seller}</span>
                    </span>
                    <span class="b2b-location-pill">${item.sellerLocation ? item.sellerLocation.split('(')[0] : 'Türkiye'}</span>
                </div>

                <!-- Görsel ve Rozet Katmanı -->
                <div class="b2b-img-container">
                    <img src="${item.image}" alt="${item.title}" loading="lazy" class="b2b-img" onerror="this.src='https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=500&auto=format&fit=crop&q=80'" />
                    
                    <div class="b2b-badges-stack">
                        ${item.badge ? `<span class="b2b-badge badge-corp">${item.badge}</span>` : ''}
                        ${item.discount >= 18 ? `<span class="b2b-badge badge-disc">%${item.discount} Kurumsal İndirim</span>` : ''}
                    </div>

                    <!-- Favori / Takip Butonu -->
                    <button type="button" class="b2b-fav-btn ${isFav ? 'active' : ''}" onclick="toggleB2BFavorite(event, ${item.id})" title="${isFav ? 'Takip Listesinden Çıkar' : 'Takip Listesine Ekle'}">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="${isFav ? '#e11d48' : 'none'}" stroke="${isFav ? '#e11d48' : '#64748b'}" stroke-width="2">
                            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                        </svg>
                    </button>

                    <!-- Hızlı İncele Butonu -->
                    <button type="button" class="b2b-quick-look-btn" onclick="openProductQuickView(${item.id})">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                            <circle cx="12" cy="12" r="3"></circle>
                        </svg>
                        Şartname & Detay
                    </button>
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
                        ${item.title}
                    </h3>

                    <!-- Toptan Fiyat Kademesi & MOQ -->
                    <div class="b2b-spec-bar">
                        <span class="b2b-moq-chip">MOQ: <strong>${item.moq}</strong></span>
                        <span class="b2b-stock-chip">📦 ${item.stockStatus}</span>
                    </div>

                    <!-- Fiyat Bloğu -->
                    <div class="b2b-pricing-box">
                        <div class="b2b-old-price-line">
                            <span class="b2b-crossed">${formatB2BMoney(item.oldPrice)}</span>
                            <span class="b2b-save-tag">%${item.discount} Tasarruf</span>
                        </div>
                        <div class="b2b-current-price-line">
                            <span class="b2b-price-val">${formatB2BMoney(item.price)}</span>
                            <span class="b2b-tax-text">${item.taxText || '+ KDV'}</span>
                        </div>
                        <div class="b2b-tier-subtext" title="Toptan alım kademe indirimi">
                            ${item.tierPricing || 'Toplu siparişlerde özel proje iskontosu'}
                        </div>
                    </div>

                    <!-- Aksiyon Butonları (Satın Al & Sepete Ekle) -->
                    <div class="b2b-actions">
                        <button type="button" class="b2b-btn-buy" onclick="directBuy(${item.id})" title="Bu ürünü doğrudan kurumsal siparişle satın alın">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                                <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
                            </svg>
                            Satın Al
                        </button>
                        <button type="button" class="b2b-btn-cart" onclick="addToCart(${item.id})" title="Sepete Ekle">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="9" cy="21" r="1"></circle>
                                <circle cx="20" cy="21" r="1"></circle>
                                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                            </svg>
                        </button>
                        <button type="button" class="b2b-btn-contact" onclick="openProductQuickView(${item.id})" title="Şartname ve Tedarikçi Detaylarını Gör">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="12" cy="12" r="10"></circle>
                                <line x1="12" y1="16" x2="12" y2="12"></line>
                                <line x1="12" y1="8" x2="12.01" y2="8"></line>
                            </svg>
                        </button>
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
    // Filtre Sekmeleri
    const tabs = document.querySelectorAll('.b2b-tab-chip');
    tabs.forEach(tab => {
        tab.addEventListener('click', function () {
            tabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            currentFilter = this.getAttribute('data-filter') || 'all';
            renderCorporateVitrin();
        });
    });

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
    currentFilter = 'all';
    currentSearchQuery = '';
    currentSort = 'recommended';

    const searchInput = document.getElementById('vitrinSearchInput');
    if (searchInput) searchInput.value = '';

    const sortSelect = document.getElementById('vitrinSortSelect');
    if (sortSelect) sortSelect.value = 'recommended';

    const tabs = document.querySelectorAll('.b2b-tab-chip');
    tabs.forEach(t => {
        if (t.getAttribute('data-filter') === 'all') t.classList.add('active');
        else t.classList.remove('active');
    });

    renderCorporateVitrin();
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
 * Hızlı Ürün & Şartname İnceleme Modalı
 */
function openProductQuickView(productId) {
    const product = activeProducts.find(p => p.id === productId);
    if (!product) return;

    const modal = document.getElementById('tyQuickViewModal');
    if (!modal) return;

    document.getElementById('qvModalImage').src = product.image;
    document.getElementById('qvModalTitle').textContent = product.title;
    document.getElementById('qvModalBrand').textContent = product.brand;
    document.getElementById('qvModalSku').textContent = product.sku;
    document.getElementById('qvModalSeller').textContent = product.seller;
    document.getElementById('qvModalLocation').textContent = product.sellerLocation || 'Türkiye';
    document.getElementById('qvModalScore').textContent = `★ ${product.sellerScore} / 10`;
    document.getElementById('qvModalPrice').textContent = formatB2BMoney(product.price);
    document.getElementById('qvModalOldPrice').textContent = formatB2BMoney(product.oldPrice);
    document.getElementById('qvModalDiscount').textContent = `%${product.discount} İskonto`;
    document.getElementById('qvModalCategory').textContent = product.categoryName;
    document.getElementById('qvModalStock').textContent = product.stockStatus;
    document.getElementById('qvModalMoq').textContent = `Min. Sipariş (MOQ): ${product.moq}`;
    document.getElementById('qvModalLeadTime').textContent = `Termin: ${product.leadTime || '1-2 Gün'}`;
    document.getElementById('qvModalTierPricing').textContent = product.tierPricing || 'Adet arttıkça özel iskonto uygulanır.';

    const modalQuoteBtn = document.getElementById('qvModalQuoteBtn');
    if (modalQuoteBtn) {
        modalQuoteBtn.innerHTML = `
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
            </svg>
            Hemen Satın Al
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

    modal.classList.add('show');
}

/**
 * Kurumsal Teklif (RFQ) İsteme Modalı
 */
function openQuoteModal(productId) {
    const product = activeProducts.find(p => p.id === productId);
    if (!product) return;

    const modal = document.getElementById('tyQuoteModal');
    if (!modal) return;

    document.getElementById('quoteModalProductTitle').textContent = product.title;
    document.getElementById('quoteModalProductSku').textContent = `Stok Kodu (SKU): ${product.sku} | Marka: ${product.brand}`;
    document.getElementById('quoteModalSellerName').textContent = product.seller;
    document.getElementById('quoteModalUnitPrice').textContent = `${formatB2BMoney(product.price)} ${product.taxText || '+ KDV'}`;
    document.getElementById('quoteModalHiddenProductId').value = product.id;

    modal.classList.add('show');
}

/**
 * Tedarikçi Olarak Ürün Satışı Başlatma Modalı
 */
function openSellerOfferModal(productId) {
    const product = productId ? activeProducts.find(p => p.id === productId) : null;
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
        if (stockInput) stockInput.value = '';
        clearCatTreeSelection();
    }

    modal.classList.add('show');
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
        displayText.innerHTML = `<strong>${catName}</strong> <span style="font-size:0.74rem; opacity:0.85; margin-left:4px;">(${breadcrumbPath || catName})</span>`;
        if (displayIcon) displayIcon.textContent = '✅';
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
        displayText.textContent = 'Lütfen aşağıdaki ağaçtan bir kategori seçiniz...';
        if (displayIcon) displayIcon.textContent = '📁';
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
            showB2BToast(result.message, "success");

            // Yeni ürünü anında vitrinin en başına ekle ve yeniden render et
            if (result.item) {
                activeProducts.unshift(result.item);
                B2B_PRODUCT_CATALOG.unshift(result.item);
                renderCorporateVitrin();
            }

            // Formu sıfırla
            e.target.reset();
            clearCatTreeSelection();
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

    const toast = document.createElement('div');
    toast.className = `ty-toast ty-toast-${type}`;
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
 * Sepete ürün ekler
 */
function addToCart(productId, qty = 1, showDrawer = false) {
    const product = activeProducts.find(p => p.id === productId) || B2B_PRODUCT_CATALOG.find(p => p.id === productId);
    if (!product) {
        showB2BToast("⚠️ Ürün bilgisi bulunamadı.", "danger");
        return;
    }

    let cart = getB2BCart();
    const existingIndex = cart.findIndex(item => item.productId === productId);

    if (existingIndex > -1) {
        cart[existingIndex].quantity += qty;
    } else {
        cart.push({
            productId: product.id,
            title: product.title,
            sku: product.sku,
            category: product.categoryName || product.category,
            price: product.price,
            quantity: qty,
            seller: product.seller,
            image: product.image
        });
    }

    saveB2BCart(cart);
    showB2BToast(`🛒 "${product.title}" sepete eklendi!`, "success");

    if (showDrawer) {
        openCartDrawer();
    }
}

/**
 * Doğrudan Satın Al (Hemen Satın Al): Ürünü sepete ekleyip direkt Checkout modalını açar
 */
function directBuy(productId) {
    addToCart(productId, 1, false);
    openCheckoutModal();
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
}

/**
 * Sepetteki ürünün miktarını günceller
 */
function updateCartQty(productId, newQty) {
    let cart = getB2BCart();
    const item = cart.find(i => i.productId === productId);
    if (item) {
        if (newQty <= 0) {
            removeFromCart(productId);
            return;
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
    showB2BToast("Sepetiniz boşaltıldı.", "info");
}

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
                <img src="${item.image}" alt="${item.title}" style="width: 54px; height: 54px; object-fit: cover; border-radius: 6px; border: 1px solid #e2e8f0;" onerror="this.src='https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=100&auto=format&fit=crop&q=80'" />
                
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

/**
 * Kurumsal Checkout modalını açar
 */
function openCheckoutModal() {
    const cart = getB2BCart();
    if (cart.length === 0) {
        showB2BToast("⚠️ Sepetinizde satın alınacak ürün bulunmamaktadır.", "danger");
        openCartDrawer();
        return;
    }

    closeCartDrawer();

    const totals = getCartTotals();
    const grandTotalEl = document.getElementById('checkoutModalGrandTotalText');
    const countEl = document.getElementById('checkoutModalItemCount');

    if (grandTotalEl) grandTotalEl.textContent = formatB2BMoney(totals.grandTotal);
    if (countEl) countEl.textContent = `${totals.uniqueItemCount} Kalem (${totals.totalCount} Adet)`;

    const modal = document.getElementById('b2bCheckoutModal');
    if (modal) {
        modal.style.display = 'flex';
    }
}

/**
 * Checkout modalını kapatır
 */
function closeCheckoutModal() {
    const modal = document.getElementById('b2bCheckoutModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

/**
 * Ödeme yöntemi seçimine göre ilgili alanları gösterir/gizler
 */
function togglePaymentFields(method) {
    const ccFields = document.getElementById('paymentCreditCardFields');
    const bankFields = document.getElementById('paymentBankFields');

    if (method === 'Kredi Kartı') {
        if (ccFields) ccFields.style.display = 'block';
        if (bankFields) bankFields.style.display = 'none';
    } else {
        if (ccFields) ccFields.style.display = 'none';
        if (bankFields) bankFields.style.display = 'block';
    }
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

    const company = document.getElementById('checkoutBuyerCompany')?.value || '';
    const taxNo = document.getElementById('checkoutBuyerTaxNo')?.value || '';
    const taxOffice = document.getElementById('checkoutBuyerTaxOffice')?.value || '';
    const city = document.getElementById('checkoutDeliveryCity')?.value || '';
    const phone = document.getElementById('checkoutReceiverPhone')?.value || '';
    const address = document.getElementById('checkoutDeliveryAddress')?.value || '';
    const payMethod = document.querySelector('input[name="checkoutPaymentMethod"]:checked')?.value || 'Kredi Kartı';

    if (!company.trim() || !city.trim() || !address.trim()) {
        showB2BToast("⚠️ Lütfen zorunlu alanları (Firma Ünvanı, Şehir ve Açık Adres) doldurunuz.", "danger");
        return false;
    }

    const submitBtn = document.getElementById('btnSubmitOrder');
    const originalBtnHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span>⏳ Sipariş Onaylanıyor...</span>';
    }

    try {
        const orderPayload = {
            buyerCompany: company.trim(),
            buyerTaxNumber: taxNo.trim(),
            buyerTaxOffice: taxOffice.trim(),
            deliveryCity: city.trim(),
            deliveryAddress: address.trim(),
            receiverPhone: phone.trim(),
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

        const response = await fetch('/Order/CreateOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(orderPayload)
        });

        const result = await response.json();
        if (result.success) {
            // Sepeti boşalt
            clearB2BCart();
            closeCheckoutModal();

            // Başarılı modalını aç
            const successOrderNo = document.getElementById('successModalOrderNo');
            const successTotal = document.getElementById('successModalTotal');
            if (successOrderNo) successOrderNo.textContent = result.orderNumber;
            if (successTotal) successTotal.textContent = formatB2BMoney(result.grandTotal);

            const successModal = document.getElementById('b2bOrderSuccessModal');
            if (successModal) {
                successModal.style.display = 'flex';
            }

            showB2BToast(result.message, "success");
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

