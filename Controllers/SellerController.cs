using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;

namespace satinalmaportali2.Controllers
{
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SellerController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentCompanyId()
        {
            var cidClaim = User.FindFirst("CompanyID")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(cidClaim) && int.TryParse(cidClaim, out int cid))
            {
                return cid;
            }
            return 0;
        }

        /// <summary>
        /// Satıcı Sayfası (Tedarikçi Mağazası & 10 Yıldızlı Değerlendirme Karnesi)
        /// URL: /Seller/Detail/{id} veya /Seller/{id}
        /// </summary>
        [HttpGet]
        [Route("Seller/{id:int}")]
        [Route("Seller/Detail/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyID == id);

            if (company == null)
            {
                // Eğer bulunamazsa ilk tedarikçiye veya anasayfaya yönlendir
                TempData["ErrorMessage"] = "Aradığınız satıcı firma profili bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            // Adres ve Şehir Bilgisi Çözümle
            string cityName = "İstanbul";
            string districtName = "Organize Sanayi";
            string addressText = "";

            if (company.AddressID.HasValue)
            {
                var addr = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.AddressID == company.AddressID.Value);
                if (addr != null)
                {
                    addressText = addr.AddressText ?? "";
                    if (addr.DistrictID.HasValue)
                    {
                        var dist = await _context.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.DistrictID == addr.DistrictID.Value);
                        if (dist != null)
                        {
                            districtName = dist.DistrictName ?? districtName;
                            if (dist.TownID.HasValue)
                            {
                                var town = await _context.Towns.AsNoTracking().FirstOrDefaultAsync(t => t.TownID == dist.TownID.Value);
                                if (town != null) cityName = town.TownName ?? cityName;
                            }
                        }
                    }
                }
            }

            // Üyelik Türü
            string memType = "Kurumsal Onaylı Tedarikçi";
            if (company.MembershipTypeID.HasValue)
            {
                var mt = await _context.MembershipTypes.AsNoTracking().FirstOrDefaultAsync(m => m.MembershipTypeID == company.MembershipTypeID.Value);
                if (mt != null && !string.IsNullOrWhiteSpace(mt.MembershipTypeName))
                {
                    memType = mt.MembershipTypeName;
                }
            }

            // Satıcıya Verilen 10 Yıldızlı Değerlendirmeleri Yükle
            var reviewsQuery = _context.Reviews
                .Include(r => r.Order)
                .AsNoTracking()
                .Where(r => r.CompanyID == company.CompanyID && r.ReviewType == "BuyerToSeller" && r.ReviewScore.HasValue)
                .OrderByDescending(r => r.ReviewDate);

            var dbReviews = await reviewsQuery.ToListAsync();

            int totalReviews = dbReviews.Count;
            double overallScore = 9.8;
            double deliveryAvg = 9.7;
            double qualityAvg = 9.9;
            double commAvg = 9.8;

            if (totalReviews > 0)
            {
                overallScore = Math.Round(dbReviews.Average(r => r.ReviewScore!.Value), 1);

                var delivList = dbReviews.Where(r => r.DeliveryScore.HasValue).Select(r => r.DeliveryScore!.Value).ToList();
                if (delivList.Any()) deliveryAvg = Math.Round(delivList.Average(), 1);

                var qualList = dbReviews.Where(r => r.QualityScore.HasValue).Select(r => r.QualityScore!.Value).ToList();
                if (qualList.Any()) qualityAvg = Math.Round(qualList.Average(), 1);

                var commList = dbReviews.Where(r => r.CommunicationScore.HasValue).Select(r => r.CommunicationScore!.Value).ToList();
                if (commList.Any()) commAvg = Math.Round(commList.Average(), 1);
            }

            // 10'dan 1'e Yıldız Dağılımını Hesapla
            var starCounts = new Dictionary<int, int>();
            var starPercentages = new Dictionary<int, double>();
            for (int s = 10; s >= 1; s--)
            {
                starCounts[s] = 0;
                starPercentages[s] = 0.0;
            }

            foreach (var r in dbReviews)
            {
                int roundedStar = (int)Math.Clamp(Math.Round(r.ReviewScore!.Value), 1, 10);
                starCounts[roundedStar]++;
            }

            if (totalReviews > 0)
            {
                for (int s = 10; s >= 1; s--)
                {
                    starPercentages[s] = Math.Round((double)starCounts[s] / totalReviews * 100.0, 1);
                }
            }
            else
            {
                // Varsayılan vitrin dağılımı
                starPercentages[10] = 85.0;
                starPercentages[9] = 15.0;
            }

            // Değerlendirme Kartı DTO'larına Dönüştür
            var reviewItems = dbReviews.Select(r =>
            {
                string rName = !string.IsNullOrWhiteSpace(r.ReviewerName) ? r.ReviewerName : "Doğrulanmış B2B Alıcı";
                string inits = "BA";
                var words = rName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2) inits = $"{words[0][0]}{words[1][0]}".ToUpper();
                else if (words.Length == 1 && words[0].Length >= 2) inits = words[0].Substring(0, 2).ToUpper();

                return new ReviewDetailItemDto
                {
                    ReviewID = r.ReviewID,
                    ReviewScore = r.ReviewScore ?? 10.0,
                    DeliveryScore = r.DeliveryScore,
                    QualityScore = r.QualityScore,
                    CommunicationScore = r.CommunicationScore,
                    ReviewText = r.ReviewText,
                    ReviewDate = r.ReviewDate,
                    ReviewerName = rName,
                    ReviewerInitials = inits,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    OrderNumber = r.Order?.OrderNumber,
                    ProductName = r.Order?.OrderItems.FirstOrDefault()?.ProductName
                };
            }).ToList();

            // Satıcının Vitrindeki Ürünlerini Yükle
            var compName = company.CompanyName ?? "";
            var productsQuery = _context.Items.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(compName))
            {
                productsQuery = productsQuery.Where(i => i.Brand == compName);
            }

            var dbProducts = await productsQuery.Take(24).ToListAsync();

            var productItems = dbProducts.Select(p => new SellerProductItemDto
            {
                ItemID = p.ItemID,
                ItemName = p.ItemName ?? "Endüstriyel Ürün",
                Sku = $"SKU-B2B-{p.ItemID:D4}",
                CategoryName = !string.IsNullOrWhiteSpace(p.Properties) && p.Properties.StartsWith("Kategori: ")
                    ? p.Properties.Substring("Kategori: ".Length)
                    : "Endüstriyel Ürünler",
                UnitPrice = (decimal)(p.ItemPrice ?? 0),
                Stock = p.Quantity ?? 50,
                Properties = p.Properties,
                ImageUrl = "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?w=500&auto=format&fit=crop&q=80"
            }).ToList();

            // Giriş yapmış kullanıcının bu satıcıdan siparişi var mı kontrol et
            int currentCompanyId = GetCurrentCompanyId();
            bool hasPurchased = false;
            int? eligibleOrderId = null;

            if (currentCompanyId > 0)
            {
                var eligibleOrder = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.BuyerCompanyID == currentCompanyId &&
                               (o.SupplierCompanyID == company.CompanyID || (o.SupplierCompanyName != null && o.SupplierCompanyName == compName)) &&
                               o.OrderStatus != "İptal")
                    .OrderByDescending(o => o.OrderID)
                    .FirstOrDefaultAsync();

                if (eligibleOrder != null)
                {
                    hasPurchased = true;
                    eligibleOrderId = eligibleOrder.OrderID;
                }
            }

            var vm = new SellerProfileViewModel
            {
                CompanyID = company.CompanyID,
                CompanyName = compName,
                CompanyOfficial = company.CompanyOfficial,
                CompanyLogo = company.CompanyLogo,
                CompanyMail = company.CompanyMail,
                CompanyPhone = company.CompanyPhone,
                CityName = cityName,
                DistrictName = districtName,
                AddressText = addressText,
                CompanyDetailText = company.CompanyDetailText,
                MembershipTypeName = memType,
                OverallScore = overallScore,
                TotalReviews = totalReviews,
                DeliveryScoreAvg = deliveryAvg,
                QualityScoreAvg = qualityAvg,
                CommunicationScoreAvg = commAvg,
                StarCounts = starCounts,
                StarPercentages = starPercentages,
                Reviews = reviewItems,
                Products = productItems,
                HasUserPurchasedFromSeller = hasPurchased,
                EligibleOrderID = eligibleOrderId
            };

            return View(vm);
        }
    }
}
