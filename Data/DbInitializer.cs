using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Helpers;
using satinalmaportali2.Models;

namespace satinalmaportali2.Data
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(ApplicationDbContext context)
        {
            try
            {
                // Ensure Database and Schema exist
                await context.Database.EnsureCreatedAsync();

                // Create orders and orderitems tables if not exist
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS orders (
                            orderid SERIAL PRIMARY KEY,
                            ordernumber VARCHAR(50) NOT NULL,
                            buyercompanyid INT,
                            buyercompanyname VARCHAR(255) NOT NULL,
                            buyertaxnumber VARCHAR(50),
                            buyertaxoffice VARCHAR(100),
                            buyeremail VARCHAR(255),
                            buyerphone VARCHAR(50),
                            suppliercompanyid INT,
                            suppliercompanyname VARCHAR(255),
                            orderdate TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            deliverydate TIMESTAMP WITH TIME ZONE,
                            totalamount DECIMAL(18,2) NOT NULL,
                            taxamount DECIMAL(18,2) NOT NULL,
                            grandtotal DECIMAL(18,2) NOT NULL,
                            paymentmethod VARCHAR(50) NOT NULL,
                            paymentstatus VARCHAR(50) NOT NULL,
                            orderstatus VARCHAR(50) NOT NULL,
                            deliverycity VARCHAR(100),
                            deliveryaddress TEXT,
                            receivername VARCHAR(100),
                            receiverphone VARCHAR(50),
                            trackingnumber VARCHAR(100),
                            shippingcarrier VARCHAR(100),
                            notes TEXT
                        );

                        CREATE TABLE IF NOT EXISTS orderitems (
                            orderitemid SERIAL PRIMARY KEY,
                            orderid INT NOT NULL,
                            itemid INT,
                            productname VARCHAR(255) NOT NULL,
                            sku VARCHAR(100),
                            categoryname VARCHAR(100),
                            unitprice DECIMAL(18,2) NOT NULL,
                            quantity INT NOT NULL,
                            linetotal DECIMAL(18,2) NOT NULL,
                            suppliercompanyname VARCHAR(255),
                            suppliercompanyid INT
                        );

                        CREATE TABLE IF NOT EXISTS cartitems (
                            cartitemid SERIAL PRIMARY KEY,
                            companyid INT NOT NULL,
                            productid INT NOT NULL,
                            title VARCHAR(255) NOT NULL,
                            sku VARCHAR(100),
                            category VARCHAR(100),
                            price DECIMAL(18,2) NOT NULL,
                            quantity INT NOT NULL DEFAULT 1,
                            seller VARCHAR(255),
                            image TEXT,
                            createdat TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            updatedat TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );

                        CREATE TABLE IF NOT EXISTS reviews (
                            reviewid SERIAL PRIMARY KEY,
                            requestid INT,
                            orderid INT,
                            orderitemid INT,
                            companyid INT,
                            reviewercompanyid INT,
                            reviewername VARCHAR(255),
                            reviewtype VARCHAR(50) DEFAULT 'BuyerToSeller',
                            reviewscore DOUBLE PRECISION,
                            deliveryscore DOUBLE PRECISION,
                            qualityscore DOUBLE PRECISION,
                            communicationscore DOUBLE PRECISION,
                            reviewtext TEXT,
                            reviewdate TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            isverifiedpurchase BOOLEAN DEFAULT TRUE
                        );

                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS orderid INT;
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS orderitemid INT;
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS reviewercompanyid INT;
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS reviewername VARCHAR(255);
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS reviewtype VARCHAR(50) DEFAULT 'BuyerToSeller';
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS deliveryscore DOUBLE PRECISION;
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS qualityscore DOUBLE PRECISION;
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS communicationscore DOUBLE PRECISION;
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS reviewdate TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;
                        ALTER TABLE reviews ADD COLUMN IF NOT EXISTS isverifiedpurchase BOOLEAN DEFAULT TRUE;
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Orders/CartItems/Reviews table check/create note: " + ex.Message);
                }

                // ====================================================
                // STEP 0: Seed CompanyTypes (FK dependency for Companies)
                // ====================================================
                try
                {
                    if (!await context.CompanyTypes.AnyAsync(ct => ct.CompanyTypeID == 1))
                    {
                        var types = new List<CompanyTypes>
                        {
                            new CompanyTypes { CompanyTypeID = 1, CompanyTypeName = "Tedarikçi Firma" },
                            new CompanyTypes { CompanyTypeID = 2, CompanyTypeName = "Alıcı / Müşteri" },
                            new CompanyTypes { CompanyTypeID = 3, CompanyTypeName = "Hem Alıcı Hem Tedarikçi" },
                            new CompanyTypes { CompanyTypeID = 4, CompanyTypeName = "Süper Admin" }
                        };

                        foreach (var type in types)
                        {
                            if (!await context.CompanyTypes.AnyAsync(ct => ct.CompanyTypeID == type.CompanyTypeID))
                            {
                                await context.CompanyTypes.AddAsync(type);
                            }
                        }
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CompanyTypes seed note: " + ex.Message);
                }

                // ====================================================
                // STEP 1: Seed MembershipTypes
                // ====================================================
                try
                {
                    if (!await context.MembershipTypes.AnyAsync(mt => mt.MembershipTypeID == 1))
                    {
                        var mTypes = new List<MembershipTypes>
                        {
                            new MembershipTypes { MembershipTypeID = 1, MembershipTypeName = "Standart Üyelik" },
                            new MembershipTypes { MembershipTypeID = 2, MembershipTypeName = "Premium Üyelik" }
                        };

                        foreach (var mt in mTypes)
                        {
                            if (!await context.MembershipTypes.AnyAsync(m => m.MembershipTypeID == mt.MembershipTypeID))
                            {
                                await context.MembershipTypes.AddAsync(mt);
                            }
                        }
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("MembershipTypes seed note: " + ex.Message);
                }

                // ====================================================
                // STEP 2: Seed Countries (Türkiye)
                // ====================================================
                try
                {
                    if (!await context.Countries.AnyAsync())
                    {
                        await context.Countries.AddAsync(new Countries { CountryName = "Türkiye" });
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Countries seed note: " + ex.Message);
                }

                // Get CountryID for Türkiye (default to 1)
                var turkeyCountry = await context.Countries.FirstOrDefaultAsync(c => c.CountryName == "Türkiye");
                int turkeyId = turkeyCountry?.CountryID ?? 1;

                // ====================================================
                // STEP 3: Seed Towns (81 İl) & District (Tüm İlçeler)
                // ====================================================
                await SeedTownsAndDistrictsAsync(context, turkeyId);

                // ====================================================
                // STEP 4: Fix NULL DistrictID in Addresses table
                // ====================================================
                await FixAddressDistrictIdsAsync(context);

                // ====================================================
                // STEP 5: Seed 10 Supplier Test Accounts (Password: Test1234*_)
                // ====================================================
                await SeedSupplierTestAccountsAsync(context);

                // ====================================================
                // STEP 6: Seed Sample 10-Star Verified Reviews (Buyer <-> Seller)
                // ====================================================
                await SeedSampleVerifiedReviewsAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB SEED ERROR: {ex.Message}");
            }
        }

        private static async Task SeedTownsAndDistrictsAsync(ApplicationDbContext context, int turkeyId)
        {
            try
            {
                var cityDistrictsData = GetCityDistrictsData();

                // Check if Towns table is empty
                bool hasTowns = await context.Towns.AnyAsync();
                if (!hasTowns)
                {
                    var newTowns = cityDistrictsData.Keys.Select(cityName => new Towns
                    {
                        TownName = cityName,
                        CountryID = turkeyId
                    }).ToList();

                    await context.Towns.AddRangeAsync(newTowns);
                    await context.SaveChangesAsync();
                }

                // Get mapped Towns from DB to pair TownID with TownName
                var dbTowns = await context.Towns.Where(t => !string.IsNullOrEmpty(t.TownName)).ToListAsync();
                var townMap = dbTowns.ToDictionary(t => t.TownName!, t => t.TownID, StringComparer.OrdinalIgnoreCase);

                // Check if District table is empty
                bool hasDistricts = await context.Districts.AnyAsync();
                if (!hasDistricts)
                {
                    var newDistricts = new List<Districts>();

                    foreach (var kvp in cityDistrictsData)
                    {
                        string cityName = kvp.Key;
                        if (!townMap.TryGetValue(cityName, out int townId)) continue;

                        foreach (var distName in kvp.Value)
                        {
                            newDistricts.Add(new Districts
                            {
                                DistrictName = distName,
                                TownID = townId
                            });
                        }
                    }

                    if (newDistricts.Any())
                    {
                        await context.Districts.AddRangeAsync(newDistricts);
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SeedTownsAndDistricts error: {ex.Message}");
            }
        }

        private static async Task FixAddressDistrictIdsAsync(ApplicationDbContext context)
        {
            try
            {
                var nullDistrictAddresses = await context.Addresses.Where(a => a.DistrictID == null).ToListAsync();
                if (nullDistrictAddresses.Any())
                {
                    var allDistricts = await context.Districts.ToListAsync();

                    foreach (var addr in nullDistrictAddresses)
                    {
                        if (string.IsNullOrEmpty(addr.AddressText)) continue;

                        // Try matching district name inside AddressText string
                        foreach (var d in allDistricts)
                        {
                            if (!string.IsNullOrEmpty(d.DistrictName) && addr.AddressText.Contains(d.DistrictName, StringComparison.OrdinalIgnoreCase))
                            {
                                addr.DistrictID = d.DistrictID;
                                break;
                            }
                        }

                        // If still null, fallback to first district or default (e.g. 1)
                        if (addr.DistrictID == null && allDistricts.Any())
                        {
                            addr.DistrictID = allDistricts.First().DistrictID;
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FixAddressDistrictIds error: " + ex.Message);
            }
        }

        public static Dictionary<string, string[]> GetCityDistrictsData()
        {
            return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Adana", new[] { "Aladağ","Ceyhan","Çukurova","Feke","İmamoğlu","Karaisalı","Karataş","Kozan","Pozantı","Saimbeyli","Sarıçam","Seyhan","Tufanbeyli","Yumurtalık","Yüreğir" } },
                { "Adıyaman", new[] { "Besni","Çelikhan","Gerger","Gölbaşı","Kahta","Merkez","Samsat","Sincik","Tut" } },
                { "Afyonkarahisar", new[] { "Başmakçı","Bayat","Bolvadin","Çay","Çobanlar","Dazkırı","Dinar","Emirdağ","Evciler","Hocalar","İhsaniye","İscehisar","Kızılören","Merkez","Sandıklı","Sinanpaşa","Sultandağı","Şuhut" } },
                { "Ağrı", new[] { "Diyadin","Doğubayazıt","Eleşkirt","Hamur","Merkez","Patnos","Taşlıçay","Tutak" } },
                { "Amasya", new[] { "Göynücek","Gümüşhacıköy","Hamamözü","Merkez","Merzifon","Suluova","Taşova" } },
                { "Ankara", new[] { "Akyurt","Altındağ","Ayaş","Bala","Beypazarı","Çamlıdere","Çankaya","Çubuk","Elmadağ","Etimesgut","Evren","Gölbaşı","Güdül","Haymana","Kalecik","Kahramankazan","Keçiören","Kızılcahamam","Mamak","Nallıhan","Polatlı","Pursaklar","Sincan","Şereflikoçhisar","Yenimahalle" } },
                { "Antalya", new[] { "Akseki","Aksu","Alanya","Demre","Döşemealtı","Elmalı","Finike","Gazipaşa","Gündoğmuş","İbradı","Kaş","Kemer","Kepez","Konyaaltı","Korkuteli","Kumluca","Manavgat","Muratpaşa","Serik" } },
                { "Artvin", new[] { "Ardanuç","Arhavi","Borçka","Hopa","Kemalpaşa","Merkez","Murgul","Şavşat","Yusufeli" } },
                { "Aydın", new[] { "Bozdoğan","Buharkent","Çine","Didim","Efeler","Germencik","İncirliova","Karacasu","Karpuzlu","Koçarlı","Köşk","Kuşadası","Kuyucak","Nazilli","Söke","Sultanhisar","Yenipazar" } },
                { "Balıkesir", new[] { "Altıeylül","Ayvalık","Balya","Bandırma","Bigadiç","Burhaniye","Dursunbey","Edremit","Erdek","Gömeç","Gönen","Havran","İvrindi","Karesi","Kepsut","Manyas","Marmara","Savaştepe","Sındırgı","Susurluk" } },
                { "Bilecik", new[] { "Bozüyük","Gölpazarı","İnhisar","Merkez","Osmaneli","Pazaryeri","Söğüt","Yenipazar" } },
                { "Bingöl", new[] { "Adaklı","Genç","Karlıova","Kiğı","Merkez","Solhan","Yayladere","Yedisu" } },
                { "Bitlis", new[] { "Adilcevaz","Ahlat","Güroymak","Hizan","Merkez","Mutki","Tatvan" } },
                { "Bolu", new[] { "Dörtdivan","Gerede","Göynük","Kıbrıscık","Mengen","Merkez","Mudurnu","Seben","Yeniçağa" } },
                { "Burdur", new[] { "Ağlasun","Altınyayla","Bucak","Çavdır","Çeltikçi","Gölhisar","Karamanlı","Kemer","Merkez","Tefenni","Yeşilova" } },
                { "Bursa", new[] { "Büyükorhan","Gemlik","Gürsu","Harmancık","İnegöl","İznik","Karacabey","Keles","Kestel","Mudanya","Mustafakemalpaşa","Nilüfer","Orhaneli","Orhangazi","Osmangazi","Yenişehir","Yıldırım" } },
                { "Çanakkale", new[] { "Ayvacık","Bayramiç","Biga","Bozcaada","Çan","Eceabat","Ezine","Gelibolu","Gökçeada","Lapseki","Merkez","Yenice" } },
                { "Çankırı", new[] { "Atkaracalar","Bayramören","Çerkeş","Eldivan","Ilgaz","Kızılırmak","Korgun","Kurşunlu","Merkez","Orta","Şabanözü","Yapraklı" } },
                { "Çorum", new[] { "Alaca","Bayat","Boğazkale","Dodurga","İskilip","Kargı","Laçin","Mecitözü","Merkez","Oğuzlar","Ortaköy","Osmancık","Sungurlu","Uğurludağ" } },
                { "Denizli", new[] { "Acıpayam","Babadağ","Baklan","Bekilli","Beyağaç","Bozkurt","Buldan","Çal","Çameli","Çardak","Çivril","Güney","Honaz","Kale","Merkezefendi","Pamukkale","Sarayköy","Serinhisar","Tavas" } },
                { "Diyarbakır", new[] { "Bağlar","Bismil","Çermik","Çınar","Çüngüş","Dicle","Eğil","Ergani","Hani","Hazro","Kayapınar","Kocaköy","Kulp","Lice","Silvan","Sur","Yenişehir" } },
                { "Edirne", new[] { "Enez","Havsa","İpsala","Keşan","Lalapaşa","Meriç","Merkez","Süloğlu","Uzunköprü" } },
                { "Elazığ", new[] { "Ağın","Alacakaya","Arıcak","Baskil","Karakoçan","Keban","Kovancılar","Maden","Merkez","Palu","Sivrice" } },
                { "Erzincan", new[] { "Çayırlı","İliç","Kemah","Kemaliye","Merkez","Otlukbeli","Refahiye","Tercan","Üzümlü" } },
                { "Erzurum", new[] { "Aşkale","Aziziye","Çat","Hınıs","Horasan","İspir","Karaçoban","Karayazı","Köprüköy","Narman","Oltu","Olur","Palandöken","Pasinler","Pazaryolu","Şenkaya","Tekman","Tortum","Uzundere","Yakutiye" } },
                { "Eskişehir", new[] { "Alpu","Beylikova","Çifteler","Günyüzü","Han","İnönü","Mahmudiye","Mihalgazi","Mihalıççık","Odunpazarı","Sarıcakaya","Seyitgazi","Sivrihisar","Tepebaşı" } },
                { "Gaziantep", new[] { "Araban","İslahiye","Karkamış","Nizip","Nurdağı","Oğuzeli","Şahinbey","Şehitkâmil","Yavuzeli" } },
                { "Giresun", new[] { "Alucra","Bulancak","Çamoluk","Çanakçı","Dereli","Doğankent","Espiye","Eynesil","Görele","Güce","Keşap","Merkez","Piraziz","Şebinkarahisar","Tirebolu","Yağlıdere" } },
                { "Gümüşhane", new[] { "Kelkit","Köse","Kürtün","Merkez","Şiran","Torul" } },
                { "Hakkari", new[] { "Çukurca","Derecik","Merkez","Şemdinli","Yüksekova" } },
                { "Hatay", new[] { "Altınözü","Antakya","Arsuz","Belen","Defne","Dörtyol","Erzin","Hassa","İskenderun","Kırıkhan","Kumlu","Payas","Reyhanlı","Samandağ","Yayladağı" } },
                { "Isparta", new[] { "Aksu","Atabey","Eğirdir","Gelendost","Gönen","Keçiborlu","Merkez","Senirkent","Sütçüler","Şarkikaraağaç","Uluborlu","Yalvaç","Yenişarbademli" } },
                { "Mersin", new[] { "Akdeniz","Anamur","Aydıncık","Bozyazı","Çamlıyayla","Erdemli","Gülnar","Mezitli","Mut","Silifke","Tarsus","Toroslar","Yenişehir" } },
                { "İstanbul", new[] { "Adalar","Arnavutköy","Ataşehir","Avcılar","Bağcılar","Bahçelievler","Bakırköy","Başakşehir","Bayrampaşa","Beşiktaş","Beykoz","Beylikdüzü","Beyoğlu","Büyükçekmece","Çatalca","Çekmeköy","Esenler","Esenyurt","Eyüpsultan","Fatih","Gaziosmanpaşa","Güngören","Kadıköy","Kağıthane","Kartal","Küçükçekmece","Maltepe","Pendik","Sancaktepe","Sarıyer","Silivri","Sultanbeyli","Sultangazi","Şile","Şişli","Tuzla","Ümraniye","Üsküdar","Zeytinburnu" } },
                { "İzmir", new[] { "Aliağa","Balçova","Bayındır","Bayraklı","Bergama","Beydağ","Bornova","Buca","Çeşme","Çiğli","Dikili","Foça","Gaziemir","Güzelbahçe","Karabağlar","Karaburun","Karşıyaka","Kemalpaşa","Kınık","Kiraz","Konak","Menderes","Menemen","Narlıdere","Ödemiş","Seferihisar","Selçuk","Tire","Torbalı","Urla" } },
                { "Kars", new[] { "Akyaka","Arpaçay","Digor","Kağızman","Merkez","Sarıkamış","Selim","Susuz" } },
                { "Kastamonu", new[] { "Abana","Ağlı","Araç","Azdavay","Bozkurt","Cide","Çatalzeytin","Daday","Devrekani","Doğanyurt","Hanönü","İhsangazi","İnebolu","Küre","Merkez","Pınarbaşı","Seydiler","Şenpazar","Taşköprü","Tosya" } },
                { "Kayseri", new[] { "Akkışla","Bünyan","Develi","Felahiye","Hacılar","İncesu","Kocasinan","Melikgazi","Özvatan","Pınarbaşı","Sarıoğlan","Sarız","Talas","Tomarza","Yahyalı","Yeşilhisar" } },
                { "Kırklareli", new[] { "Babaeski","Demirköy","Kofçaz","Lüleburgaz","Merkez","Pehlivanköy","Pınarhisar","Vize" } },
                { "Kırşehir", new[] { "Akçakent","Akpınar","Boztepe","Çiçekdağı","Kaman","Merkez","Mucur" } },
                { "Kocaeli", new[] { "Başiskele","Çayırova","Darıca","Derince","Dilovası","Gebze","Gölcük","İzmit","Kandıra","Karamürsel","Kartepe","Körfez" } },
                { "Konya", new[] { "Ahırlı","Akören","Akşehir","Altınekin","Beyşehir","Bozkır","Cihanbeyli","Çeltik","Çumra","Derbent","Derebucak","Doğanhisar","Emirgazi","Ereğli","Güneysınır","Hadım","Halkapınar","Hüyük","Ilgın","Kadınhanı","Karapınar","Karatay","Kulu","Meram","Sarayönü","Selçuklu","Seydişehir","Taşkent","Tuzlukçu","Yalıhüyük","Yunak" } },
                { "Kütahya", new[] { "Altıntaş","Aslanapa","Çavdarhisar","Domaniç","Dumlupınar","Emet","Gediz","Hisarcık","Merkez","Pazarlar","Simav","Şaphane","Tavşanlı" } },
                { "Malatya", new[] { "Akçadağ","Arapgir","Arguvan","Battalgazi","Darende","Doğanşehir","Doğanyol","Hekimhan","Kale","Kuluncak","Pütürge","Yazıhan","Yeşilyurt" } },
                { "Manisa", new[] { "Ahmetli","Akhisar","Alaşehir","Demirci","Gölmarmara","Gördes","Kırkağaç","Köprübaşı","Kula","Salihli","Sarıgöl","Saruhanlı","Selendi","Soma","Şehzadeler","Turgutlu","Yunusemre" } },
                { "Kahramanmaraş", new[] { "Afşin","Andırın","Çağlayancerit","Dulkadiroğlu","Ekinözü","Elbistan","Göksun","Nurhak","Onikişubat","Pazarcık","Türkoğlu" } },
                { "Mardin", new[] { "Artuklu","Dargeçit","Derik","Kızıltepe","Mazıdağı","Midyat","Nusaybin","Ömerli","Savur","Yeşilli" } },
                { "Muğla", new[] { "Bodrum","Dalaman","Datça","Fethiye","Kavaklıdere","Köyceğiz","Marmaris","Menteşe","Milas","Ortaca","Seydikemer","Ula","Yatağan" } },
                { "Muş", new[] { "Bulanık","Hasköy","Korkut","Malazgirt","Merkez","Varto" } },
                { "Nevşehir", new[] { "Acıgöl","Avanos","Derinkuyu","Gülşehir","Hacıbektaş","Kozaklı","Merkez","Ürgüp" } },
                { "Niğde", new[] { "Altunhisar","Bor","Çamardı","Çiftlik","Merkez","Ulukışla" } },
                { "Ordu", new[] { "Akkuş","Altınordu","Aybastı","Çamaş","Çatalpınar","Çaybaşı","Fatsa","Gölköy","Gülyalı","Gürgentepe","İkizce","Kabadüz","Kabataş","Korgan","Kumru","Mesudiye","Perşembe","Ulubey","Ünye" } },
                { "Rize", new[] { "Ardeşen","Çamlıhemşin","Çayeli","Derepazarı","Fındıklı","Güneysu","Hemşin","İkizdere","İyidere","Kalkandere","Merkez","Pazar" } },
                { "Sakarya", new[] { "Adapazarı","Akyazı","Arifiye","Erenler","Ferizli","Geyve","Hendek","Karapürçek","Karasu","Kaynarca","Kocaali","Pamukova","Sapanca","Serdivan","Söğütlü","Taraklı" } },
                { "Samsun", new[] { "Alaçam","Asarcık","Atakum","Ayvacık","Bafra","Canik","Çarşamba","Havza","İlkadım","Kavak","Ladik","Salıpazarı","Tekkeköy","Terme","Vezirköprü","Yakakent" } },
                { "Siirt", new[] { "Baykan","Eruh","Kurtalan","Merkez","Pervari","Şirvan","Tillo" } },
                { "Sinop", new[] { "Ayancık","Boyabat","Dikmen","Durağan","Erfelek","Gerze","Merkez","Saraydüzü","Türkeli" } },
                { "Sivas", new[] { "Akıncılar","Altınyayla","Divriği","Doğanşar","Gemerek","Gölova","Güneykaya","Gürün","Hafik","İmranlı","Kangal","Koyulhisar","Merkez","Suşehri","Şarkışla","Ulaş","Yıldızeli","Zara" } },
                { "Tekirdağ", new[] { "Çerkezköy","Çorlu","Ergene","Hayrabolu","Kapaklı","Malkara","Marmaraereğlisi","Muratlı","Saray","Süleymanpaşa","Şarköy" } },
                { "Tokat", new[] { "Almus","Artova","Başçiftlik","Erbaa","Merkez","Niksar","Pazar","Reşadiye","Sulusaray","Turhal","Yeşilyurt","Zile" } },
                { "Trabzon", new[] { "Akçaabat","Araklı","Arsin","Beşikdüzü","Çarşıbaşı","Çaykara","Dernekpazarı","Düzköy","Hayrat","Köprübaşı","Maçka","Of","Ortahisar","Sürmene","Şalpazarı","Tonya","Vakfıkebir","Yomra" } },
                { "Tunceli", new[] { "Çemişgezek","Hozat","Mazgirt","Merkez","Nazımiye","Ovacık","Pertek","Pülümür" } },
                { "Şanlıurfa", new[] { "Akçakale","Birecik","Bozova","Ceylanpınar","Eyyübiye","Halfeti","Haliliye","Harran","Hilvan","Karaköprü","Siverek","Suruç","Viranşehir" } },
                { "Uşak", new[] { "Banaz","Eşme","Karahallı","Merkez","Sivaslı","Ulubey" } },
                { "Van", new[] { "Bahçesaray","Başkale","Çaldıran","Çatak","Edremit","Erciş","Gevaş","Gürpınar","İpekyolu","Muradiye","Özalp","Saray","Tuşba" } },
                { "Yozgat", new[] { "Akdağmadeni","Aydıncık","Boğazlıyan","Çandır","Çayıralan","Çekerek","Kadışehri","Merkez","Saraykent","Sarıkaya","Sorgun","Şefaatli","Yenifakılı","Yerköy" } },
                { "Zonguldak", new[] { "Alaplı","Çaycuma","Devrek","Ereğli","Gökçebey","Kilimli","Kozlu","Merkez" } },
                { "Aksaray", new[] { "Ağaçören","Eskil","Gülağaç","Güzelyurt","Merkez","Ortaköy","Sarıyahşi","Sultanhanı" } },
                { "Bayburt", new[] { "Aydıntepe","Demirözü","Merkez" } },
                { "Karaman", new[] { "Ayrancı","Başyayla","Ermenek","Kazımkarabekir","Merkez","Sarıveliler" } },
                { "Kırıkkale", new[] { "Bahşılı","Balışeyh","Çelebi","Delice","Karakeçili","Keskin","Merkez","Sulakyurt","Yahşihan" } },
                { "Batman", new[] { "Beşiri","Gercüş","Hasankeyf","Kozluk","Merkez","Sason" } },
                { "Şırnak", new[] { "Beytüşşebap","Cizre","Güçlükonak","İdil","Merkez","Silopi","Uludere" } },
                { "Bartın", new[] { "Amasra","Kurucaşile","Merkez","Ulus" } },
                { "Ardahan", new[] { "Çıldır","Damal","Göle","Hanak","Merkez","Posof" } },
                { "Iğdır", new[] { "Aralık","Karakoyunlu","Merkez","Tuzluca" } },
                { "Yalova", new[] { "Altınova","Armutlu","Çınarcık","Çiftlikköy","Merkez","Termal" } },
                { "Karabük", new[] { "Eflani","Eskipazar","Merkez","Ovacık","Safranbolu","Yenice" } },
                { "Kilis", new[] { "Elbeyli","Merkez","Musabeyli","Polateli" } },
                { "Osmaniye", new[] { "Bahçe","Düziçi","Hasanbeyli","Kadirli","Merkez","Sumbas","Toprakkale" } },
                { "Düzce", new[] { "Akçakoca","Cumayeri","Çilimli","Gölyaka","Gümüşova","Kaynaşlı","Merkez","Yığılca" } }
            };
        }

        public static async Task SeedSupplierTestAccountsAsync(ApplicationDbContext context)
        {
            try
            {
                string rawPassword = "Test1234*_";
                string passwordHash = PasswordHasher.HashPassword(rawPassword);

                var testSuppliers = new List<(string Email, string CompanyName, string Official, string Phone, string TCNO, string TaxOffice, string Detail)>
                {
                    ("tedarikci1@test.com", "Atlas Metal & Sac Sanayi A.Ş.", "Burak Çelik", "05321010001", "12345678901", "Ostim Vergi Dairesi", "Lazer kesim, büküm, profil ve sac işleme alanında uzman tedarikçi."),
                    ("tedarikci2@test.com", "Ege Plastik & Ambalaj Ltd. Şti.", "Gizem Yılmaz", "05321010002", "23456789012", "Bornova Vergi Dairesi", "Endüstriyel plastik enjeksiyon ve özel ambalaj çözümleri üreticisi."),
                    ("tedarikci3@test.com", "Nova Elektronik & Otomasyon A.Ş.", "Selim Doğan", "05321010003", "34567890123", "Kadıköy Vergi Dairesi", "PLC, sensör, sürücü sistemleri ve endüstriyel elektronik tedariki."),
                    ("tedarikci4@test.com", "Karadeniz Kereste & Ahşap San.", "Hakan Demir", "05321010004", "45678901234", "Trabzon Vergi Dairesi", "Masif ahşap, palet, inşaatlık kereste ve orman ürünleri tedarikçisi."),
                    ("tedarikci5@test.com", "Zirve Kimya & Hammadde San. Tic.", "Merve Kaya", "05321010005", "56789012345", "Tuzla Vergi Dairesi", "Sanayi kimyasalları, solventler ve hammadde distribütörü."),
                    ("tedarikci6@test.com", "Birlik Rulman & Hırdavat Ltd.", "Kenan Aydın", "05321010006", "67890123456", "Nilüfer Vergi Dairesi", "Bilyalı rulmanlar, kayış-kasnak ve genel endüstriyel hırdavat ürünleri."),
                    ("tedarikci7@test.com", "Yıldız Lojistik & Kargo Çözümleri", "Serdar Koç", "05321010007", "78901234567", "Mersin Vergi Dairesi", "Yurtiçi taşımacılık, depolama ve lojistik tedarik hizmetleri."),
                    ("tedarikci8@test.com", "Tekno Enerji & Kablo Sanayi", "Tolga Güler", "05321010008", "89012345678", "Gaziantep Vergi Dairesi", "Enerji kabloları, trafo ve elektrik pano ekipmanları imalatçısı."),
                    ("tedarikci9@test.com", "Başak Cıvata & Bağlantı Elemanları", "Oğuz Kurt", "05321010009", "90123456789", "Konya Vergi Dairesi", "Paslanmaz cıvata, somun, pul ve özel imalat bağlantı elemanları."),
                    ("tedarikci10@test.com", "Vizyon Endüstriyel Çelik Ltd.", "Can Arslan", "05321010010", "10987654321", "Gebze Vergi Dairesi", "Vasıflı çelik, transmisyon milleri ve boru profil tedarikçisi.")
                };

                foreach (var item in testSuppliers)
                {
                    var existing = await context.Companies.FirstOrDefaultAsync(c => c.CompanyMail == item.Email);
                    if (existing == null)
                    {
                        var newCompany = new Company
                        {
                            CompanyName = item.CompanyName,
                            CompanyOfficial = item.Official,
                            CompanyMail = item.Email,
                            CompanyPhone = item.Phone,
                            TCNO = item.TCNO,
                            TaxOffice = item.TaxOffice,
                            CompanyDetailText = item.Detail,
                            CompanyPassword = passwordHash,
                            CompanyTypeID = 1, // Tedarikçi Firma
                            MembershipTypeID = 1 // Standart Üyelik
                        };
                        await context.Companies.AddAsync(newCompany);
                    }
                    else
                    {
                        // Ensure credentials and role match requirements
                        existing.CompanyName = item.CompanyName;
                        existing.CompanyOfficial = item.Official;
                        existing.CompanyPhone = item.Phone;
                        existing.TCNO = item.TCNO;
                        existing.TaxOffice = item.TaxOffice;
                        existing.CompanyDetailText = item.Detail;
                        existing.CompanyPassword = passwordHash;
                        existing.CompanyTypeID = 1;
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SeedSupplierTestAccounts note: " + ex.Message);
            }
        }

        private static async Task SeedSampleVerifiedReviewsAsync(ApplicationDbContext context)
        {
            try
            {
                // Find test suppliers
                var suppliers = await context.Companies.Where(c => c.CompanyTypeID == 1).Take(5).ToListAsync();
                if (!suppliers.Any()) return;

                // Find or create test buyer
                var testBuyer = await context.Companies.FirstOrDefaultAsync(c => c.CompanyTypeID == 2 || c.CompanyTypeID == 3);
                if (testBuyer == null)
                {
                    testBuyer = await context.Companies.FirstOrDefaultAsync();
                }

                int buyerId = testBuyer?.CompanyID ?? 1;
                string buyerName = testBuyer?.CompanyName ?? "Kurumsal Alıcı A.Ş.";

                var firstSupplier = suppliers.First();
                var order = await context.Orders.FirstOrDefaultAsync(o => o.SupplierCompanyID == firstSupplier.CompanyID);
                if (order == null)
                {
                    order = new Order
                    {
                        OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-0001",
                        BuyerCompanyID = buyerId,
                        BuyerCompanyName = buyerName,
                        SupplierCompanyID = firstSupplier.CompanyID,
                        SupplierCompanyName = firstSupplier.CompanyName ?? "Atlas Metal Sanayi",
                        OrderDate = DateTime.UtcNow.AddDays(-10),
                        DeliveryDate = DateTime.UtcNow.AddDays(-7),
                        TotalAmount = 45000,
                        TaxAmount = 9000,
                        GrandTotal = 54000,
                        PaymentMethod = "Kredi Kartı",
                        PaymentStatus = "Ödendi",
                        OrderStatus = "Teslim Edildi",
                        DeliveryCity = "İstanbul",
                        DeliveryAddress = "Organize Sanayi Bölgesi 4. Cadde No:12",
                        ReceiverName = "Satın Alma Departmanı",
                        TrackingNumber = "YRT-849201948",
                        ShippingCarrier = "Yurtiçi Kargo",
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                ProductName = "Endüstriyel Lazer Kesim Sac Parçaları (3mm DKP)",
                                Sku = "SKU-B2B-0101",
                                UnitPrice = 45000,
                                Quantity = 1,
                                LineTotal = 45000,
                                SupplierCompanyID = firstSupplier.CompanyID,
                                SupplierCompanyName = firstSupplier.CompanyName
                            }
                        }
                    };
                    await context.Orders.AddAsync(order);
                    await context.SaveChangesAsync();
                }

                // Tedarikçinin alıcı olarak verdiği siparişi de ekle (Önceden aldığım ürünler testi için)
                var supplierAsBuyerOrder = await context.Orders.FirstOrDefaultAsync(o => o.BuyerCompanyID == firstSupplier.CompanyID);
                if (supplierAsBuyerOrder == null && suppliers.Count > 1)
                {
                    var secondSupplier = suppliers[1];
                    supplierAsBuyerOrder = new Order
                    {
                        OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-0002",
                        BuyerCompanyID = firstSupplier.CompanyID,
                        BuyerCompanyName = firstSupplier.CompanyName ?? "Atlas Metal Sanayi",
                        BuyerEmail = firstSupplier.CompanyMail,
                        SupplierCompanyID = secondSupplier.CompanyID,
                        SupplierCompanyName = secondSupplier.CompanyName ?? "Ege Plastik & Ambalaj Ltd. Şti.",
                        OrderDate = DateTime.UtcNow.AddDays(-4),
                        DeliveryDate = DateTime.UtcNow.AddDays(-1),
                        TotalAmount = 28500,
                        TaxAmount = 5700,
                        GrandTotal = 34200,
                        PaymentMethod = "Banka Havalesi / EFT",
                        PaymentStatus = "Ödendi",
                        OrderStatus = "Teslim Edildi",
                        DeliveryCity = "Ankara",
                        DeliveryAddress = "Ostim Sanayi Sitesi 1234. Sokak No:45",
                        ReceiverName = "Burak Çelik (Mal Kabul)",
                        TrackingNumber = "MNG-728193810",
                        ShippingCarrier = "MNG Kargo",
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                ProductName = "Özel İmalat Endüstriyel Koruma ve Paketleme Kasası",
                                Sku = "SKU-AMB-2040",
                                UnitPrice = 28500,
                                Quantity = 2,
                                CategoryName = "Ambalaj & Plastik",
                                LineTotal = 57000,
                                SupplierCompanyID = secondSupplier.CompanyID,
                                SupplierCompanyName = secondSupplier.CompanyName
                            }
                        }
                    };
                    await context.Orders.AddAsync(supplierAsBuyerOrder);
                    await context.SaveChangesAsync();
                }

                // testBuyer şifresini Test1234*_ olarak güncelle
                if (testBuyer != null)
                {
                    testBuyer.CompanyPassword = PasswordHasher.HashPassword("Test1234*_");
                    await context.SaveChangesAsync();
                }

                // Sample Verified Reviews from Buyers to Suppliers (10-star system)
                bool hasReviews = await context.Reviews.AnyAsync(r => r.ReviewScore != null && r.ReviewScore > 0);
                if (!hasReviews)
                {
                    var sampleBuyerReviews = new List<Reviews>
                    {
                        new Reviews
                        {
                            OrderID = order.OrderID,
                            CompanyID = firstSupplier.CompanyID,
                            ReviewerCompanyID = buyerId,
                            ReviewerName = buyerName,
                            ReviewType = "BuyerToSeller",
                            ReviewScore = 10.0,
                            DeliveryScore = 10.0,
                            QualityScore = 10.0,
                            CommunicationScore = 9.8,
                            ReviewText = "Ürünler belirtilen teknik toleranslara birebir uygun üretilmiş ve 2 iş gününde fabrikamıza teslim edildi. Ambalajlama, irsaliye ve fatura süreçleri son derece profesyoneldi. 10/10.",
                            ReviewDate = DateTime.UtcNow.AddDays(-6),
                            IsVerifiedPurchase = true
                        },
                        new Reviews
                        {
                            OrderID = order.OrderID,
                            CompanyID = firstSupplier.CompanyID,
                            ReviewerCompanyID = buyerId,
                            ReviewerName = "Borusan Tedarik & İmalat San. A.Ş.",
                            ReviewType = "BuyerToSeller",
                            ReviewScore = 9.7,
                            DeliveryScore = 9.5,
                            QualityScore = 10.0,
                            CommunicationScore = 9.7,
                            ReviewText = "Lazer kesim ve büküm kalitesi mükemmel. Satış temsilcisi sipariş sürecinin her aşamasında bilgilendirme yaptı. Güvenilir ve kurumsal bir tedarikçi.",
                            ReviewDate = DateTime.UtcNow.AddDays(-4),
                            IsVerifiedPurchase = true
                        },
                        new Reviews
                        {
                            OrderID = order.OrderID,
                            CompanyID = firstSupplier.CompanyID,
                            ReviewerCompanyID = buyerId,
                            ReviewerName = "Aselsan Yan Sanayi İş Ortakları Ltd.",
                            ReviewType = "BuyerToSeller",
                            ReviewScore = 9.5,
                            DeliveryScore = 9.6,
                            QualityScore = 9.8,
                            CommunicationScore = 9.2,
                            ReviewText = "Şartnameye tam uygunluk sağlandı. Malzeme sertifikaları ve kalite test raporları kargo ile birlikte eksiksiz ulaştırıldı. Teşekkür ederiz.",
                            ReviewDate = DateTime.UtcNow.AddDays(-2),
                            IsVerifiedPurchase = true
                        }
                    };

                    foreach (var sup in suppliers.Skip(1))
                    {
                        sampleBuyerReviews.Add(new Reviews
                        {
                            OrderID = order.OrderID,
                            CompanyID = sup.CompanyID,
                            ReviewerCompanyID = buyerId,
                            ReviewerName = buyerName,
                            ReviewType = "BuyerToSeller",
                            ReviewScore = 9.8,
                            DeliveryScore = 9.7,
                            QualityScore = 9.9,
                            CommunicationScore = 9.8,
                            ReviewText = "Düzenli sipariş verdiğimiz güvenilir çözüm ortağımız. Malzeme kalitesi ve sevkiyat hızı sektör standartlarının üzerinde.",
                            ReviewDate = DateTime.UtcNow.AddDays(-3),
                            IsVerifiedPurchase = true
                        });
                    }

                    // Sample Seller Review to Buyer (Two-Way Reputation)
                    sampleBuyerReviews.Add(new Reviews
                    {
                        OrderID = order.OrderID,
                        CompanyID = buyerId, // Target is buyer
                        ReviewerCompanyID = firstSupplier.CompanyID, // Reviewer is seller
                        ReviewerName = firstSupplier.CompanyName ?? "Atlas Metal Sanayi",
                        ReviewType = "SellerToBuyer",
                        ReviewScore = 10.0,
                        CommunicationScore = 10.0,
                        DeliveryScore = 10.0,
                        ReviewText = "Alıcı firma ödeme ve mal kabul sürecinde son derece dakik ve şeffaf davrandı. Sipariş detayları ve onayları çok hızlı tamamlandı. Çalışmaktan büyük memnuniyet duyduğumuz kurumsal bir alıcı.",
                        ReviewDate = DateTime.UtcNow.AddDays(-5),
                        IsVerifiedPurchase = true
                    });

                    await context.Reviews.AddRangeAsync(sampleBuyerReviews);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SeedSampleVerifiedReviews error: " + ex.Message);
            }
        }
    }
}
