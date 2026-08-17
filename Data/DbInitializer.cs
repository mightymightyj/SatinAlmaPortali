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
                            new CompanyTypes { CompanyTypeID = 3, CompanyTypeName = "Hem Alıcı Hem Tedarikçi" }
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
    }
}
