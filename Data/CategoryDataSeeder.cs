using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Models;

namespace satinalmaportali2.Data
{
    public class CategoryNodeDef
    {
        public string Name { get; set; }
        public List<CategoryNodeDef> Children { get; set; }

        public CategoryNodeDef(string name, params CategoryNodeDef[] children)
        {
            Name = name;
            Children = new List<CategoryNodeDef>(children);
        }
    }

    public static class CategoryDataSeeder
    {
        public static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            try
            {
                // Sütun boyutunu kontrol et ve genişlet (150 karakter)
                try
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE category ALTER COLUMN categoryname TYPE VARCHAR(150);");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Category column alter notice: {ex.Message}");
                }

                // Mevcut kategorileri çek
                var dbCategories = await context.Category.ToListAsync();
                Console.WriteLine($"[CategoryDataSeeder] Mevcut kategori sayısı: {dbCategories.Count}");

                // Ağaç yapısını oluştur
                var rootDefinitions = GetCategoryTreeDefinitions();

                int newInsertedCount = 0;

                // Özyinelemeli (recursive) kategori kontrolü ve ekleme fonksiyonu
                async Task ProcessNodeAsync(CategoryNodeDef nodeDef, int? parentId)
                {
                    string cleanName = nodeDef.Name.Trim();

                    // Halihazırda var mı kontrol et (Ad ve Üst Kategori eşleşmesi)
                    var existing = dbCategories.FirstOrDefault(c =>
                        string.Equals(c.CategoryName?.Trim(), cleanName, StringComparison.OrdinalIgnoreCase) &&
                        ((!c.ParentCategoryID.HasValue && !parentId.HasValue) ||
                         (c.ParentCategoryID.HasValue && parentId.HasValue && c.ParentCategoryID.Value == parentId.Value)));

                    int currentId;

                    if (existing != null)
                    {
                        currentId = existing.CategoryID;
                    }
                    else
                    {
                        var newCat = new Category
                        {
                            CategoryName = cleanName,
                            ParentCategoryID = parentId
                        };

                        context.Category.Add(newCat);
                        await context.SaveChangesAsync();

                        currentId = newCat.CategoryID;
                        dbCategories.Add(newCat);
                        newInsertedCount++;
                    }

                    // Alt kategorileri işle
                    foreach (var childDef in nodeDef.Children)
                    {
                        await ProcessNodeAsync(childDef, currentId);
                    }
                }

                foreach (var rootDef in rootDefinitions)
                {
                    await ProcessNodeAsync(rootDef, null);
                }

                if (newInsertedCount > 0)
                {
                    Console.WriteLine($"[CategoryDataSeeder] 🎉 Başarıyla {newInsertedCount} yeni kategori eklendi. Toplam: {dbCategories.Count}");
                }
                else
                {
                    Console.WriteLine($"[CategoryDataSeeder] ✓ Tüm kategoriler zaten güncel ({dbCategories.Count} kategori).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CategoryDataSeeder] Hata oluştu: {ex.Message}");
            }
        }

        public static List<CategoryNodeDef> GetCategoryTreeDefinitions()
        {
            return new List<CategoryNodeDef>
            {
                // ==========================================
                // 1. ELEKTRONİK (Mevcut ID: 1)
                // ==========================================
                new CategoryNodeDef("Elektronik",
                    new CategoryNodeDef("Bilgisayar", // Mevcut ID: 3
                        new CategoryNodeDef("Laptop"), // Mevcut ID: 4
                        new CategoryNodeDef("Dizüstü"), // Mevcut ID: 5
                        new CategoryNodeDef("Mini Pc"), // Mevcut ID: 6
                        new CategoryNodeDef("Masaüstü & İş İstasyonu (Workstation)"),
                        new CategoryNodeDef("Sunucu (Server) & Veri Depolama"),
                        new CategoryNodeDef("Monitör & Endüstriyel Ekranlar"),
                        new CategoryNodeDef("Bilgisayar Bileşenleri (RAM, SSD, Ekran Kartı)"),
                        new CategoryNodeDef("Çevre Birimleri & Aksesuarlar"),
                        new CategoryNodeDef("Yazıcı, Tarayıcı & Fotokopi"),
                        new CategoryNodeDef("Kesintisiz Güç Kaynağı (UPS)")
                    ),
                    new CategoryNodeDef("Ağ & İletişim (Network) Sistemleri",
                        new CategoryNodeDef("Switch & Router (Yönlendirici)"),
                        new CategoryNodeDef("Access Point & Kablosuz Ağlar"),
                        new CategoryNodeDef("Fiber Optik & Ağ Kablolama"),
                        new CategoryNodeDef("Server Kabinetleri & Aksesuarları"),
                        new CategoryNodeDef("Firewall & Ağ Güvenlik Donanımları")
                    ),
                    new CategoryNodeDef("Güvenlik, CCTV & Alarm Sistemleri",
                        new CategoryNodeDef("IP & AHD Güvenlik Kameraları"),
                        new CategoryNodeDef("NVR / DVR Kayıt Cihazları"),
                        new CategoryNodeDef("Geçiş Kontrol, Kartlı & Biyometrik Sistemler"),
                        new CategoryNodeDef("Yangın & Gaz Algılama Sistemleri"),
                        new CategoryNodeDef("Hırsız Alarm & Güvenlik Sensörleri")
                    ),
                    new CategoryNodeDef("Ofis & Barkod Elektroniği",
                        new CategoryNodeDef("Barkod Okuyucu & El Terminalleri"),
                        new CategoryNodeDef("Barkod & Etiket Yazıcıları"),
                        new CategoryNodeDef("Projeksiyon & İnteraktif Sunum Cihazları"),
                        new CategoryNodeDef("Konferans & Ses Sistemleri"),
                        new CategoryNodeDef("Para Sayma & Sahte Para Dedektörleri")
                    ),
                    new CategoryNodeDef("Endüstriyel Elektronik & Otomasyon",
                        new CategoryNodeDef("PLC & Genişleme Modülleri"),
                        new CategoryNodeDef("HMI Operatör Dokunmatik Panelleri"),
                        new CategoryNodeDef("Endüstriyel Sensörler & Fotoseller"),
                        new CategoryNodeDef("Frekans İnvertörleri & Hız Kontrol Cihazları"),
                        new CategoryNodeDef("Endüstriyel DIN Ray Güç Kaynakları"),
                        new CategoryNodeDef("Röleler & Zaman Röleleri")
                    )
                ),

                // ==========================================
                // 2. GİYİM (Mevcut ID: 2)
                // ==========================================
                new CategoryNodeDef("Giyim",
                    new CategoryNodeDef("Kurumsal İş Kıyafetleri",
                        new CategoryNodeDef("Promosyon Tişört & Polo Yaka"),
                        new CategoryNodeDef("Güvenlik Personeli Üniformaları"),
                        new CategoryNodeDef("Sağlık & Laboratuvar Kıyafetleri (Scrubs/Önlük)"),
                        new CategoryNodeDef("Mutfak & Aşçı Kıyafetleri / Önlükler"),
                        new CategoryNodeDef("Temizlik Personeli Kıyafetleri")
                    ),
                    new CategoryNodeDef("Günlük & Kurumsal Giyim",
                        new CategoryNodeDef("Erkek Kurumsal Giyim (Gömlek/Pantolon/Takım)"),
                        new CategoryNodeDef("Kadın Kurumsal Giyim (Gömlek/Pantolon/Ceket)"),
                        new CategoryNodeDef("Kaban, Mont, Yelek & Polar"),
                        new CategoryNodeDef("Yağmurluk & Rüzgarlıklar")
                    ),
                    new CategoryNodeDef("Endüstriyel Tekstil & Kumaşlar",
                        new CategoryNodeDef("İş Elbisesi Kumaşları (Gabardin, Ripstop)"),
                        new CategoryNodeDef("Alev Almaz & Antistatik Teknik Kumaşlar"),
                        new CategoryNodeDef("Su İtici & Membran Kumaşlar"),
                        new CategoryNodeDef("Nonwoven (Tela) & Endüstriyel Keçeler"),
                        new CategoryNodeDef("Branda, Tente & Çadır Kumaşları")
                    )
                ),

                // ==========================================
                // 3. MAKİNE, EKİPMAN & İMALAT
                // ==========================================
                new CategoryNodeDef("Makine, Ekipman & İmalat",
                    new CategoryNodeDef("Takım Tezgahları & CNC Sistemleri",
                        new CategoryNodeDef("CNC Torna & Dik İşleme Merkezleri"),
                        new CategoryNodeDef("Freze & Matkap Tezgahları"),
                        new CategoryNodeDef("Şerit & Daire Testere Tezgahları"),
                        new CategoryNodeDef("Taşlama & Erozyon Tezgahları"),
                        new CategoryNodeDef("Lazer & Plazma Kesim Tezgahları"),
                        new CategoryNodeDef("Abkant Pres & Giyotin Makaslar")
                    ),
                    new CategoryNodeDef("Talaşlı İmalat & Kesici Takımlar",
                        new CategoryNodeDef("Karbür Parmak Frezeler & Matkap Uçları"),
                        new CategoryNodeDef("Torna Elmasları & Katerler"),
                        new CategoryNodeDef("Kılavuz, Pafta & Raybalar"),
                        new CategoryNodeDef("Takım Tutucular (BT40, SK40) & Pensler"),
                        new CategoryNodeDef("Makinist Mengeneleri & Bağlama Elemanları")
                    ),
                    new CategoryNodeDef("Kaynak & Lehimleme Sistemleri",
                        new CategoryNodeDef("MIG/MAG Gazaltı Kaynak Makineleri"),
                        new CategoryNodeDef("TIG (Argon) Kaynak Makineleri"),
                        new CategoryNodeDef("İnvertör Çanta & Elektrod Kaynak Makineleri"),
                        new CategoryNodeDef("Plazma Kesme Makineleri"),
                        new CategoryNodeDef("Kaynak Telleri, Elektrodlar & Gazlar"),
                        new CategoryNodeDef("Kaynak Torçları & Yedek Parçaları")
                    ),
                    new CategoryNodeDef("Hidrolik & Pnömatik Sistemler",
                        new CategoryNodeDef("Hidrolik Silindirler & Pompalar"),
                        new CategoryNodeDef("Hidrolik Valfler, Bloklar & Hortumlar"),
                        new CategoryNodeDef("Pnömatik Pistonlar & Silindirler"),
                        new CategoryNodeDef("Pnömatik Yön Valfleri & Şartlandırıcılar"),
                        new CategoryNodeDef("Poliüretan Hava Hortumları & Kaplinler"),
                        new CategoryNodeDef("Vidalı & Pistonlu Hava Kompresörleri"),
                        new CategoryNodeDef("Basınçlı Hava Kurutucuları & Filtreleri")
                    ),
                    new CategoryNodeDef("Güç Aktarım & Mekanik Elemanlar",
                        new CategoryNodeDef("Rulmanlar & Yataklı Rulmanlar"),
                        new CategoryNodeDef("V Kayışlar, Triger Kayışlar & Kasnaklar"),
                        new CategoryNodeDef("Konveyör & Rulo Zincirleri ve Dişlileri"),
                        new CategoryNodeDef("Elektrik Motorları, Redüktörler & Sürücüler"),
                        new CategoryNodeDef("Servo Motorlar & Step Motorlar"),
                        new CategoryNodeDef("Lineer Kızak, Araba & Vidalı Mil Sistemleri"),
                        new CategoryNodeDef("Endüstriyel Kaplinler & Şaft Bağlantıları")
                    )
                ),

                // ==========================================
                // 4. ELEKTRİK & ENERJİ SİSTEMLERİ
                // ==========================================
                new CategoryNodeDef("Elektrik & Enerji Sistemleri",
                    new CategoryNodeDef("Alçak Gerilim Şalt Malzemeleri",
                        new CategoryNodeDef("Otomatik Sigortalar (W Otomat)"),
                        new CategoryNodeDef("Kaçak Akım Koruma Röleleri"),
                        new CategoryNodeDef("Kompakt Şalterler (Termik Manyetik)"),
                        new CategoryNodeDef("Kontaktörler & Motor Koruma Şalterleri"),
                        new CategoryNodeDef("Kompanzasyon Kondansatörleri & Röleler"),
                        new CategoryNodeDef("Ray Klemensler & Dağıtım Blokları")
                    ),
                    new CategoryNodeDef("Kablo & Kablo Yönetimi",
                        new CategoryNodeDef("AG Güç Kabloları (NYY, NYA, TTR)"),
                        new CategoryNodeDef("Halojensiz (HFFR) & Yangın Kabloları"),
                        new CategoryNodeDef("Kumanda & Sinyal Kabloları"),
                        new CategoryNodeDef("Delikli Sac & Tel Kablo Kanalları"),
                        new CategoryNodeDef("Kablo Merdivenleri & Destek Elemanları"),
                        new CategoryNodeDef("Spiral Borular & Kablo Rakorları"),
                        new CategoryNodeDef("Kablo Pabuçları, Yüksükler & Cırt Kelepçeler")
                    ),
                    new CategoryNodeDef("Elektrik Panoları & Dağıtım",
                        new CategoryNodeDef("Duvar Tipi & Dikili Tip Sac Panolar"),
                        new CategoryNodeDef("Polyester & Plastik Su Geçirmez Panolar"),
                        new CategoryNodeDef("Pano Klimaları & Fan Filtre Sistemleri"),
                        new CategoryNodeDef("Şantiye & Saha Dağıtım Panoları")
                    ),
                    new CategoryNodeDef("Yenilenebilir Enerji & Güç Sistemleri",
                        new CategoryNodeDef("Güneş (Solar) Panelleri"),
                        new CategoryNodeDef("Solar İnvertörler & Şarj Kontrol Cihazları"),
                        new CategoryNodeDef("Solar Aküler & Jel Aküler"),
                        new CategoryNodeDef("Dizel & Benzinli Jeneratörler"),
                        new CategoryNodeDef("Voltaj Regülatörleri & Transformatörler")
                    ),
                    new CategoryNodeDef("Endüstriyel Aydınlatma",
                        new CategoryNodeDef("Yüksek Tavan (High Bay) LED Armatürler"),
                        new CategoryNodeDef("LED Projektörler & Saha Aydınlatmaları"),
                        new CategoryNodeDef("Etanj & Toz/Nem Korumalı Armatürler"),
                        new CategoryNodeDef("Acil Durum Aydınlatma & Yönlendirme"),
                        new CategoryNodeDef("Ex-Proof (Patlamaya Dayanıklı) Armatürler")
                    )
                ),

                // ==========================================
                // 5. HIRDAVAT & EL ALETLERİ
                // ==========================================
                new CategoryNodeDef("Hırdavat & El Aletleri",
                    new CategoryNodeDef("Elektrikli & Akülü El Aletleri",
                        new CategoryNodeDef("Akülü Vidalama & Darbeli Matkaplar"),
                        new CategoryNodeDef("Kırıcı & Deliciler (SDS-Max / Plus)"),
                        new CategoryNodeDef("Avuç Taşlama & Polisaj Makineleri"),
                        new CategoryNodeDef("Dekupaj, Daire & Tilki Kuyruğu Testereler"),
                        new CategoryNodeDef("Akülü Somun Sıkmalar & Torklu Aletler"),
                        new CategoryNodeDef("Sıcak Hava Tabancaları & Gönye Kesmeler")
                    ),
                    new CategoryNodeDef("Mekanik El Aletleri",
                        new CategoryNodeDef("Kombine & Yıldız Anahtar Takımları"),
                        new CategoryNodeDef("Lokma Anahtar Takımları & Cırcır Kolları"),
                        new CategoryNodeDef("Pense, Kargaburun & Yan Keski Çeşitleri"),
                        new CategoryNodeDef("Tornavida Takımları & Bits Uç Setleri"),
                        new CategoryNodeDef("Çekiç, Balyoz, Keski & Zımbalar"),
                        new CategoryNodeDef("Boru Anahtarları, Kerpeten & Mengene/İşkenceler")
                    ),
                    new CategoryNodeDef("Ölçüm & Test Cihazları",
                        new CategoryNodeDef("Dijital Kumpaslar & Mikrometreler"),
                        new CategoryNodeDef("Şerit Metreler & Lazer Mesafe Ölçerler"),
                        new CategoryNodeDef("Su Terazileri & Çizgi Lazer Hizalamalar"),
                        new CategoryNodeDef("Dijital Multimetreler & Pensampermetreler"),
                        new CategoryNodeDef("Sıcaklık, Nem & Mesafe Sensörleri"),
                        new CategoryNodeDef("Tork Anahtarları (Torkmetre)")
                    ),
                    new CategoryNodeDef("Bağlantı Elemanları & Hırdavat",
                        new CategoryNodeDef("Metrik & İnç Cıvatalar (8.8, 10.9, Inox)"),
                        new CategoryNodeDef("Akıllı Vidalar & Sac Vidaları"),
                        new CategoryNodeDef("Somunlar, Pullar & Rondelalar"),
                        new CategoryNodeDef("Çelik, Plastik & Kimyasal Dübeller"),
                        new CategoryNodeDef("Pop Perçinler & Kör Somunlar"),
                        new CategoryNodeDef("Hortum Kelepçeleri & Boru Askıları")
                    ),
                    new CategoryNodeDef("Aşındırıcılar & Zımparalar",
                        new CategoryNodeDef("Metal & Taş Kesme Diskleri"),
                        new CategoryNodeDef("Taşlama & Çapak Alma Taşları"),
                        new CategoryNodeDef("Flap Diskler & Mop Zımparalar"),
                        new CategoryNodeDef("Bant Zımpara & Rulo Zımparalar"),
                        new CategoryNodeDef("Parlatma & Keçe Diskleri")
                    )
                ),

                // ==========================================
                // 6. İŞ SAĞLIĞI VE GÜVENLİĞİ (İSG)
                // ==========================================
                new CategoryNodeDef("İş Sağlığı ve Güvenliği (İSG)",
                    new CategoryNodeDef("Baş, Göz & Kulak Koruma",
                        new CategoryNodeDef("Endüstriyel Baretler & Darbe Şapkaları"),
                        new CategoryNodeDef("İş Güvenlik Gözlükleri & Siperlikler"),
                        new CategoryNodeDef("Gürültü Önleyici Kulaklıklar & Tıkaçlar"),
                        new CategoryNodeDef("Toz Maskeleri & Yarım/Tam Gaz Maskeleri")
                    ),
                    new CategoryNodeDef("El & Kol Koruma",
                        new CategoryNodeDef("Kesilmeye Dirençli Seviye D/F Eldivenler"),
                        new CategoryNodeDef("Nitril & Poliüretan Kaplı Montaj Eldivenleri"),
                        new CategoryNodeDef("Kimyasal & Asit Dayanımlı Eldivenler"),
                        new CategoryNodeDef("Kaynakçı & Isıya Dayanıklı Deri Eldivenler"),
                        new CategoryNodeDef("Tek Kullanımlık Nitril/Lateks Eldivenler")
                    ),
                    new CategoryNodeDef("İş Ayakkabıları & Çizmeler",
                        new CategoryNodeDef("S1 / S1P Çelik Burunlu İş Ayakkabıları"),
                        new CategoryNodeDef("S3 Su Geçirmez Kompozit Burunlu Botlar"),
                        new CategoryNodeDef("ESD & Antistatik Güvenlik Ayakkabıları"),
                        new CategoryNodeDef("Çelik Burunlu PVC & Poliüretan Çizmeler")
                    ),
                    new CategoryNodeDef("Koruyucu İş Giysileri",
                        new CategoryNodeDef("Reflektörlü İkaz Yelekleri & Montları"),
                        new CategoryNodeDef("Gabardin & Kot İş Pantolonları / Tulumlar"),
                        new CategoryNodeDef("Alev Almaz & Antistatik Kıyafetler"),
                        new CategoryNodeDef("Kimyasal & Toz Koruyucu Lamineli Tulumlar")
                    ),
                    new CategoryNodeDef("Yüksekte Çalışma Donanımları",
                        new CategoryNodeDef("Paraşüt Tipi Emniyet Kemerleri"),
                        new CategoryNodeDef("Şok Emicili Lanyard & Yaşam Hatları"),
                        new CategoryNodeDef("Otomatik Düşüş Tutucular & Makaralar"),
                        new CategoryNodeDef("Karabinalar, Halatlar & Ankraj Noktaları")
                    ),
                    new CategoryNodeDef("Tesis Güvenliği, Yangın & İlk Yardım",
                        new CategoryNodeDef("Yangın Söndürme Tüpleri & Yangın Dolapları"),
                        new CategoryNodeDef("Acil Göz & Vücut Duşları"),
                        new CategoryNodeDef("İlk Yardım Çantaları & Ecza Dolapları"),
                        new CategoryNodeDef("İSG İkaz Levhaları & Trafik Konileri"),
                        new CategoryNodeDef("LOTO (Kilitleme / Etiketleme) Ekipmanları")
                    )
                ),

                // ==========================================
                // 7. AMBALAJ, PAKETLEME & DEPOLAMA
                // ==========================================
                new CategoryNodeDef("Ambalaj, Paketleme & Depolama",
                    new CategoryNodeDef("Endüstriyel Sarma & Paketleme",
                        new CategoryNodeDef("El & Makine Tipi Streç Filmler"),
                        new CategoryNodeDef("Baloncuklu Naylon & Şilteler"),
                        new CategoryNodeDef("Koli Bantları & Özel Baskılı Bantlar"),
                        new CategoryNodeDef("Oluklu Mukavva & Karton Koli Çeşitleri"),
                        new CategoryNodeDef("Plastik, Polyester & Çelik Çemberler"),
                        new CategoryNodeDef("Çemberleme Makineleri & Tokaları")
                    ),
                    new CategoryNodeDef("Taşıma Kapları & Kasalar",
                        new CategoryNodeDef("Ahşap Euro Paletler & Plastik Paletler"),
                        new CategoryNodeDef("İstiflenebilir Plastik Kasalar"),
                        new CategoryNodeDef("Plastik & Sac Variller"),
                        new CategoryNodeDef("IBC Tanklar (1000 Litre Sıvı Tankı)"),
                        new CategoryNodeDef("Big-Bag Çuvallar & Polipropilen Çuvallar")
                    ),
                    new CategoryNodeDef("Depo Raf & Düzenleme Sistemleri",
                        new CategoryNodeDef("Ağır Yük (Palet) Raf Sistemleri"),
                        new CategoryNodeDef("Hafif & Orta Yük Raf Sistemleri"),
                        new CategoryNodeDef("Konsol Kollu Uzun Yük Rafları"),
                        new CategoryNodeDef("Takım Dolapları & Çekmeceli Malzeme Rafları"),
                        new CategoryNodeDef("Plastik Avadanlık & Kutu Setleri")
                    ),
                    new CategoryNodeDef("İntralojistik & İstifleme Araçları",
                        new CategoryNodeDef("Manuel & Akülü Transpaletler"),
                        new CategoryNodeDef("Yarı Akülü & Tam Akülü İstif Makineleri"),
                        new CategoryNodeDef("Forklift Çatalları & Ataşmanları"),
                        new CategoryNodeDef("Paket Taşıma Arabaları & Platform Arabalar"),
                        new CategoryNodeDef("Makaralı Konveyörler & Rulo Hatları")
                    )
                ),

                // ==========================================
                // 8. KİMYASALLAR, YAĞLAR & BOYALAR
                // ==========================================
                new CategoryNodeDef("Kimyasallar, Yağlar & Boyalar",
                    new CategoryNodeDef("Endüstriyel Madeni Yağlar & Gresler",
                        new CategoryNodeDef("Hidrolik Sistem Yağları (ISO VG 32/46/68)"),
                        new CategoryNodeDef("Endüstriyel Dişli & Şanzıman Yağları"),
                        new CategoryNodeDef("Kompresör & Türbin Yağları"),
                        new CategoryNodeDef("Yüksek Sıcaklık & Rulman Gresleri"),
                        new CategoryNodeDef("Metal İşleme Sıvıları (Bor Yağı, Kesme Yağı)"),
                        new CategoryNodeDef("Pas Sökücü & Bakım Spreyleri")
                    ),
                    new CategoryNodeDef("Endüstriyel Yapıştırıcılar & Mastikler",
                        new CategoryNodeDef("Poliüretan Mastikler & Silikonlar"),
                        new CategoryNodeDef("Hızlı Yapıştırıcılar (Cyanoacrylate)"),
                        new CategoryNodeDef("Epoksi Yapıştırıcılar & Tamir Macunları"),
                        new CategoryNodeDef("Anaerobik Vida Sabitleyiciler & Sıvı Contalar"),
                        new CategoryNodeDef("Hotmelt Sıcak Tutkal Çubukları")
                    ),
                    new CategoryNodeDef("Sanayi Boyaları & Kaplamalar",
                        new CategoryNodeDef("Epoksi Zemin Boyaları & Astar Sistemleri"),
                        new CategoryNodeDef("Antipas & Endüstriyel Metal Astarları"),
                        new CategoryNodeDef("Sentetik & Hızlı Kuruyan Son Kat Boyalar"),
                        new CategoryNodeDef("Isıya Dayanıklı Boyalar & Çinko Galvaniz Sprey"),
                        new CategoryNodeDef("Selülozik & Sentetik Tinerler, Çözücüler")
                    ),
                    new CategoryNodeDef("Tesis Temizlik & Hijyen Kimyasalları",
                        new CategoryNodeDef("Ağır Yağ & Kir Çözücü Endüstriyel Sıvılar"),
                        new CategoryNodeDef("Parça Yıkama & Daldırma Solventleri"),
                        new CategoryNodeDef("Kireç, Pas & Harç Temizleyiciler"),
                        new CategoryNodeDef("Sanayi Tipi Ağır Kir Sıvı Sabun & Kremleri"),
                        new CategoryNodeDef("Yüzey Dezenfektanları & Zemin Kimyasalları")
                    )
                ),

                // ==========================================
                // 9. İNŞAAT, YAPI & TESİSAT
                // ==========================================
                new CategoryNodeDef("İnşaat, Yapı & Tesisat",
                    new CategoryNodeDef("Yapı Kimyasalları & İzolasyon",
                        new CategoryNodeDef("Su Yalıtım Membranları & Sürme İzolasyonlar"),
                        new CategoryNodeDef("Isı Yalıtım Levhaları (Taşyünü, EPS, XPS)"),
                        new CategoryNodeDef("Seramik Yapıştırıcıları & Derz Dolguları"),
                        new CategoryNodeDef("Grout & Epoksi Esaslı Tamir Harçları")
                    ),
                    new CategoryNodeDef("Boru & Sıhhi Tesisat Sistemleri",
                        new CategoryNodeDef("PPRC Temiz Su Boruları & Fitingleri"),
                        new CategoryNodeDef("PVC Atık Su Boruları & Ek Parçaları"),
                        new CategoryNodeDef("PE100 Polietilen Borular & Koruge Borular"),
                        new CategoryNodeDef("Pirinç & Paslanmaz Küresel Vanalar"),
                        new CategoryNodeDef("Kelebek & Çekvalf Çeşitleri"),
                        new CategoryNodeDef("Dalgıç Pompalar, Hidroforlar & Su Sayaçları")
                    ),
                    new CategoryNodeDef("Havalandırma & İklimlendirme (HVAC)",
                        new CategoryNodeDef("Endüstriyel Salyangoz & Radyal Fanlar"),
                        new CategoryNodeDef("Spiral Sac Havalandırma Kanalları"),
                        new CategoryNodeDef("Menfezler, Anemostadlar & Panjurlar"),
                        new CategoryNodeDef("Radyant & Sıcak Hava Isıtıcı Cihazlar"),
                        new CategoryNodeDef("Su Soğutma Kuleleri & Chiller Cihazları")
                    ),
                    new CategoryNodeDef("Endüstriyel Kapı & Bina Donanımları",
                        new CategoryNodeDef("Yangın Çıkış Kapıları & Panik Barları"),
                        new CategoryNodeDef("Endüstriyel Seksiyonel & Hızlı Sarmal Kapılar"),
                        new CategoryNodeDef("Ağır Hizmet Kilitleri & Hidrolik Kapı Yayları")
                    )
                ),

                // ==========================================
                // 10. METAL, PLASTİK & HAMMADDE
                // ==========================================
                new CategoryNodeDef("Metal, Plastik & Hammadde",
                    new CategoryNodeDef("Demir & Çelik Ürünleri",
                        new CategoryNodeDef("Yapısal Profiller (HEA, HEB, IPE, NPU)"),
                        new CategoryNodeDef("Kutu Profiller & Sanayi Boruları"),
                        new CategoryNodeDef("DKP & Siyah Sac Levhalar"),
                        new CategoryNodeDef("Galvanizli Düz & Trapez Saclar"),
                        new CategoryNodeDef("İmalat Çelikleri (Transmisyon, 4140, 1040)"),
                        new CategoryNodeDef("Paslanmaz Çelik Sac, Çubuk & Boru (304/316)")
                    ),
                    new CategoryNodeDef("Demir Dışı Renkli Metaller",
                        new CategoryNodeDef("Alüminyum Levhalar, Profiller & Borular"),
                        new CategoryNodeDef("Bakır Levhalar, Borular & Bara Lamalar"),
                        new CategoryNodeDef("Pirinç (Sarı) Çubuklar, Miller & Lamalar"),
                        new CategoryNodeDef("Bronz & Kurşun Döküm Malzemeler")
                    ),
                    new CategoryNodeDef("Mühendislik Plastikleri & Kompozitler",
                        new CategoryNodeDef("Poliamid (Kestamid / Pa6) Levha & Takozlar"),
                        new CategoryNodeDef("Polioksimetilen (Delrin / POM) Çubuklar"),
                        new CategoryNodeDef("Teflon (PTFE) Levha & Silindirler"),
                        new CategoryNodeDef("Polietilen (PE1000 / PE500) Levhalar"),
                        new CategoryNodeDef("Pleksiglas (Akrilik) & Polikarbonat Levhalar")
                    ),
                    new CategoryNodeDef("Kauçuk & Sızdırmazlık Plakaları",
                        new CategoryNodeDef("Kord Bezli & Düz Neopren/NBR Lastik Levhalar"),
                        new CategoryNodeDef("Silikon Levha & Sünger Kauçuk Profiller"),
                        new CategoryNodeDef("Klingerit & Grafit Esaslı Conta Levhaları"),
                        new CategoryNodeDef("Standart & Özel O-Ring / Keçe Çeşitleri")
                    )
                ),

                // ==========================================
                // 11. OFİS, KIRTASİYE & MOBİLYA
                // ==========================================
                new CategoryNodeDef("Ofis, Kırtasiye & Mobilya",
                    new CategoryNodeDef("Kağıt & Baskı Sarfiyatı",
                        new CategoryNodeDef("A4 & A3 Fotokopi Kağıtları (80gr)"),
                        new CategoryNodeDef("Sürekli Form & Lazer Yazıcı Kağıtları"),
                        new CategoryNodeDef("Termal Pos & Yazar Kasa Ruloları"),
                        new CategoryNodeDef("Zarflar, Klasörler & Poşet Dosyalar"),
                        new CategoryNodeDef("Bloknotlar, Not Defterleri & Yapışkan Notlar")
                    ),
                    new CategoryNodeDef("Masaüstü Kırtasiye Gereçleri",
                        new CategoryNodeDef("Tükenmez, Jel & Roller Kalemler"),
                        new CategoryNodeDef("Yazı Tahtası Kalemleri & Fosforlu Markerlar"),
                        new CategoryNodeDef("Zımba Makineleri, Teller & Delgeçler"),
                        new CategoryNodeDef("Evrak Rafları & Masaüstü Düzenleyiciler"),
                        new CategoryNodeDef("Makaslar, Falçatalar & Kesim Matları")
                    ),
                    new CategoryNodeDef("Kurumsal Mutfak & İkram Sarfiyatı",
                        new CategoryNodeDef("Dökme & Poşet Çaylar, Filtre Kahveler"),
                        new CategoryNodeDef("Küp & Toz Şekerler, Tatlandırıcılar"),
                        new CategoryNodeDef("Karton Bardaklar, Karıştırıcılar & Peçeteler"),
                        new CategoryNodeDef("Su Sebilleri & Arıtma Filtre Kartuşları")
                    ),
                    new CategoryNodeDef("Ofis Mobilyaları & Ergonomi",
                        new CategoryNodeDef("Personel Çalışma Masaları & Toplantı Masaları"),
                        new CategoryNodeDef("Ergonomik Fileli Ofis & Müdür Koltukları"),
                        new CategoryNodeDef("Çekmeceli Kesonlar & Metal Dosya Dolapları"),
                        new CategoryNodeDef("Monitör Standları, Ayak & Bel Destekleri")
                    )
                ),

                // ==========================================
                // 12. OTOMOTİV, FİLO & LOJİSTİK
                // ==========================================
                new CategoryNodeDef("Otomotiv, Filo & Lojistik",
                    new CategoryNodeDef("Ticari Araç Yedek Parçaları",
                        new CategoryNodeDef("Fren Balataları, Diskler & Kampanalar"),
                        new CategoryNodeDef("Hava, Yağ, Yakıt & Kurutucu Filtreleri"),
                        new CategoryNodeDef("Debriyaj Setleri & Şanzıman Parçaları"),
                        new CategoryNodeDef("Hava Süspansiyon Körükleri & Amortisörler"),
                        new CategoryNodeDef("Stop Lambaları, Farlar & Yan Sinyaller"),
                        new CategoryNodeDef("12V / 24V Ağır Vasıta Aküleri")
                    ),
                    new CategoryNodeDef("Yük Emniyeti & Lojistik Donanımları",
                        new CategoryNodeDef("Cırcırlı Yük Germe Spanzetleri (5T/10T)"),
                        new CategoryNodeDef("Konteyner Emniyet Kilitleri & Mühürler"),
                        new CategoryNodeDef("Çelik Halatlar, Sapanlar & Çektirmeler"),
                        new CategoryNodeDef("Plastik Yük Köşebentleri & Kaymaz Şilteler"),
                        new CategoryNodeDef("Dunnage Kargo Hava Yastıkları")
                    ),
                    new CategoryNodeDef("Servis & Garaj Bakım Ekipmanları",
                        new CategoryNodeDef("Şişe Krikolar & Havalı Krikolar"),
                        new CategoryNodeDef("Ağır Vasıta Lastik Sökme & Bijon Tabancaları"),
                        new CategoryNodeDef("Atık Yağ Emme & Doldurma Tankları"),
                        new CategoryNodeDef("Akü Şarj & Takviye Cihazları (Booster)"),
                        new CategoryNodeDef("Araç Takip Sistemleri & GPS Filo Cihazları")
                    )
                ),

                // ==========================================
                // 13. MEDİKAL, LABORATUVAR & TEST
                // ==========================================
                new CategoryNodeDef("Medikal, Laboratuvar & Test",
                    new CategoryNodeDef("Laboratuvar Cihazları & Analiz",
                        new CategoryNodeDef("Hassas Terazi & Analitik Tartım Cihazları"),
                        new CategoryNodeDef("Mikroskoplar, Refraktometreler & Büyüteçler"),
                        new CategoryNodeDef("Santrifüj Cihazları & Vorteks Çalkalayıcılar"),
                        new CategoryNodeDef("Manyetik Isıtıcılı Karıştırıcılar"),
                        new CategoryNodeDef("pH Metreler, İletkenlik & TDS Ölçerler")
                    ),
                    new CategoryNodeDef("Laboratuvar Cam & Sarf Malzemeleri",
                        new CategoryNodeDef("Beher, Erlenmayer & Mezürler (Cam)"),
                        new CategoryNodeDef("Deney Tüpleri, Tüplükler & Bagetler"),
                        new CategoryNodeDef("Otomatik Pipetler & Pipet Uçları"),
                        new CategoryNodeDef("Laboratuvar Filtre Kağıtları"),
                        new CategoryNodeDef("Numune Kapları, Şişeler & Petri Kutuları")
                    ),
                    new CategoryNodeDef("Medikal & Hijyen Sarfiyatı",
                        new CategoryNodeDef("Cerrahi Maskeler, Boneler & Galoşlar"),
                        new CategoryNodeDef("Tek Kullanımlık Şırıngalar & İğne Uçları"),
                        new CategoryNodeDef("Steril Gazlı Bezler, Sargı Bezleri & Flasterler"),
                        new CategoryNodeDef("Tıbbi Atık Kutuları & Torbaları")
                    )
                )
            };
        }
    }
}
