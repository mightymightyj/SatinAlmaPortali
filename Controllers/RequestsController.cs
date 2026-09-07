using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using satinalmaportali2.Helpers;

namespace satinalmaportali2.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IHubContext<PortalHub> _hubContext;

        public RequestsController(ApplicationDbContext context, EmailService emailService, IHubContext<PortalHub> hubContext)
        {
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        private int GetCurrentCompanyId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("CompanyID");
            if (int.TryParse(idClaim, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        private string GetCurrentCompanyName()
        {
            return User.FindFirstValue("CompanyName") ?? User.Identity?.Name ?? "Kurumsal Kullanıcı";
        }

        private string GetCurrentUserType()
        {
            return User.FindFirstValue("UserType") ?? User.FindFirstValue(ClaimTypes.Role) ?? "User";
        }

        /// <summary>
        /// Bir talebin oturum açmış olan firmaya ait olup olmadığını (SenderID veya Firma Adı üzerinden) kesin olarak doğrular.
        /// </summary>
        private bool IsRequestOwnedByCurrentCompany(Requests request, int currentCompanyId, Company? currentCompany)
        {
            if (request == null) return false;

            // 1. Doğrudan SenderID eşleşmesi
            if (currentCompanyId > 0 && request.SenderID.HasValue && request.SenderID.Value == currentCompanyId)
            {
                return true;
            }

            // 2. BuyerInfo firma adı veya kullanıcı eşleşmesi
            if (!string.IsNullOrWhiteSpace(request.BuyerInfo))
            {
                var buyerTrimmed = request.BuyerInfo.Trim();

                if (currentCompany != null)
                {
                    if (!string.IsNullOrWhiteSpace(currentCompany.CompanyName) &&
                        string.Equals(buyerTrimmed, currentCompany.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (!string.IsNullOrWhiteSpace(currentCompany.CompanyOfficial) &&
                        string.Equals(buyerTrimmed, currentCompany.CompanyOfficial.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (!string.IsNullOrWhiteSpace(currentCompany.CompanyMail) &&
                        string.Equals(buyerTrimmed, currentCompany.CompanyMail.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                var sessionCompName = GetCurrentCompanyName();
                if (!string.IsNullOrWhiteSpace(sessionCompName) &&
                    string.Equals(buyerTrimmed, sessionCompName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var userName = User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(userName) &&
                    string.Equals(buyerTrimmed, userName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrWhiteSpace(userEmail) &&
                    string.Equals(buyerTrimmed, userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Talepler sekmesi ana listeleme görünümü.
        /// Gerçek veritabanı kayıtları, teklif sayıları ve durum filtreleriyle listeler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? type, string? status, string? tab = null)
        {
            int currentCompanyId = GetCurrentCompanyId();
            string userType = GetCurrentUserType();
            var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;

            if (string.IsNullOrWhiteSpace(tab))
            {
                tab = "all";
            }
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "Tümü";
            }

            var requestList = new List<RequestListItemViewModel>();

            try
            {
                // Firma isimlerini getir
                var companies = await _context.Companies
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.CompanyID, c => c.CompanyName ?? c.CompanyOfficial ?? $"Firma #{c.CompanyID}");

                // Veritabanındaki gerçek talepleri, kalemleri ve teklifleri çek
                var dbRequests = await _context.Requests
                    .Include(r => r.Offers)
                    .Include(r => r.Items)
                    .AsNoTracking()
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                if (dbRequests != null && dbRequests.Any())
                {
                    foreach (var req in dbRequests)
                    {
                        string buyerName = !string.IsNullOrWhiteSpace(req.BuyerInfo)
                            ? req.BuyerInfo
                            : (req.SenderID.HasValue && companies.TryGetValue(req.SenderID.Value, out var cName) ? cName : "Kurumsal Alıcı");

                        string prodName = !string.IsNullOrWhiteSpace(req.ProductName)
                            ? req.ProductName
                            : (!string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Satın Alma Talebi");

                        bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);
                        var myOffer = currentCompanyId > 0 ? req.Offers.FirstOrDefault(o => o.SupplierID == currentCompanyId) : null;
                        bool isAdmin = userType == "SuperAdmin" || userType == "Admin";

                        // Alıcı veya satıcı haklarını koruma: Kaldırılan talepleri yalnızca talep sahibi, teklif verenler ve admin görebilir
                        if (req.Status == "Kaldırıldı" && !isOwner && myOffer == null && !isAdmin)
                        {
                            continue;
                        }

                        var bestOffer = req.Offers.Any() ? req.Offers.Min(o => o.OfferedPrice) : (decimal?)null;
                        var (cleanNotes, myRejectionReason, _, _, _) = ParseSupplierNotes(myOffer?.SupplierNotes);
                        int revisedOffersCount = req.Offers.Count(o => ParseSupplierNotes(o.SupplierNotes).IsRevised);

                        var itemsList = req.Items.OrderBy(i => i.ItemOrder).Select(i => new RequestItemDto
                        {
                            RequestItemID = i.RequestItemID,
                            RequestID = i.RequestID,
                            ItemOrder = i.ItemOrder,
                            ItemName = i.ItemName,
                            Quantity = i.Quantity,
                            Unit = i.Unit ?? "Adet",
                            EstimatedUnitPrice = i.EstimatedUnitPrice,
                            ItemDescription = i.ItemDescription
                        }).ToList();

                        int itemCount = itemsList.Count > 0 ? itemsList.Count : 1;
                        int totalQty = itemsList.Count > 0 ? (int)Math.Round(itemsList.Sum(i => i.Quantity)) : (req.Quantity ?? 1);

                        requestList.Add(new RequestListItemViewModel
                        {
                            RequestID = req.RequestID,
                            RequestDate = req.RequestDate ?? DateTime.Now,
                            SenderID = req.SenderID,
                            RecieverID = req.RecieverID,
                            BuyerInfo = buyerName,
                            ProductName = prodName,
                            Quantity = totalQty,
                            ItemCount = itemCount,
                            Items = itemsList,
                            RequestType = !string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Satın Alma",
                            Status = !string.IsNullOrWhiteSpace(req.Status) ? req.Status : "Beklemede",
                            Notes = req.Notes,
                            EstimatedBudget = req.EstimatedBudget,
                            TargetDeliveryDate = req.TargetDeliveryDate,
                            CategoryID = req.CategoryID,
                            ApprovedDate = req.ApprovedDate,
                            RejectionReason = req.RejectionReason,
                            OfferCount = req.Offers.Count,
                            BestOfferPrice = bestOffer,
                            IsOwner = isOwner,
                            HasMyOffer = myOffer != null,
                            MyOfferPrice = myOffer?.OfferedPrice,
                            MyOfferDeliveryDays = myOffer?.DeliveryDays,
                            MyOfferNotes = cleanNotes,
                            MyOfferStatus = myOffer?.Status,
                            MyOfferRejectionReason = myRejectionReason ?? (myOffer?.Status == "Reddedildi" ? "Talep için başka bir tedarikçinin teklifi kabul edildi." : null),
                            HasRevisedOffers = revisedOffersCount > 0,
                            RevisedOfferCount = revisedOffersCount
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Veritabanından talepler yüklenirken bir hata oluştu: " + ex.Message;
            }

            // Sekme sayılarını ve genel KPI'ları hesapla (Tab filtresi uygulanmadan ÖNCE)
            int allCount = requestList.Count;
            int myCount = currentCompanyId > 0 ? requestList.Count(r => r.IsOwner) : 0;
            int incomingCount = currentCompanyId > 0 ? requestList.Count(r => r.IsOwner && r.OfferCount > 0) : 0;
            int myBidsCount = currentCompanyId > 0 ? requestList.Count(r => r.HasMyOffer) : 0;

            int totalKpi = requestList.Count;
            int pendingKpi = requestList.Count(r => r.Status == "Beklemede" || r.Status == "Açık" || r.Status == "Teklif Alındı");
            int approvedKpi = requestList.Count(r => r.Status == "Onaylandı" || r.Status == "Kabul Edildi" || r.Status == "Tamamlandı");
            int totalQtyKpi = requestList.Sum(r => r.Quantity);

            // Giriş yapılmamışsa oturum gerektiren sekmeler 'all' sekmesine yönlendirilir
            if (currentCompanyId <= 0 && (tab == "my-requests" || tab == "incoming-offers" || tab == "my-bids"))
            {
                tab = "all";
            }

            var viewModel = new RequestListViewModel
            {
                Requests = requestList,
                AllRequestsCount = allCount,
                MyRequestsCount = myCount,
                IncomingOffersCount = incomingCount,
                MyBidsCount = myBidsCount,
                TotalRequestsCount = totalKpi,
                PendingRequestsCount = pendingKpi,
                ApprovedRequestsCount = approvedKpi,
                TotalQuantityCount = totalQtyKpi,
                SearchQuery = search,
                SelectedType = type,
                SelectedStatus = status,
                ActiveTab = tab ?? "all",
                CurrentCompanyID = currentCompanyId,
                UserType = userType
            };

            return View(viewModel);
        }

        /// <summary>
        /// Yeni Talep Oluşturma POST Action (İstenilenler Listesi Desteği ile)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRequestInputModel input)
        {
            var validItems = input.Items != null ? input.Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0).ToList() : new List<CreateRequestItemInputModel>();

            if (validItems.Count == 0 && string.IsNullOrWhiteSpace(input.ProductName))
            {
                TempData["ErrorMessage"] = "Lütfen talep başlığı veya en az 1 istenilen ürün/hizmet kalemi ekleyiniz.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;

                // Üyelik Seviyesi Talep Limiti Kontrolü
                if (currentCompanyId > 0 && currentCompany != null)
                {
                    int memTypeId = currentCompany.MembershipTypeID ?? MembershipConstants.TierBedava;
                    int maxRequests = MembershipConstants.GetMaxRequests(memTypeId);
                    if (maxRequests < int.MaxValue)
                    {
                        int activeCount = await _context.Requests
                            .Where(r => r.SenderID == currentCompanyId && r.Status != "İptal Edildi" && r.Status != "Tamamlandı" && r.Status != "Reddedildi" && r.Status != "Kaldırıldı")
                            .CountAsync();

                        if (activeCount >= maxRequests)
                        {
                            string tierName = MembershipConstants.GetTierName(memTypeId);
                            TempData["ErrorMessage"] = $"{tierName} üyelik planınızda aynı anda en fazla {maxRequests} aktif talebe sahip olabilirsiniz. (Mevcut aktif talep: {activeCount}). Yeni talep oluşturmak için mevcut taleplerinizi sonuçlandırabilir veya üyeliğinizi yükseltebilirsiniz.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                }

                string companyName = currentCompany?.CompanyName ?? currentCompany?.CompanyOfficial ?? GetCurrentCompanyName();
                string finalBuyerInfo = !string.IsNullOrWhiteSpace(companyName) ? companyName : (!string.IsNullOrWhiteSpace(input.BuyerInfo) ? input.BuyerInfo : "Kurumsal Alıcı");

                string finalTitle = !string.IsNullOrWhiteSpace(input.ProductName) 
                    ? input.ProductName.Trim() 
                    : (validItems.Count > 0 ? (validItems[0].ItemName + (validItems.Count > 1 ? $" ve {validItems.Count - 1} diğer kalem" : "")) : "Satın Alma Talebi");

                int totalQuantity = validItems.Count > 0 ? (int)Math.Round(validItems.Sum(i => i.Quantity)) : (input.Quantity > 0 ? input.Quantity : 1);

                decimal? totalEstimatedBudget = input.EstimatedBudget;
                if (!totalEstimatedBudget.HasValue || totalEstimatedBudget.Value <= 0)
                {
                    decimal calcBudget = validItems.Sum(i => (i.EstimatedUnitPrice ?? 0) * i.Quantity);
                    if (calcBudget > 0)
                    {
                        totalEstimatedBudget = calcBudget;
                    }
                }

                var newRequest = new Requests
                {
                    RequestDate = DateTime.UtcNow,
                    SenderID = currentCompanyId > 0 ? currentCompanyId : null,
                    BuyerInfo = finalBuyerInfo,
                    ProductName = finalTitle,
                    Quantity = totalQuantity,
                    RequestType = string.IsNullOrWhiteSpace(input.RequestType) ? "Satın Alma" : input.RequestType,
                    CategoryID = input.CategoryID,
                    EstimatedBudget = totalEstimatedBudget,
                    TargetDeliveryDate = input.TargetDeliveryDate,
                    Status = "Beklemede",
                    Notes = input.Notes
                };

                if (validItems.Count > 0)
                {
                    int order = 1;
                    foreach (var item in validItems)
                    {
                        newRequest.Items.Add(new RequestItem
                        {
                            ItemOrder = order++,
                            ItemName = item.ItemName.Trim(),
                            Quantity = item.Quantity,
                            Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Adet" : item.Unit.Trim(),
                            EstimatedUnitPrice = item.EstimatedUnitPrice,
                            ItemDescription = item.ItemDescription?.Trim(),
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    newRequest.Items.Add(new RequestItem
                    {
                        ItemOrder = 1,
                        ItemName = finalTitle,
                        Quantity = totalQuantity,
                        Unit = "Adet",
                        EstimatedUnitPrice = totalEstimatedBudget.HasValue && totalQuantity > 0 ? totalEstimatedBudget.Value / totalQuantity : null,
                        ItemDescription = input.Notes,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.Requests.Add(newRequest);
                await _context.SaveChangesAsync();

                await BroadcastRequestCreatedAsync(newRequest);

                TempData["SuccessMessage"] = $"'{finalTitle}' talebiniz ({newRequest.Items.Count} Kalem, Toplam {totalQuantity} Birim) başarıyla oluşturuldu ve tedarikçilere açıldı!";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Talep veritabanına kaydedilirken bir hata oluştu: " + detail;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Dashboard veya sayfa üzerinden AJAX ile Talep Oluşturma Endpoint'i (İstenilenler Listesi Desteği ile)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] CreateRequestInputModel input)
        {
            if (input == null)
            {
                return Json(new { success = false, message = "Geçersiz talep verisi." });
            }

            var validItems = input.Items != null ? input.Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0).ToList() : new List<CreateRequestItemInputModel>();

            if (validItems.Count == 0 && string.IsNullOrWhiteSpace(input.ProductName))
            {
                return Json(new { success = false, message = "Lütfen bir talep başlığı veya en az 1 istenilen ürün/hizmet kalemi ekleyiniz." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;

                // Üyelik Seviyesi Talep Limiti Kontrolü
                if (currentCompanyId > 0 && currentCompany != null)
                {
                    int memTypeId = currentCompany.MembershipTypeID ?? MembershipConstants.TierBedava;
                    int maxRequests = MembershipConstants.GetMaxRequests(memTypeId);
                    if (maxRequests < int.MaxValue)
                    {
                        int activeCount = await _context.Requests
                            .Where(r => r.SenderID == currentCompanyId && r.Status != "İptal Edildi" && r.Status != "Tamamlandı" && r.Status != "Reddedildi" && r.Status != "Kaldırıldı")
                            .CountAsync();

                        if (activeCount >= maxRequests)
                        {
                            string tierName = MembershipConstants.GetTierName(memTypeId);
                            return Json(new
                            {
                                success = false,
                                limitExceeded = true,
                                message = $"{tierName} üyelik planınızda aynı anda en fazla {maxRequests} aktif talebe sahip olabilirsiniz. (Mevcut aktif talep: {activeCount}). Yeni talep açmak için mevcut taleplerinizi sonuçlandırabilir veya üyeliğinizi yükseltebilirsiniz."
                            });
                        }
                    }
                }

                string companyName = currentCompany?.CompanyName ?? currentCompany?.CompanyOfficial ?? GetCurrentCompanyName();
                string finalBuyerInfo = !string.IsNullOrWhiteSpace(companyName) ? companyName : (!string.IsNullOrWhiteSpace(input.BuyerInfo) ? input.BuyerInfo : "Kurumsal Alıcı");

                string finalTitle = !string.IsNullOrWhiteSpace(input.ProductName) 
                    ? input.ProductName.Trim() 
                    : (validItems.Count > 0 ? (validItems[0].ItemName + (validItems.Count > 1 ? $" ve {validItems.Count - 1} diğer kalem" : "")) : "Satın Alma Talebi");

                int totalQuantity = validItems.Count > 0 ? (int)Math.Round(validItems.Sum(i => i.Quantity)) : (input.Quantity > 0 ? input.Quantity : 1);

                decimal? totalEstimatedBudget = input.EstimatedBudget;
                if (!totalEstimatedBudget.HasValue || totalEstimatedBudget.Value <= 0)
                {
                    decimal calcBudget = validItems.Sum(i => (i.EstimatedUnitPrice ?? 0) * i.Quantity);
                    if (calcBudget > 0)
                    {
                        totalEstimatedBudget = calcBudget;
                    }
                }

                var newRequest = new Requests
                {
                    RequestDate = DateTime.UtcNow,
                    SenderID = currentCompanyId > 0 ? currentCompanyId : null,
                    BuyerInfo = finalBuyerInfo,
                    ProductName = finalTitle,
                    Quantity = totalQuantity,
                    RequestType = string.IsNullOrWhiteSpace(input.RequestType) ? "Satın Alma" : input.RequestType,
                    CategoryID = input.CategoryID,
                    EstimatedBudget = totalEstimatedBudget,
                    TargetDeliveryDate = input.TargetDeliveryDate,
                    Status = "Beklemede",
                    Notes = input.Notes
                };

                if (validItems.Count > 0)
                {
                    int order = 1;
                    foreach (var item in validItems)
                    {
                        newRequest.Items.Add(new RequestItem
                        {
                            ItemOrder = order++,
                            ItemName = item.ItemName.Trim(),
                            Quantity = item.Quantity,
                            Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Adet" : item.Unit.Trim(),
                            EstimatedUnitPrice = item.EstimatedUnitPrice,
                            ItemDescription = item.ItemDescription?.Trim(),
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    newRequest.Items.Add(new RequestItem
                    {
                        ItemOrder = 1,
                        ItemName = finalTitle,
                        Quantity = totalQuantity,
                        Unit = "Adet",
                        EstimatedUnitPrice = totalEstimatedBudget.HasValue && totalQuantity > 0 ? totalEstimatedBudget.Value / totalQuantity : null,
                        ItemDescription = input.Notes,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.Requests.Add(newRequest);
                await _context.SaveChangesAsync();

                await BroadcastRequestCreatedAsync(newRequest);

                return Json(new
                {
                    success = true,
                    requestId = newRequest.RequestID,
                    itemCount = newRequest.Items.Count,
                    totalQuantity = totalQuantity,
                    message = $"'{newRequest.ProductName}' talebi ({newRequest.Items.Count} Kalem) başarıyla oluşturuldu!"
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Kayıt hatası: " + detail });
            }
        }

        /// <summary>
        /// Tedarikçinin bir satın alma talebine teklif vermesi (Form POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOffer(SubmitOfferInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Lütfen geçerli bir teklif tutarı ve teslimat süresi giriniz.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                if (currentCompanyId <= 0)
                {
                    TempData["ErrorMessage"] = "Teklif verebilmek için giriş yapmalısınız.";
                    return RedirectToAction("Login", "Account");
                }

                var currentCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);

                var request = await _context.Requests
                    .Include(r => r.Offers)
                    .Include(r => r.Items)
                    .FirstOrDefaultAsync(r => r.RequestID == input.RequestID);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Teklif verilmek istenen talep bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                if (IsRequestOwnedByCurrentCompany(request, currentCompanyId, currentCompany))
                {
                    TempData["ErrorMessage"] = "Kendi oluşturduğunuz ya da firmanıza ait olan satın alma talebine teklif veremezsiniz!";
                    return RedirectToAction(nameof(Index));
                }

                // Talep sonuçlanmış veya kapatılmışsa yeni teklif verilemez / güncellenemez
                bool isRequestClosed = string.Equals(request.Status, "Onaylandı", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(request.Status, "Kabul Edildi", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(request.Status, "Tamamlandı", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(request.Status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);

                if (isRequestClosed)
                {
                    TempData["ErrorMessage"] = "Bu talep sonuçlanmış veya kapatılmış olduğu için teklif verilemez / düzenlenemez.";
                    return RedirectToAction(nameof(Index));
                }

                // Daha önce verilmiş teklif var mı kontrol et
                var existingOffer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == currentCompanyId);

                bool wasRejected = false;
                decimal? prevPrice = null;

                if (existingOffer != null)
                {
                    // Alıcı teklifi onayladıktan sonra kilitlenir
                    if (string.Equals(existingOffer.Status, "Kabul Edildi", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existingOffer.Status, "Onaylandı", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ErrorMessage"] = "Bu teklif alıcı tarafından onaylanmış olduğu için üzerinde değişiklik yapılamaz.";
                        return RedirectToAction(nameof(Index));
                    }

                    var parsedPrev = ParseSupplierNotes(existingOffer.SupplierNotes);
                    wasRejected = string.Equals(existingOffer.Status, "Reddedildi", StringComparison.OrdinalIgnoreCase) 
                                       || !string.IsNullOrWhiteSpace(parsedPrev.RejectionReason)
                                       || parsedPrev.IsRevised;

                    prevPrice = existingOffer.OfferedPrice;
                    string prevRej = parsedPrev.RejectionReason ?? parsedPrev.PreviousRejectionReason ?? "Alıcı tarafından reddedildi";

                    // Güncelle / Revize et
                    existingOffer.OfferedPrice = input.OfferedPrice;
                    existingOffer.DeliveryDays = input.DeliveryDays;
                    existingOffer.ValidityDays = input.ValidityDays;
                    existingOffer.OfferDate = DateTime.UtcNow;
                    existingOffer.Status = "Beklemede";

                    if (wasRejected && prevPrice.HasValue)
                    {
                        string revisionTag = $"[Revize:EskiFiyat={prevPrice.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}|EskiRed={prevRej.Replace("|", " ").Replace("]", "")}]";
                        string cleanUserNote = input.SupplierNotes?.Trim() ?? "";
                        existingOffer.SupplierNotes = string.IsNullOrWhiteSpace(cleanUserNote) ? revisionTag : $"{cleanUserNote} {revisionTag}";
                    }
                    else
                    {
                        existingOffer.SupplierNotes = input.SupplierNotes;
                    }

                    if (request.Status == "Beklemede")
                    {
                        request.Status = "Teklif Alındı";
                    }

                    if (wasRejected)
                    {
                        TempData["SuccessMessage"] = $"#{input.RequestID} numaralı talep için reddedilen teklifiniz revize edilerek {input.OfferedPrice:N2} ₺ olarak alıcıya tekrar iletildi!";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"#{input.RequestID} numaralı talep için teklifiniz {input.OfferedPrice:N2} ₺ olarak güncellendi!";
                    }
                }
                else
                {
                    // Yeni teklif ekle
                    var newOffer = new RequestOffers
                    {
                        RequestID = input.RequestID,
                        SupplierID = currentCompanyId,
                        OfferedPrice = input.OfferedPrice,
                        DeliveryDays = input.DeliveryDays,
                        ValidityDays = input.ValidityDays,
                        OfferDate = DateTime.UtcNow,
                        Status = "Beklemede",
                        SupplierNotes = input.SupplierNotes
                    };
                    _context.RequestOffers.Add(newOffer);

                    if (request.Status == "Beklemede")
                    {
                        request.Status = "Teklif Alındı";
                    }

                    TempData["SuccessMessage"] = $"#{input.RequestID} numaralı talep için {input.OfferedPrice:N2} ₺ tutarındaki teklifiniz başarıyla iletildi!";
                }

                await _context.SaveChangesAsync();

                if (existingOffer != null)
                {
                    await BroadcastOfferSubmittedAsync(request, existingOffer, currentCompany, wasRejected, wasRejected ? prevPrice : null);
                }
                else
                {
                    var createdOffer = await _context.RequestOffers.FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == currentCompanyId);
                    if (createdOffer != null)
                    {
                        await BroadcastOfferSubmittedAsync(request, createdOffer, currentCompany, false, null);
                    }
                }

                // Alıcı firmaya e-posta bildirimini arka planda gönder
                await TriggerOfferReceivedEmailNotificationAsync(request, currentCompany, input, input.OfferedPrice);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Teklif kaydedilirken hata oluştu: " + detail;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Dashboard veya modal üzerinden AJAX ile Teklif Gönderme Endpoint'i
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitOfferAjax([FromBody] SubmitOfferInputModel input)
        {
            if (input == null || input.RequestID <= 0 || input.OfferedPrice <= 0)
            {
                return Json(new { success = false, message = "Lütfen geçerli bir teklif tutarı giriniz." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                if (currentCompanyId <= 0)
                {
                    return Json(new { success = false, message = "Oturum süreniz dolmuş, lütfen tekrar giriş yapın." });
                }

                var currentCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);

                var request = await _context.Requests
                    .Include(r => r.Offers)
                    .Include(r => r.Items)
                    .FirstOrDefaultAsync(r => r.RequestID == input.RequestID);
                if (request == null)
                {
                    return Json(new { success = false, message = "Talep bulunamadı." });
                }

                if (IsRequestOwnedByCurrentCompany(request, currentCompanyId, currentCompany))
                {
                    return Json(new { success = false, message = "Kendi firmanıza ait olan satın alma talebine teklif veremezsiniz!" });
                }

                bool isRequestClosed = string.Equals(request.Status, "Onaylandı", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(request.Status, "Kabul Edildi", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(request.Status, "Tamamlandı", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(request.Status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);

                if (isRequestClosed)
                {
                    return Json(new { success = false, message = "Bu talep sonuçlanmış veya kapatılmış olduğu için teklif verilemez / düzenlenemez." });
                }

                // Kalemler gönderildiyse toplam fiyatı hesapla/doğrula
                decimal calculatedTotalPrice = input.OfferedPrice;
                if (input.Items != null && input.Items.Any(i => i.UnitPrice > 0))
                {
                    decimal sumLineTotals = 0;
                    foreach (var it in input.Items)
                    {
                        var reqItem = request.Items?.FirstOrDefault(ri => ri.RequestItemID == it.RequestItemID);
                        decimal qty = reqItem?.Quantity ?? 1;
                        sumLineTotals += (it.UnitPrice * qty);
                    }
                    if (sumLineTotals > 0)
                    {
                        calculatedTotalPrice = sumLineTotals;
                    }
                }

                var existingOffer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == currentCompanyId);

                if (existingOffer != null)
                {
                    // Alıcı teklifi onayladıktan sonra düzenlenemez
                    if (string.Equals(existingOffer.Status, "Kabul Edildi", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existingOffer.Status, "Onaylandı", StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { success = false, message = "Bu teklif alıcı tarafından onaylandığı için artık düzenlenemez." });
                    }

                    var parsedPrev = ParseSupplierNotes(existingOffer.SupplierNotes);
                    bool wasRejected = string.Equals(existingOffer.Status, "Reddedildi", StringComparison.OrdinalIgnoreCase)
                                       || !string.IsNullOrWhiteSpace(parsedPrev.RejectionReason)
                                       || parsedPrev.IsRevised;

                    decimal prevPrice = existingOffer.OfferedPrice;
                    string prevRej = parsedPrev.RejectionReason ?? parsedPrev.PreviousRejectionReason ?? "Alıcı tarafından reddedildi";

                    existingOffer.OfferedPrice = calculatedTotalPrice;
                    existingOffer.DeliveryDays = input.DeliveryDays;
                    existingOffer.ValidityDays = input.ValidityDays;
                    existingOffer.OfferDate = DateTime.UtcNow;
                    existingOffer.Status = "Beklemede";

                    if (wasRejected)
                    {
                        string revisionTag = $"[Revize:EskiFiyat={prevPrice.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}|EskiRed={prevRej.Replace("|", " ").Replace("]", "")}]";
                        string cleanUserNote = input.SupplierNotes?.Trim() ?? "";
                        existingOffer.SupplierNotes = string.IsNullOrWhiteSpace(cleanUserNote) ? revisionTag : $"{cleanUserNote} {revisionTag}";
                    }
                    else
                    {
                        existingOffer.SupplierNotes = input.SupplierNotes;
                    }

                    // Kalem bazlı teklifleri güncelle
                    if (existingOffer.OfferItems != null && existingOffer.OfferItems.Any())
                    {
                        _context.RequestOfferItems.RemoveRange(existingOffer.OfferItems);
                    }

                    existingOffer.OfferItems ??= new List<RequestOfferItem>();

                    if (input.Items != null && input.Items.Any())
                    {
                        foreach (var it in input.Items)
                        {
                            if (it.UnitPrice >= 0)
                            {
                                var reqItem = request.Items?.FirstOrDefault(ri => ri.RequestItemID == it.RequestItemID);
                                decimal qty = reqItem?.Quantity ?? 1;
                                existingOffer.OfferItems.Add(new RequestOfferItem
                                {
                                    OfferID = existingOffer.OfferID,
                                    RequestItemID = it.RequestItemID,
                                    UnitPrice = it.UnitPrice,
                                    LineTotal = it.UnitPrice * qty,
                                    Notes = it.Notes?.Trim()
                                });
                            }
                        }
                    }

                    if (request.Status == "Beklemede")
                    {
                        request.Status = "Teklif Alındı";
                    }

                    await _context.SaveChangesAsync();
                    await TriggerOfferReceivedEmailNotificationAsync(request, currentCompany, input, calculatedTotalPrice);
                    await BroadcastOfferSubmittedAsync(request, existingOffer, currentCompany, wasRejected, wasRejected ? prevPrice : null);

                    return Json(new
                    {
                        success = true,
                        message = wasRejected
                            ? $"Reddedilen teklifiniz {calculatedTotalPrice:N2} ₺ olarak revize edildi ve alıcıya tekrar iletildi!"
                            : $"Teklifiniz {calculatedTotalPrice:N2} ₺ olarak başarıyla güncellendi!"
                    });
                }
                else
                {
                    var newOffer = new RequestOffers
                    {
                        RequestID = input.RequestID,
                        SupplierID = currentCompanyId,
                        OfferedPrice = calculatedTotalPrice,
                        DeliveryDays = input.DeliveryDays,
                        ValidityDays = input.ValidityDays,
                        OfferDate = DateTime.UtcNow,
                        Status = "Beklemede",
                        SupplierNotes = input.SupplierNotes
                    };
                    _context.RequestOffers.Add(newOffer);
                    await _context.SaveChangesAsync(); // OfferID üretilmesi için

                    // Kalem bazlı teklifleri ekle
                    if (input.Items != null && input.Items.Any())
                    {
                        foreach (var it in input.Items)
                        {
                            if (it.UnitPrice >= 0)
                            {
                                var reqItem = request.Items?.FirstOrDefault(ri => ri.RequestItemID == it.RequestItemID);
                                decimal qty = reqItem?.Quantity ?? 1;
                                _context.RequestOfferItems.Add(new RequestOfferItem
                                {
                                    OfferID = newOffer.OfferID,
                                    RequestItemID = it.RequestItemID,
                                    UnitPrice = it.UnitPrice,
                                    LineTotal = it.UnitPrice * qty,
                                    Notes = it.Notes?.Trim()
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    if (request.Status == "Beklemede")
                    {
                        request.Status = "Teklif Alındı";
                    }

                    await _context.SaveChangesAsync();
                    await TriggerOfferReceivedEmailNotificationAsync(request, currentCompany, input, calculatedTotalPrice);
                    await BroadcastOfferSubmittedAsync(request, newOffer, currentCompany, false, null);

                    return Json(new { success = true, message = $"{calculatedTotalPrice:N2} ₺ tutarındaki teklifiniz başarıyla iletildi!" });
                }
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Hata: " + detail });
            }
        }

        private async Task BroadcastRequestCreatedAsync(Requests newRequest)
        {
            try
            {
                var itemsDto = newRequest.Items.OrderBy(i => i.ItemOrder).Select(i => new
                {
                    requestItemId = i.RequestItemID,
                    itemName = i.ItemName,
                    quantity = i.Quantity,
                    unit = i.Unit,
                    estimatedUnitPrice = i.EstimatedUnitPrice,
                    itemDescription = i.ItemDescription
                }).ToList();

                int itemCount = itemsDto.Count > 0 ? itemsDto.Count : 1;
                int totalQuantity = newRequest.Quantity ?? (itemsDto.Count > 0 ? (int)Math.Round(itemsDto.Sum(i => i.quantity)) : 1);

                await _hubContext.Clients.All.SendAsync("OnRequestCreated", new
                {
                    requestId = newRequest.RequestID,
                    requestCode = $"REQ-{newRequest.RequestID:D4}",
                    productName = newRequest.ProductName ?? "Satın Alma Talebi",
                    category = newRequest.RequestType ?? "Satın Alma",
                    quantity = totalQuantity,
                    itemCount = itemCount,
                    items = itemsDto,
                    estimatedBudget = newRequest.EstimatedBudget,
                    estimatedBudgetFormatted = newRequest.EstimatedBudget.HasValue ? newRequest.EstimatedBudget.Value.ToString("N2") + " ₺" : "Belirtilmedi",
                    senderId = newRequest.SenderID,
                    buyerInfo = newRequest.BuyerInfo ?? "Kurumsal Alıcı",
                    status = newRequest.Status ?? "Beklemede",
                    requestDate = newRequest.RequestDate?.ToString("dd.MM.yyyy HH:mm") ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    targetDeliveryDate = newRequest.TargetDeliveryDate?.ToString("dd.MM.yyyy") ?? "Belirtilmedi",
                    notes = newRequest.Notes ?? "",
                    offerCount = 0
                });
            }
            catch (Exception hubEx)
            {
                Console.WriteLine($"SignalR BroadcastRequestCreated error: {hubEx.Message}");
            }
        }

        private async Task BroadcastOfferSubmittedAsync(Requests request, RequestOffers offer, Company? supplier, bool isRevised, decimal? prevPrice)
        {
            try
            {
                var supplierName = supplier?.CompanyName ?? supplier?.CompanyOfficial ?? $"Tedarikçi #{offer.SupplierID}";
                var parsed = ParseSupplierNotes(offer.SupplierNotes);

                var payload = new
                {
                    requestId = request.RequestID,
                    requestCode = $"REQ-{request.RequestID:D4}",
                    productName = request.ProductName ?? "Satın Alma Talebi",
                    offerId = offer.OfferID,
                    supplierId = offer.SupplierID,
                    supplierName = supplierName,
                    supplierOfficial = supplier?.CompanyOfficial ?? "",
                    supplierPhone = supplier?.CompanyPhone ?? "",
                    supplierMail = supplier?.CompanyMail ?? "",
                    offeredPrice = offer.OfferedPrice,
                    offeredPriceFormatted = offer.OfferedPrice.ToString("N2") + " ₺",
                    deliveryDays = offer.DeliveryDays,
                    validityDays = offer.ValidityDays,
                    offerDate = offer.OfferDate.ToString("dd.MM.yyyy HH:mm"),
                    status = offer.Status ?? "Beklemede",
                    supplierNotes = parsed.CleanNotes ?? "",
                    isRevised = isRevised,
                    previousPrice = prevPrice,
                    previousPriceFormatted = prevPrice.HasValue ? prevPrice.Value.ToString("N2") + " ₺" : null,
                    totalOffersCount = request.Offers.Count,
                    bestOfferPrice = request.Offers.Any() ? request.Offers.Min(o => o.OfferedPrice) : offer.OfferedPrice
                };

                // 1. Talebin canlı modalını izleyen gruba ve tüm portal kullanıcılarına gönder
                await _hubContext.Clients.Group($"request_{request.RequestID}").SendAsync("OnOfferSubmitted", payload);
                await _hubContext.Clients.All.SendAsync("OnOfferSubmitted", payload);

                // 2. Talep sahibi alıcı firmanın grubuna özel bildirim gönder
                if (request.SenderID.HasValue && request.SenderID.Value > 0)
                {
                    await _hubContext.Clients.Group($"company_{request.SenderID.Value}").SendAsync("OnOfferReceivedNotification", new
                    {
                        requestId = request.RequestID,
                        productName = request.ProductName,
                        supplierName = supplierName,
                        offeredPrice = offer.OfferedPrice,
                        offeredPriceFormatted = offer.OfferedPrice.ToString("N2") + " ₺",
                        isRevised = isRevised
                    });
                }

                // 3. Genel listeleri ve sayaçları güncellemek için genel yayın
                await _hubContext.Clients.All.SendAsync("OnRequestOfferCountUpdated", new
                {
                    requestId = request.RequestID,
                    offerCount = request.Offers.Count,
                    bestOfferPrice = request.Offers.Any() ? request.Offers.Min(o => o.OfferedPrice) : offer.OfferedPrice,
                    bestOfferPriceFormatted = request.Offers.Any() ? request.Offers.Min(o => o.OfferedPrice).ToString("N2") + " ₺" : "—",
                    status = request.Status
                });
            }
            catch (Exception hubEx)
            {
                Console.WriteLine($"SignalR BroadcastOfferSubmitted error: {hubEx.Message}");
            }
        }

        private async Task BroadcastOfferAcceptedAsync(Requests request, RequestOffers winningOffer, List<RequestOffers> rejectedOffers)
        {
            try
            {
                var winSupName = winningOffer.Supplier?.CompanyName ?? winningOffer.Supplier?.CompanyOfficial ?? $"Tedarikçi #{winningOffer.SupplierID}";

                // 1. Kazanan tedarikçiye doğrudan bildirim
                await _hubContext.Clients.Group($"company_{winningOffer.SupplierID}").SendAsync("OnOfferWonNotification", new
                {
                    requestId = request.RequestID,
                    requestCode = $"REQ-{request.RequestID:D4}",
                    productName = request.ProductName,
                    offeredPrice = winningOffer.OfferedPrice,
                    offeredPriceFormatted = winningOffer.OfferedPrice.ToString("N2") + " ₺",
                    message = $"🎉 Tebrikler! '{request.ProductName}' (#REQ-{request.RequestID:D4}) talebine verdiğiniz {winningOffer.OfferedPrice:N2} ₺ tutarındaki teklifiniz KABUL EDİLDİ!"
                });

                // 2. Elenen tedarikçilere red bildirimi
                foreach (var rej in rejectedOffers)
                {
                    await _hubContext.Clients.Group($"company_{rej.SupplierID}").SendAsync("OnOfferLostNotification", new
                    {
                        requestId = request.RequestID,
                        requestCode = $"REQ-{request.RequestID:D4}",
                        productName = request.ProductName,
                        rejectionReason = "Talep için başka bir tedarikçinin teklifi kabul edildi."
                    });
                }

                // 3. İlgili talep modal grubuna ve genel listelere yayın
                var payload = new
                {
                    requestId = request.RequestID,
                    winningOfferId = winningOffer.OfferID,
                    winningSupplierId = winningOffer.SupplierID,
                    winningSupplierName = winSupName,
                    status = "Onaylandı",
                    approvedDate = request.ApprovedDate?.ToString("dd.MM.yyyy HH:mm") ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                };

                await _hubContext.Clients.Group($"request_{request.RequestID}").SendAsync("OnRequestApproved", payload);
                await _hubContext.Clients.All.SendAsync("OnRequestApproved", payload);
                await _hubContext.Clients.All.SendAsync("OnRequestStatusChanged", payload);
            }
            catch (Exception hubEx)
            {
                Console.WriteLine($"SignalR BroadcastOfferAccepted error: {hubEx.Message}");
            }
        }

        private async Task BroadcastOfferRejectedAsync(Requests request, RequestOffers rejectedOffer, string reason)
        {
            try
            {
                var payload = new
                {
                    requestId = request.RequestID,
                    offerId = rejectedOffer.OfferID,
                    supplierId = rejectedOffer.SupplierID,
                    rejectionReason = reason,
                    status = "Reddedildi"
                };

                await _hubContext.Clients.Group($"company_{rejectedOffer.SupplierID}").SendAsync("OnOfferLostNotification", new
                {
                    requestId = request.RequestID,
                    requestCode = $"REQ-{request.RequestID:D4}",
                    productName = request.ProductName,
                    rejectionReason = reason
                });

                await _hubContext.Clients.Group($"request_{request.RequestID}").SendAsync("OnOfferRejected", payload);
                await _hubContext.Clients.All.SendAsync("OnOfferRejected", payload);
            }
            catch (Exception hubEx)
            {
                Console.WriteLine($"SignalR BroadcastOfferRejected error: {hubEx.Message}");
            }
        }

        /// <summary>
        /// Talebi açan alıcı firmaya yeni teklif geldiğinde otomatik zengin HTML e-postası tetikler (Kalem listesi destekli).
        /// </summary>
        private async Task TriggerOfferReceivedEmailNotificationAsync(
            Requests request,
            Company? currentCompany,
            SubmitOfferInputModel input,
            decimal? finalTotalPrice = null)
        {
            try
            {
                string targetEmail = "";
                string buyerName = request.BuyerInfo ?? "Kurumsal Alıcı";

                // 1. SenderID üzerinden alıcı firma kaydını ve e-postasını bul
                if (request.SenderID.HasValue && request.SenderID.Value > 0)
                {
                    var buyerCompany = await _context.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CompanyID == request.SenderID.Value);

                    if (buyerCompany != null)
                    {
                        if (!string.IsNullOrWhiteSpace(buyerCompany.CompanyMail))
                        {
                            targetEmail = buyerCompany.CompanyMail.Trim();
                        }
                        if (!string.IsNullOrWhiteSpace(buyerCompany.CompanyName))
                        {
                            buyerName = buyerCompany.CompanyName.Trim();
                        }
                        else if (!string.IsNullOrWhiteSpace(buyerCompany.CompanyOfficial))
                        {
                            buyerName = buyerCompany.CompanyOfficial.Trim();
                        }
                    }
                }

                // 2. SenderID'den bulunamadıysa BuyerInfo metnindeki e-posta veya unvan eşleşmesine bak
                if (string.IsNullOrWhiteSpace(targetEmail) && !string.IsNullOrWhiteSpace(request.BuyerInfo))
                {
                    var emailCandidate = request.BuyerInfo
                        .Split(new[] { ' ', ';', ',', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(s => s.Contains("@"));

                    if (!string.IsNullOrWhiteSpace(emailCandidate))
                    {
                        targetEmail = emailCandidate.Trim();
                    }
                    else
                    {
                        var buyerNameTrimmed = request.BuyerInfo.Trim().ToLower();
                        var matchedCompany = await _context.Companies
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CompanyName != null && c.CompanyName.ToLower() == buyerNameTrimmed);

                        if (matchedCompany != null && !string.IsNullOrWhiteSpace(matchedCompany.CompanyMail))
                        {
                            targetEmail = matchedCompany.CompanyMail.Trim();
                            if (!string.IsNullOrWhiteSpace(matchedCompany.CompanyName))
                            {
                                buyerName = matchedCompany.CompanyName.Trim();
                            }
                        }
                    }
                }

                // 3. Kalem listesini hazırla
                var emailItems = new List<OfferItemEmailDto>();
                var reqItems = request.Items ?? await _context.RequestItems
                    .Where(ri => ri.RequestID == request.RequestID)
                    .OrderBy(ri => ri.ItemOrder)
                    .ToListAsync();

                if (input.Items != null && input.Items.Any(i => i.UnitPrice >= 0))
                {
                    int order = 1;
                    foreach (var it in input.Items)
                    {
                        var reqItem = reqItems?.FirstOrDefault(ri => ri.RequestItemID == it.RequestItemID);
                        decimal qty = reqItem?.Quantity ?? 1;
                        string unit = reqItem?.Unit ?? "Adet";
                        string name = reqItem?.ItemName ?? $"Kalem #{order}";
                        string? desc = reqItem?.ItemDescription;
                        decimal unitPrice = it.UnitPrice;
                        decimal lineTotal = unitPrice * qty;

                        emailItems.Add(new OfferItemEmailDto
                        {
                            ItemOrder = order++,
                            ItemName = name,
                            Quantity = qty,
                            Unit = unit,
                            UnitPrice = unitPrice,
                            LineTotal = lineTotal,
                            ItemDescription = desc,
                            Notes = it.Notes
                        });
                    }
                }
                else if (reqItems != null && reqItems.Any())
                {
                    int order = 1;
                    decimal totalInputPrice = finalTotalPrice ?? input.OfferedPrice;
                    decimal totalQty = reqItems.Sum(ri => ri.Quantity);
                    foreach (var ri in reqItems)
                    {
                        decimal proportion = totalQty > 0 ? (ri.Quantity / totalQty) : (1m / reqItems.Count);
                        decimal lineTot = Math.Round(totalInputPrice * proportion, 2);
                        decimal uPrice = ri.Quantity > 0 ? Math.Round(lineTot / ri.Quantity, 2) : lineTot;

                        emailItems.Add(new OfferItemEmailDto
                        {
                            ItemOrder = order++,
                            ItemName = ri.ItemName,
                            Quantity = ri.Quantity,
                            Unit = ri.Unit ?? "Adet",
                            UnitPrice = uPrice,
                            LineTotal = lineTot,
                            ItemDescription = ri.ItemDescription,
                            Notes = null
                        });
                    }
                }

                // 4. Hedef alıcı e-posta adresi mevcutsa arka planda MailKit ile gönder
                if (!string.IsNullOrWhiteSpace(targetEmail))
                {
                    string supplierName = currentCompany?.CompanyName 
                        ?? currentCompany?.CompanyOfficial 
                        ?? GetCurrentCompanyName();

                    string requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=incoming-offers&requestId={request.RequestID}";
                    string rfqCode = $"REQ-{request.RequestID:D4}";
                    string prodName = request.ProductName ?? (reqItems != null && reqItems.Any() ? string.Join(", ", reqItems.Take(2).Select(i => i.ItemName)) + (reqItems.Count > 2 ? $" (+{reqItems.Count - 2} kalem)" : "") : "Satın Alma Talebi");
                    int qty = request.Quantity ?? (reqItems != null && reqItems.Any() ? (int)Math.Round(reqItems.Sum(i => i.Quantity)) : 1);
                    decimal offerPrice = finalTotalPrice ?? (emailItems.Any() ? emailItems.Sum(i => i.LineTotal) : input.OfferedPrice);
                    int deliveryDays = input.DeliveryDays;
                    int validityDays = input.ValidityDays;
                    string? notes = input.SupplierNotes;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendOfferReceivedEmailAsync(
                                toEmail: targetEmail,
                                buyerName: buyerName,
                                supplierName: supplierName,
                                rfqCode: rfqCode,
                                productName: prodName,
                                productImageUrl: null,
                                quantity: qty,
                                unit: "Adet",
                                offerPrice: offerPrice,
                                deliveryDays: deliveryDays,
                                validityDays: validityDays,
                                offerNotes: notes,
                                requestsUrl: requestsUrl,
                                items: emailItems.Any() ? emailItems : null);
                        }
                        catch (Exception mailEx)
                        {
                            Console.WriteLine($"Teklif e-posta arka plan gönderim hatası: {mailEx.Message}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[E-posta Bildirimi] Talep #{request.RequestID} için alıcı e-posta adresi tespit edilemedi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Teklif e-posta hazırlık hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Tedarikçi notları içerisinden sistem red gerekçesini ve revizyon durumunu ayrıştırır.
        /// </summary>
        public static (string? CleanNotes, string? RejectionReason, bool IsRevised, decimal? PreviousPrice, string? PreviousRejectionReason) ParseSupplierNotes(string? rawNotes)
        {
            if (string.IsNullOrWhiteSpace(rawNotes))
                return (null, null, false, null, null);

            string notes = rawNotes;
            string? rejectionReason = null;
            bool isRevised = false;
            decimal? prevPrice = null;
            string? prevRejectionReason = null;

            // 1. Red Gerekçesi Ayrıştırma: [Red Gerekçesi: ...]
            const string redTag = "[Red Gerekçesi:";
            int redIdx = notes.IndexOf(redTag, StringComparison.OrdinalIgnoreCase);
            if (redIdx >= 0)
            {
                int endIdx = notes.IndexOf(']', redIdx);
                if (endIdx > redIdx)
                {
                    int start = redIdx + redTag.Length;
                    rejectionReason = notes.Substring(start, endIdx - start).Trim();
                    notes = notes.Remove(redIdx, (endIdx - redIdx) + 1);
                }
            }

            // 2. Revizyon Bilgisi Ayrıştırma: [Revize:EskiFiyat=...|EskiRed=...]
            const string revTag = "[Revize:";
            int revIdx = notes.IndexOf(revTag, StringComparison.OrdinalIgnoreCase);
            if (revIdx >= 0)
            {
                int endIdx = notes.IndexOf(']', revIdx);
                if (endIdx > revIdx)
                {
                    isRevised = true;
                    int start = revIdx + revTag.Length;
                    string content = notes.Substring(start, endIdx - start);

                    var parts = content.Split('|');
                    foreach (var p in parts)
                    {
                        var kv = p.Split('=');
                        if (kv.Length == 2)
                        {
                            var key = kv[0].Trim();
                            var val = kv[1].Trim();
                            if (key.Equals("EskiFiyat", StringComparison.OrdinalIgnoreCase))
                            {
                                if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal ep))
                                {
                                    prevPrice = ep;
                                }
                            }
                            else if (key.Equals("EskiRed", StringComparison.OrdinalIgnoreCase))
                            {
                                prevRejectionReason = val;
                            }
                        }
                    }
                    notes = notes.Remove(revIdx, (endIdx - revIdx) + 1);
                }
            }

            // Not metnini temizle
            string clean = notes.Replace("|", " ").Trim();
            while (clean.Contains("  "))
            {
                clean = clean.Replace("  ", " ");
            }

            return (string.IsNullOrWhiteSpace(clean) ? null : clean, rejectionReason, isRevised, prevPrice, prevRejectionReason);
        }

        /// <summary>
        /// Bir teklif kabul edildiğinde kazanan tedarikçiye onay e-postası, elenen tedarikçilere ise red gerekçesi e-postasını arka planda gönderir.
        /// </summary>
        private void TriggerOfferDecisionEmails(
            Requests request,
            RequestOffers winningOffer,
            List<RequestOffers> rejectedOffers,
            Company? buyerCompany)
        {
            try
            {
                string buyerName = buyerCompany?.CompanyName ?? buyerCompany?.CompanyOfficial ?? request.BuyerInfo ?? "Kurumsal Alıcı";
                string rfqCode = $"REQ-{request.RequestID:D4}";
                string prodName = request.ProductName ?? "Satın Alma Talebi";
                int qty = request.Quantity ?? 1;
                string requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=my-bids&requestId={request.RequestID}";
                string ordersUrl = $"{Request.Scheme}://{Request.Host}/Dashboard/SupplierOrders";

                // Kalemleri hazırla
                var reqItems = request.Items ?? _context.RequestItems
                    .Where(ri => ri.RequestID == request.RequestID)
                    .OrderBy(ri => ri.ItemOrder)
                    .ToList();

                var winOfferItems = winningOffer.OfferItems ?? _context.RequestOfferItems
                    .Where(oi => oi.OfferID == winningOffer.OfferID)
                    .ToList();

                var winEmailItems = new List<OfferItemEmailDto>();
                int wOrder = 1;
                if (winOfferItems.Any())
                {
                    foreach (var oi in winOfferItems)
                    {
                        var reqItem = reqItems.FirstOrDefault(ri => ri.RequestItemID == oi.RequestItemID);
                        decimal itemQty = reqItem?.Quantity ?? 1;
                        string itemUnit = reqItem?.Unit ?? "Adet";
                        string itemName = reqItem?.ItemName ?? $"Kalem #{wOrder}";
                        string? itemDesc = reqItem?.ItemDescription;

                        winEmailItems.Add(new OfferItemEmailDto
                        {
                            ItemOrder = wOrder++,
                            ItemName = itemName,
                            Quantity = itemQty,
                            Unit = itemUnit,
                            UnitPrice = oi.UnitPrice,
                            LineTotal = oi.LineTotal > 0 ? oi.LineTotal : (oi.UnitPrice * itemQty),
                            ItemDescription = itemDesc,
                            Notes = oi.Notes
                        });
                    }
                }
                else if (reqItems.Any())
                {
                    decimal totalInputPrice = winningOffer.OfferedPrice;
                    decimal totalQty = reqItems.Sum(ri => ri.Quantity);
                    foreach (var ri in reqItems)
                    {
                        decimal proportion = totalQty > 0 ? (ri.Quantity / totalQty) : (1m / reqItems.Count);
                        decimal lineTot = Math.Round(totalInputPrice * proportion, 2);
                        decimal uPrice = ri.Quantity > 0 ? Math.Round(lineTot / ri.Quantity, 2) : lineTot;

                        winEmailItems.Add(new OfferItemEmailDto
                        {
                            ItemOrder = wOrder++,
                            ItemName = ri.ItemName,
                            Quantity = ri.Quantity,
                            Unit = ri.Unit ?? "Adet",
                            UnitPrice = uPrice,
                            LineTotal = lineTot,
                            ItemDescription = ri.ItemDescription,
                            Notes = null
                        });
                    }
                }

                // 1. Kazanan tedarikçiye kabul e-postası
                if (winningOffer.Supplier != null && !string.IsNullOrWhiteSpace(winningOffer.Supplier.CompanyMail))
                {
                    var winEmail = winningOffer.Supplier.CompanyMail.Trim();
                    var winName = winningOffer.Supplier.CompanyName ?? winningOffer.Supplier.CompanyOfficial ?? "Tedarikçi Firma";
                    var winPrice = winningOffer.OfferedPrice;
                    var winDelivery = winningOffer.DeliveryDays;
                    var winValidity = winningOffer.ValidityDays;
                    var (cleanWinNotes, _, _, _, _) = ParseSupplierNotes(winningOffer.SupplierNotes);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendOfferAcceptedEmailAsync(
                                toEmail: winEmail,
                                supplierName: winName,
                                buyerName: buyerName,
                                rfqCode: rfqCode,
                                productName: prodName,
                                quantity: qty,
                                unit: "Adet",
                                offeredPrice: winPrice,
                                deliveryDays: winDelivery,
                                requestsUrl: requestsUrl,
                                items: winEmailItems.Any() ? winEmailItems : null,
                                validityDays: winValidity,
                                supplierNotes: cleanWinNotes,
                                ordersUrl: ordersUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Kabul e-posta arka plan gönderim hatası: {ex.Message}");
                        }
                    });
                }

                // 2. Elenen tedarikçilere red gerekçesi ve revizyon imkanı içeren e-posta
                foreach (var rej in rejectedOffers)
                {
                    if (rej.Supplier != null && !string.IsNullOrWhiteSpace(rej.Supplier.CompanyMail))
                    {
                        var rejEmail = rej.Supplier.CompanyMail.Trim();
                        var rejName = rej.Supplier.CompanyName ?? rej.Supplier.CompanyOfficial ?? "Tedarikçi Firma";
                        var rejPrice = rej.OfferedPrice;
                        var rejDelivery = rej.DeliveryDays;
                        var rejValidity = rej.ValidityDays;
                        var (cleanRejNotes, reason, _, _, _) = ParseSupplierNotes(rej.SupplierNotes);
                        var finalReason = reason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.";
                        string rejRequestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=my-bids&requestId={request.RequestID}";
                        string rejReviseUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=my-bids&requestId={request.RequestID}&open=bid";

                        var rejOfferItems = rej.OfferItems ?? _context.RequestOfferItems
                            .Where(oi => oi.OfferID == rej.OfferID)
                            .ToList();

                        var rejEmailItems = new List<OfferItemEmailDto>();
                        int rOrder = 1;
                        if (rejOfferItems.Any())
                        {
                            foreach (var oi in rejOfferItems)
                            {
                                var reqItem = reqItems.FirstOrDefault(ri => ri.RequestItemID == oi.RequestItemID);
                                decimal itemQty = reqItem?.Quantity ?? 1;
                                string itemUnit = reqItem?.Unit ?? "Adet";
                                string itemName = reqItem?.ItemName ?? $"Kalem #{rOrder}";

                                rejEmailItems.Add(new OfferItemEmailDto
                                {
                                    ItemOrder = rOrder++,
                                    ItemName = itemName,
                                    Quantity = itemQty,
                                    Unit = itemUnit,
                                    UnitPrice = oi.UnitPrice,
                                    LineTotal = oi.LineTotal > 0 ? oi.LineTotal : (oi.UnitPrice * itemQty),
                                    Notes = oi.Notes
                                });
                            }
                        }
                        else if (reqItems.Any())
                        {
                            decimal totalRejPrice = rej.OfferedPrice;
                            decimal totalQty = reqItems.Sum(ri => ri.Quantity);
                            foreach (var ri in reqItems)
                            {
                                decimal proportion = totalQty > 0 ? (ri.Quantity / totalQty) : (1m / reqItems.Count);
                                decimal lineTot = Math.Round(totalRejPrice * proportion, 2);
                                decimal uPrice = ri.Quantity > 0 ? Math.Round(lineTot / ri.Quantity, 2) : lineTot;

                                rejEmailItems.Add(new OfferItemEmailDto
                                {
                                    ItemOrder = rOrder++,
                                    ItemName = ri.ItemName,
                                    Quantity = ri.Quantity,
                                    Unit = ri.Unit ?? "Adet",
                                    UnitPrice = uPrice,
                                    LineTotal = lineTot,
                                    ItemDescription = ri.ItemDescription,
                                    Notes = null
                                });
                            }
                        }

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendOfferRejectedEmailAsync(
                                    toEmail: rejEmail,
                                    supplierName: rejName,
                                    buyerName: buyerName,
                                    rfqCode: rfqCode,
                                    productName: prodName,
                                    quantity: qty,
                                    unit: "Adet",
                                    offeredPrice: rejPrice,
                                    rejectionReason: finalReason,
                                    requestsUrl: rejRequestsUrl,
                                    items: rejEmailItems.Any() ? rejEmailItems : null,
                                    deliveryDays: rejDelivery,
                                    validityDays: rejValidity,
                                    supplierNotes: cleanRejNotes,
                                    reviseUrl: rejReviseUrl);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Red e-posta arka plan gönderim hatası: {ex.Message}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Karar e-postaları tetikleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Tekil bir teklif reddedildiğinde ilgili tedarikçiye arka planda teklif dökümünü ve revize etme bağlantısını içeren red e-postası gönderir.
        /// </summary>
        private void TriggerSingleOfferRejectedEmail(
            Requests request,
            RequestOffers rejectedOffer,
            string reason,
            Company? buyerCompany)
        {
            try
            {
                if (rejectedOffer.Supplier != null && !string.IsNullOrWhiteSpace(rejectedOffer.Supplier.CompanyMail))
                {
                    string buyerName = buyerCompany?.CompanyName ?? buyerCompany?.CompanyOfficial ?? request.BuyerInfo ?? "Kurumsal Alıcı";
                    string rfqCode = $"REQ-{request.RequestID:D4}";
                    string prodName = request.ProductName ?? "Satın Alma Talebi";
                    int qty = request.Quantity ?? 1;
                    string requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=my-bids&requestId={request.RequestID}";
                    string reviseUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=my-bids&requestId={request.RequestID}&open=bid";
                    var rejEmail = rejectedOffer.Supplier.CompanyMail.Trim();
                    var rejName = rejectedOffer.Supplier.CompanyName ?? rejectedOffer.Supplier.CompanyOfficial ?? "Tedarikçi Firma";
                    var finalReason = !string.IsNullOrWhiteSpace(reason) ? reason.Trim() : "Alıcı firma tarafından doğrudan reddedildi.";
                    var (cleanNotes, _, _, _, _) = ParseSupplierNotes(rejectedOffer.SupplierNotes);

                    var reqItems = request.Items ?? _context.RequestItems
                        .Where(ri => ri.RequestID == request.RequestID)
                        .OrderBy(ri => ri.ItemOrder)
                        .ToList();

                    var rejOfferItems = rejectedOffer.OfferItems ?? _context.RequestOfferItems
                        .Where(oi => oi.OfferID == rejectedOffer.OfferID)
                        .ToList();

                    var rejEmailItems = new List<OfferItemEmailDto>();
                    int rOrder = 1;
                    if (rejOfferItems.Any())
                    {
                        foreach (var oi in rejOfferItems)
                        {
                            var reqItem = reqItems.FirstOrDefault(ri => ri.RequestItemID == oi.RequestItemID);
                            decimal itemQty = reqItem?.Quantity ?? 1;
                            string itemUnit = reqItem?.Unit ?? "Adet";
                            string itemName = reqItem?.ItemName ?? $"Kalem #{rOrder}";

                            rejEmailItems.Add(new OfferItemEmailDto
                            {
                                ItemOrder = rOrder++,
                                ItemName = itemName,
                                Quantity = itemQty,
                                Unit = itemUnit,
                                UnitPrice = oi.UnitPrice,
                                LineTotal = oi.LineTotal > 0 ? oi.LineTotal : (oi.UnitPrice * itemQty),
                                Notes = oi.Notes
                            });
                        }
                    }
                    else if (reqItems.Any())
                    {
                        decimal totalRejPrice = rejectedOffer.OfferedPrice;
                        decimal totalQty = reqItems.Sum(ri => ri.Quantity);
                        foreach (var ri in reqItems)
                        {
                            decimal proportion = totalQty > 0 ? (ri.Quantity / totalQty) : (1m / reqItems.Count);
                            decimal lineTot = Math.Round(totalRejPrice * proportion, 2);
                            decimal uPrice = ri.Quantity > 0 ? Math.Round(lineTot / ri.Quantity, 2) : lineTot;

                            rejEmailItems.Add(new OfferItemEmailDto
                            {
                                ItemOrder = rOrder++,
                                ItemName = ri.ItemName,
                                Quantity = ri.Quantity,
                                Unit = ri.Unit ?? "Adet",
                                UnitPrice = uPrice,
                                LineTotal = lineTot,
                                ItemDescription = ri.ItemDescription,
                                Notes = null
                            });
                        }
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendOfferRejectedEmailAsync(
                                toEmail: rejEmail,
                                supplierName: rejName,
                                buyerName: buyerName,
                                rfqCode: rfqCode,
                                productName: prodName,
                                quantity: qty,
                                unit: "Adet",
                                offeredPrice: rejectedOffer.OfferedPrice,
                                rejectionReason: finalReason,
                                requestsUrl: requestsUrl,
                                items: rejEmailItems.Any() ? rejEmailItems : null,
                                deliveryDays: rejectedOffer.DeliveryDays,
                                validityDays: rejectedOffer.ValidityDays,
                                supplierNotes: cleanNotes,
                                reviseUrl: reviseUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Tekil red e-posta gönderim hatası: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tekil red e-posta tetikleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Alıcının bir tedarikçi teklifini KABUL ETMESİ / ONAYLAMASI (Form POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(int offerId)
        {
            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                if (currentCompanyId <= 0)
                {
                    TempData["ErrorMessage"] = "Teklifi onaylamak için giriş yapmış olmanız gerekir.";
                    return RedirectToAction("Login", "Account");
                }

                var currentCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);

                var offer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .Include(o => o.Supplier)
                    .Include(o => o.Request!).ThenInclude(r => r.Items)
                    .FirstOrDefaultAsync(o => o.OfferID == offerId);

                if (offer == null || offer.Request == null)
                {
                    TempData["ErrorMessage"] = "Kabul edilmek istenen teklif bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                var request = offer.Request;

                // 7 Katmanlı Sahiplik / Yetki Kontrolü
                if (!IsRequestOwnedByCurrentCompany(request, currentCompanyId, currentCompany))
                {
                    TempData["ErrorMessage"] = "Bu talebin teklifini onaylama yetkiniz bulunmuyor. Yalnızca talebi oluşturan alıcı firma bu işlemi yapabilir.";
                    return RedirectToAction(nameof(Index));
                }

                // Seçilen teklifi kabul et
                offer.Status = "Kabul Edildi";

                // Talebi onayla ve kazanan tedarikçiyle eşleştir
                request.Status = "Onaylandı";
                request.RecieverID = offer.SupplierID;
                request.ApprovedDate = DateTime.UtcNow;
                request.ReceiverName = offer.Supplier?.CompanyName ?? offer.Supplier?.CompanyOfficial ?? $"Tedarikçi #{offer.SupplierID}";

                // Varsa aynı talebe ait diğer teklifleri 'Reddedildi' yap ve şeffaf red gerekçesi ekle
                var otherOffers = await _context.RequestOffers
                    .Include(o => o.Supplier)
                    .Include(o => o.OfferItems)
                    .Where(o => o.RequestID == request.RequestID && o.OfferID != offerId)
                    .ToListAsync();

                const string autoRejectReason = "Talep için başka bir tedarikçinin teklifi kabul edildi.";
                foreach (var other in otherOffers)
                {
                    other.Status = "Reddedildi";
                    if (string.IsNullOrWhiteSpace(other.SupplierNotes))
                    {
                        other.SupplierNotes = $"[Red Gerekçesi: {autoRejectReason}]";
                    }
                    else if (!other.SupplierNotes.Contains("[Red Gerekçesi:"))
                    {
                        other.SupplierNotes = $"{other.SupplierNotes} | [Red Gerekçesi: {autoRejectReason}]";
                    }
                }

                await _context.SaveChangesAsync();

                // E-posta bildirimlerini tetikle
                TriggerOfferDecisionEmails(request, offer, otherOffers, currentCompany);

                // SignalR ile anlık ekran güncellemesi ve bildirim gönder
                await BroadcastOfferAcceptedAsync(request, offer, otherOffers);

                TempData["SuccessMessage"] = $"{offer.Supplier?.CompanyName ?? "Tedarikçi"} firmasının {offer.OfferedPrice:N2} ₺ tutarındaki teklifi onaylandı ve talep sonuçlandı!";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Teklif kabul edilirken hata oluştu: " + detail;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Modal içerisinden AJAX ile Teklif Kabul Etme Endpoint'i
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AcceptOfferAjax([FromBody] AcceptOfferInputModel input)
        {
            if (input == null || input.OfferId <= 0)
            {
                return Json(new { success = false, message = "Geçersiz teklif referansı." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                if (currentCompanyId <= 0)
                {
                    return Json(new { success = false, message = "Oturum süreniz dolmuş, lütfen tekrar giriş yapın." });
                }

                var currentCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);

                var offer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .Include(o => o.Supplier)
                    .Include(o => o.Request!).ThenInclude(r => r.Items)
                    .FirstOrDefaultAsync(o => o.OfferID == input.OfferId);

                if (offer == null || offer.Request == null)
                {
                    return Json(new { success = false, message = "Teklif bulunamadı." });
                }

                var request = offer.Request;

                if (!IsRequestOwnedByCurrentCompany(request, currentCompanyId, currentCompany))
                {
                    return Json(new { success = false, message = "Bu teklifi kabul etme yetkiniz bulunmuyor." });
                }

                offer.Status = "Kabul Edildi";
                request.Status = "Onaylandı";
                request.RecieverID = offer.SupplierID;
                request.ApprovedDate = DateTime.UtcNow;
                request.ReceiverName = offer.Supplier?.CompanyName ?? offer.Supplier?.CompanyOfficial ?? $"Tedarikçi #{offer.SupplierID}";

                var otherOffers = await _context.RequestOffers
                    .Include(o => o.Supplier)
                    .Include(o => o.OfferItems)
                    .Where(o => o.RequestID == request.RequestID && o.OfferID != input.OfferId)
                    .ToListAsync();

                const string autoRejectReason = "Talep için başka bir tedarikçinin teklifi kabul edildi.";
                foreach (var other in otherOffers)
                {
                    other.Status = "Reddedildi";
                    if (string.IsNullOrWhiteSpace(other.SupplierNotes))
                    {
                        other.SupplierNotes = $"[Red Gerekçesi: {autoRejectReason}]";
                    }
                    else if (!other.SupplierNotes.Contains("[Red Gerekçesi:"))
                    {
                        other.SupplierNotes = $"{other.SupplierNotes} | [Red Gerekçesi: {autoRejectReason}]";
                    }
                }

                await _context.SaveChangesAsync();

                // E-posta bildirimlerini tetikle
                TriggerOfferDecisionEmails(request, offer, otherOffers, currentCompany);

                // SignalR ile anlık ekran güncellemesi ve bildirim gönder
                await BroadcastOfferAcceptedAsync(request, offer, otherOffers);

                return Json(new
                {
                    success = true,
                    requestId = request.RequestID,
                    message = $"{offer.Supplier?.CompanyName ?? "Tedarikçi"} firmasının {offer.OfferedPrice:N2} ₺ tutarındaki teklifi kabul edildi ve talep ONAYLANDI!"
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Onaylama hatası: " + detail });
            }
        }

        /// <summary>
        /// Alıcının bir teklifi REDDETMESİ (Form POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOffer(int offerId, string? reason)
        {
            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                if (currentCompanyId <= 0)
                {
                    TempData["ErrorMessage"] = "İşlem yapabilmek için giriş yapmalısınız.";
                    return RedirectToAction("Login", "Account");
                }

                var currentCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);

                var offer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .Include(o => o.Supplier)
                    .Include(o => o.Request!).ThenInclude(r => r.Items)
                    .FirstOrDefaultAsync(o => o.OfferID == offerId);

                if (offer == null || offer.Request == null)
                {
                    TempData["ErrorMessage"] = "Reddedilmek istenen teklif bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                if (!IsRequestOwnedByCurrentCompany(offer.Request, currentCompanyId, currentCompany))
                {
                    TempData["ErrorMessage"] = "Bu teklifi reddetme yetkiniz bulunmuyor.";
                    return RedirectToAction(nameof(Index));
                }

                string finalReason = !string.IsNullOrWhiteSpace(reason) ? reason.Trim() : "Alıcı firma tarafından doğrudan reddedildi.";

                offer.Status = "Reddedildi";
                offer.SupplierNotes = (string.IsNullOrWhiteSpace(offer.SupplierNotes) ? "" : offer.SupplierNotes + " | ") + $"[Red Gerekçesi: {finalReason}]";

                // Kalan teklifleri kontrol et
                var remainingOffers = await _context.RequestOffers
                    .Where(o => o.RequestID == offer.RequestID && o.OfferID != offerId)
                    .ToListAsync();

                if (!remainingOffers.Any(o => o.Status == "Kabul Edildi" || o.Status == "Beklemede"))
                {
                    offer.Request.Status = "Beklemede";
                }

                await _context.SaveChangesAsync();

                // Bildirim e-postası
                TriggerSingleOfferRejectedEmail(offer.Request, offer, finalReason, currentCompany);

                // SignalR ile anlık güncelleme
                await BroadcastOfferRejectedAsync(offer.Request, offer, finalReason);

                TempData["SuccessMessage"] = "Teklif başarıyla reddedildi.";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "İşlem sırasında hata oluştu: " + detail;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Modal içerisinden AJAX ile Teklif Reddetme Endpoint'i
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RejectOfferAjax([FromBody] RejectOfferInputModel input)
        {
            if (input == null || input.OfferId <= 0)
            {
                return Json(new { success = false, message = "Geçersiz teklif referansı." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                if (currentCompanyId <= 0)
                {
                    return Json(new { success = false, message = "Oturum süreniz dolmuş, lütfen tekrar giriş yapın." });
                }

                var currentCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);

                var offer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .Include(o => o.Supplier)
                    .Include(o => o.Request!).ThenInclude(r => r.Items)
                    .FirstOrDefaultAsync(o => o.OfferID == input.OfferId);

                if (offer == null || offer.Request == null)
                {
                    return Json(new { success = false, message = "Teklif bulunamadı." });
                }

                if (!IsRequestOwnedByCurrentCompany(offer.Request, currentCompanyId, currentCompany))
                {
                    return Json(new { success = false, message = "Bu teklifi reddetme yetkiniz bulunmuyor." });
                }

                string finalReason = !string.IsNullOrWhiteSpace(input.Reason) ? input.Reason.Trim() : "Alıcı firma tarafından doğrudan reddedildi.";

                offer.Status = "Reddedildi";
                offer.SupplierNotes = (string.IsNullOrWhiteSpace(offer.SupplierNotes) ? "" : offer.SupplierNotes + " | ") + $"[Red Gerekçesi: {finalReason}]";

                var remainingOffers = await _context.RequestOffers
                    .Where(o => o.RequestID == offer.RequestID && o.OfferID != input.OfferId)
                    .ToListAsync();

                if (!remainingOffers.Any(o => o.Status == "Kabul Edildi" || o.Status == "Beklemede"))
                {
                    offer.Request.Status = "Beklemede";
                }

                await _context.SaveChangesAsync();

                // Bildirim e-postası
                TriggerSingleOfferRejectedEmail(offer.Request, offer, finalReason, currentCompany);

                // SignalR ile anlık güncelleme
                await BroadcastOfferRejectedAsync(offer.Request, offer, finalReason);

                return Json(new
                {
                    success = true,
                    requestId = offer.RequestID,
                    message = "Teklif başarıyla reddedildi."
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Reddetme hatası: " + detail });
            }
        }

        /// <summary>
        /// Talep Detayları ve Gelen Teklifleri JSON olarak döndüren Endpoint (Modal için)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(string id)
        {
            try
            {
                int numericId = 0;
                if (!int.TryParse(id, out numericId))
                {
                    var digitsOnly = new string((id ?? "").Where(char.IsDigit).ToArray());
                    int.TryParse(digitsOnly, out numericId);
                }

                if (numericId <= 0)
                {
                    return Json(new { success = false, message = "Geçersiz talep numarası." });
                }

                int currentCompanyId = GetCurrentCompanyId();

                var req = await _context.Requests
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.OfferItems)
                    .Include(r => r.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RequestID == numericId);

                if (req == null)
                {
                    return Json(new { success = false, message = "Talep bulunamadı." });
                }

                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;
                bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);

                var itemsList = req.Items.OrderBy(i => i.ItemOrder).Select(i => new
                {
                    requestItemId = i.RequestItemID,
                    itemOrder = i.ItemOrder,
                    itemName = i.ItemName,
                    quantity = i.Quantity,
                    unit = i.Unit ?? "Adet",
                    estimatedUnitPrice = i.EstimatedUnitPrice,
                    estimatedUnitPriceFormatted = i.EstimatedUnitPrice.HasValue ? i.EstimatedUnitPrice.Value.ToString("N2") + " ₺" : null,
                    estimatedTotalPrice = i.EstimatedUnitPrice.HasValue ? (i.EstimatedUnitPrice.Value * i.Quantity) : (decimal?)null,
                    estimatedTotalPriceFormatted = i.EstimatedUnitPrice.HasValue ? (i.EstimatedUnitPrice.Value * i.Quantity).ToString("N2") + " ₺" : null,
                    itemDescription = i.ItemDescription ?? ""
                }).ToList();

                if (itemsList.Count == 0 && !string.IsNullOrWhiteSpace(req.ProductName))
                {
                    itemsList.Add(new
                    {
                        requestItemId = 0,
                        itemOrder = 1,
                        itemName = req.ProductName,
                        quantity = (decimal)(req.Quantity ?? 1),
                        unit = "Adet",
                        estimatedUnitPrice = req.EstimatedBudget.HasValue && req.Quantity > 0 ? (req.EstimatedBudget.Value / req.Quantity.Value) : (decimal?)null,
                        estimatedUnitPriceFormatted = req.EstimatedBudget.HasValue && req.Quantity > 0 ? (req.EstimatedBudget.Value / req.Quantity.Value).ToString("N2") + " ₺" : null,
                        estimatedTotalPrice = req.EstimatedBudget,
                        estimatedTotalPriceFormatted = req.EstimatedBudget.HasValue ? req.EstimatedBudget.Value.ToString("N2") + " ₺" : null,
                        itemDescription = req.Notes ?? ""
                    });
                }

                var offersList = req.Offers.Select(o =>
                {
                    var parsed = ParseSupplierNotes(o.SupplierNotes);
                    var fallbackReason = o.Status == "Reddedildi" ? (parsed.RejectionReason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.") : null;

                    var offerItemsList = (o.OfferItems ?? new List<RequestOfferItem>()).Select(oi => new
                    {
                        offerItemId = oi.OfferItemID,
                        requestItemId = oi.RequestItemID,
                        unitPrice = oi.UnitPrice,
                        unitPriceFormatted = oi.UnitPrice.ToString("N2") + " ₺",
                        lineTotal = oi.LineTotal,
                        lineTotalFormatted = oi.LineTotal.ToString("N2") + " ₺",
                        notes = oi.Notes ?? ""
                    }).ToList();

                    return new
                    {
                        offerId = o.OfferID,
                        supplierId = o.SupplierID,
                        supplierName = o.Supplier != null ? (o.Supplier.CompanyName ?? o.Supplier.CompanyOfficial ?? "Tedarikçi Firma") : $"Firma #{o.SupplierID}",
                        supplierPhone = o.Supplier?.CompanyPhone ?? "",
                        supplierMail = o.Supplier?.CompanyMail ?? "",
                        offeredPrice = o.OfferedPrice,
                        offeredPriceFormatted = o.OfferedPrice.ToString("N2") + " ₺",
                        deliveryDays = o.DeliveryDays,
                        validityDays = o.ValidityDays,
                        offerDate = o.OfferDate.ToString("dd.MM.yyyy HH:mm"),
                        status = o.Status,
                        supplierNotes = parsed.CleanNotes ?? "",
                        rejectionReason = fallbackReason,
                        isRevised = parsed.IsRevised,
                        previousPrice = parsed.PreviousPrice,
                        previousPriceFormatted = parsed.PreviousPrice.HasValue ? parsed.PreviousPrice.Value.ToString("N2") + " ₺" : null,
                        previousRejectionReason = parsed.PreviousRejectionReason,
                        isMyOffer = o.SupplierID == currentCompanyId,
                        offerItems = offerItemsList
                    };
                }).OrderBy(o => o.offeredPrice).ToList();

                var myOffer = offersList.FirstOrDefault(o => o.isMyOffer);

                int totalQty = itemsList.Count > 0 ? (int)Math.Round(itemsList.Sum(i => (decimal)i.quantity)) : (req.Quantity ?? 1);

                return Json(new
                {
                    success = true,
                    requestId = req.RequestID,
                    requestCode = $"REQ-{req.RequestID:D4}",
                    requestDate = req.RequestDate?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    productName = req.ProductName ?? "",
                    buyerInfo = req.BuyerInfo ?? "",
                    quantity = totalQty,
                    itemCount = itemsList.Count,
                    items = itemsList,
                    requestType = req.RequestType ?? "Satın Alma",
                    estimatedBudget = req.EstimatedBudget,
                    estimatedBudgetFormatted = req.EstimatedBudget.HasValue ? req.EstimatedBudget.Value.ToString("N2") + " ₺" : "Belirtilmedi",
                    targetDeliveryDate = req.TargetDeliveryDate?.ToString("dd.MM.yyyy") ?? "Belirtilmedi",
                    status = req.Status ?? "Beklemede",
                    notes = req.Notes ?? "",
                    isOwner = isOwner,
                    currentCompanyId = currentCompanyId,
                    offerCount = offersList.Count,
                    offers = offersList,
                    myOffer = myOffer
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Fiyat Kıyaslama Modalı için seçilen talebin tekliflerini ve analiz metriklerini JSON olarak döner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetComparisonData(string id)
        {
            try
            {
                int numericId = 0;
                if (!int.TryParse(id, out numericId))
                {
                    var digitsOnly = new string((id ?? "").Where(char.IsDigit).ToArray());
                    int.TryParse(digitsOnly, out numericId);
                }

                if (numericId <= 0)
                {
                    return Json(new { success = false, message = "Geçersiz talep numarası." });
                }

                int currentCompanyId = GetCurrentCompanyId();
                string userType = GetCurrentUserType();
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;
                bool isAdmin = string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase);

                var req = await _context.Requests
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                    .Include(r => r.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RequestID == numericId);

                if (req == null)
                {
                    return Json(new { success = false, message = "Talep bulunamadı." });
                }

                bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);
                if (!isOwner && !isAdmin)
                {
                    return Json(new { success = false, message = "Fiyat kıyaslama analizi yalnızca talep sahibi tarafından görüntülenebilir." });
                }

                var itemsList = req.Items.OrderBy(i => i.ItemOrder).Select(i => new
                {
                    requestItemId = i.RequestItemID,
                    itemOrder = i.ItemOrder,
                    itemName = i.ItemName,
                    quantity = i.Quantity,
                    unit = i.Unit ?? "Adet",
                    estimatedUnitPrice = i.EstimatedUnitPrice,
                    estimatedUnitPriceFormatted = i.EstimatedUnitPrice.HasValue ? i.EstimatedUnitPrice.Value.ToString("N2") + " ₺" : null,
                    estimatedTotalPrice = i.EstimatedUnitPrice.HasValue ? (i.EstimatedUnitPrice.Value * i.Quantity) : (decimal?)null,
                    estimatedTotalPriceFormatted = i.EstimatedUnitPrice.HasValue ? (i.EstimatedUnitPrice.Value * i.Quantity).ToString("N2") + " ₺" : null,
                    itemDescription = i.ItemDescription ?? ""
                }).ToList();

                if (itemsList.Count == 0 && !string.IsNullOrWhiteSpace(req.ProductName))
                {
                    itemsList.Add(new
                    {
                        requestItemId = 0,
                        itemOrder = 1,
                        itemName = req.ProductName,
                        quantity = (decimal)(req.Quantity ?? 1),
                        unit = "Adet",
                        estimatedUnitPrice = req.EstimatedBudget.HasValue && req.Quantity > 0 ? (req.EstimatedBudget.Value / req.Quantity.Value) : (decimal?)null,
                        estimatedUnitPriceFormatted = req.EstimatedBudget.HasValue && req.Quantity > 0 ? (req.EstimatedBudget.Value / req.Quantity.Value).ToString("N2") + " ₺" : null,
                        estimatedTotalPrice = req.EstimatedBudget,
                        estimatedTotalPriceFormatted = req.EstimatedBudget.HasValue ? req.EstimatedBudget.Value.ToString("N2") + " ₺" : null,
                        itemDescription = req.Notes ?? ""
                    });
                }

                int qty = itemsList.Count > 0 ? (int)Math.Round(itemsList.Sum(i => (decimal)i.quantity)) : (req.Quantity ?? 1);

                var rawOffers = req.Offers.Select(o =>
                {
                    var supName = o.Supplier != null ? (o.Supplier.CompanyName ?? o.Supplier.CompanyOfficial ?? "Tedarikçi Firma") : $"Firma #{o.SupplierID}";
                    var unitPrice = Math.Round(o.OfferedPrice / (qty > 0 ? qty : 1), 2);
                    var parsed = ParseSupplierNotes(o.SupplierNotes);
                    var fallbackReason = o.Status == "Reddedildi" ? (parsed.RejectionReason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.") : null;

                    decimal? budgetVariance = req.EstimatedBudget.HasValue && req.EstimatedBudget.Value > 0
                        ? Math.Round(((o.OfferedPrice - req.EstimatedBudget.Value) / req.EstimatedBudget.Value) * 100, 1)
                        : null;

                    return new
                    {
                        offerId = o.OfferID,
                        supplierId = o.SupplierID,
                        supplierName = supName,
                        supplierOfficial = o.Supplier?.CompanyOfficial ?? "",
                        supplierPhone = o.Supplier?.CompanyPhone ?? "",
                        supplierMail = o.Supplier?.CompanyMail ?? "",
                        offeredPrice = o.OfferedPrice,
                        offeredPriceFormatted = o.OfferedPrice.ToString("N2") + " ₺",
                        unitPrice = unitPrice,
                        unitPriceFormatted = unitPrice.ToString("N2") + " ₺",
                        deliveryDays = o.DeliveryDays,
                        validityDays = o.ValidityDays,
                        offerDate = o.OfferDate.ToString("dd.MM.yyyy HH:mm"),
                        status = o.Status,
                        supplierNotes = parsed.CleanNotes ?? "",
                        rejectionReason = fallbackReason,
                        isRevised = parsed.IsRevised,
                        previousPrice = parsed.PreviousPrice,
                        previousPriceFormatted = parsed.PreviousPrice.HasValue ? parsed.PreviousPrice.Value.ToString("N2") + " ₺" : null,
                        previousRejectionReason = parsed.PreviousRejectionReason,
                        isMyOffer = o.SupplierID == currentCompanyId,
                        isAccepted = string.Equals(o.Status, "Kabul Edildi", StringComparison.OrdinalIgnoreCase),
                        isRejected = string.Equals(o.Status, "Reddedildi", StringComparison.OrdinalIgnoreCase),
                        budgetVariance = budgetVariance,
                        budgetVarianceFormatted = budgetVariance.HasValue 
                            ? (budgetVariance.Value <= 0 ? $"%{Math.Abs(budgetVariance.Value)} Tasarruf" : $"+%{budgetVariance.Value} Aşım")
                            : "—"
                    };
                }).OrderBy(o => o.offeredPrice).ToList();

                decimal? lowestPrice = rawOffers.Any() ? rawOffers.Min(o => o.offeredPrice) : null;
                decimal? maxPrice = rawOffers.Any() ? rawOffers.Max(o => o.offeredPrice) : null;
                int? fastestDays = rawOffers.Any() ? rawOffers.Min(o => o.deliveryDays) : null;
                decimal? avgPrice = rawOffers.Any() ? Math.Round(rawOffers.Average(o => o.offeredPrice), 2) : null;

                var lowestOffer = rawOffers.FirstOrDefault(o => o.offeredPrice == lowestPrice);
                var fastestOffer = rawOffers.FirstOrDefault(o => o.deliveryDays == fastestDays);

                decimal? budgetSavings = null;
                if (req.EstimatedBudget.HasValue && req.EstimatedBudget.Value > 0 && lowestPrice.HasValue)
                {
                    budgetSavings = req.EstimatedBudget.Value - lowestPrice.Value;
                }
                else if (maxPrice.HasValue && lowestPrice.HasValue && maxPrice.Value > lowestPrice.Value)
                {
                    budgetSavings = maxPrice.Value - lowestPrice.Value;
                }

                bool isApproved = req.Status == "Onaylandı" || req.Status == "Kabul Edildi" || req.Status == "Tamamlandı";

                var analyzedOffers = rawOffers.Select(o => new
                {
                    o.offerId,
                    o.supplierId,
                    o.supplierName,
                    o.supplierOfficial,
                    o.supplierPhone,
                    o.supplierMail,
                    o.offeredPrice,
                    o.offeredPriceFormatted,
                    o.unitPrice,
                    o.unitPriceFormatted,
                    o.deliveryDays,
                    o.validityDays,
                    o.offerDate,
                    o.status,
                    o.supplierNotes,
                    o.rejectionReason,
                    o.isMyOffer,
                    o.isAccepted,
                    o.isRejected,
                    o.budgetVariance,
                    o.budgetVarianceFormatted,
                    isLowestPrice = lowestPrice.HasValue && o.offeredPrice == lowestPrice.Value,
                    isFastestDelivery = fastestDays.HasValue && o.deliveryDays == fastestDays.Value
                }).ToList();

                return Json(new
                {
                    success = true,
                    requestId = req.RequestID,
                    requestCode = $"REQ-{req.RequestID:D4}",
                    productName = req.ProductName ?? "",
                    requestType = req.RequestType ?? "Satın Alma",
                    quantity = qty,
                    itemCount = itemsList.Count,
                    items = itemsList,
                    estimatedBudget = req.EstimatedBudget,
                    estimatedBudgetFormatted = req.EstimatedBudget.HasValue ? req.EstimatedBudget.Value.ToString("N2") + " ₺" : "Belirtilmedi",
                    targetDeliveryDate = req.TargetDeliveryDate?.ToString("dd.MM.yyyy") ?? "Belirtilmedi",
                    status = req.Status ?? "Beklemede",
                    notes = req.Notes ?? "",
                    isOwner = isOwner || isAdmin,
                    isApproved = isApproved,
                    winningSupplierName = req.ReceiverName ?? "",
                    offerCount = analyzedOffers.Count,
                    lowestPrice = lowestPrice,
                    lowestPriceFormatted = lowestPrice.HasValue ? lowestPrice.Value.ToString("N2") + " ₺" : "—",
                    lowestSupplierName = lowestOffer?.supplierName ?? "—",
                    fastestDays = fastestDays,
                    fastestSupplierName = fastestOffer?.supplierName ?? "—",
                    avgPrice = avgPrice,
                    avgPriceFormatted = avgPrice.HasValue ? avgPrice.Value.ToString("N2") + " ₺" : "—",
                    unitAvgPriceFormatted = avgPrice.HasValue ? (avgPrice.Value / qty).ToString("N2") + " ₺" : "—",
                    budgetSavings = budgetSavings,
                    budgetSavingsFormatted = budgetSavings.HasValue ? (budgetSavings.Value >= 0 ? "+ " : "- ") + Math.Abs(budgetSavings.Value).ToString("N2") + " ₺" : "—",
                    isBudgetSavingsPositive = !budgetSavings.HasValue || budgetSavings.Value >= 0,
                    hasBudget = req.EstimatedBudget.HasValue && req.EstimatedBudget.Value > 0,
                    offers = analyzedOffers
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Kıyaslama verileri alınamadı: " + ex.Message });
            }
        }

        /// <summary>
        /// Talep Silme Action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                string userType = GetCurrentUserType();
                bool isAdmin = userType == "SuperAdmin" || userType == "Admin";
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;

                var req = await _context.Requests
                    .Include(r => r.Offers)
                    .FirstOrDefaultAsync(r => r.RequestID == id);

                if (req != null)
                {
                    bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);

                    if (!isOwner && !isAdmin)
                    {
                        var unauthMsg = "Yalnızca kendi açtığınız talepleri silebilirsiniz.";
                        if (isAjax) return Json(new { success = false, message = unauthMsg });
                        TempData["ErrorMessage"] = unauthMsg;
                        return RedirectToAction(nameof(Index));
                    }

                    // Onaylanmış teklif, kabul edilmiş anlaşma veya inceleme/değerlendirme geçmişi kontrolü
                    bool hasApprovedOffer = (req.Offers != null && req.Offers.Any(o => o.Status == "Kabul Edildi" || o.Status == "Onaylandı"))
                                         || req.Status == "Onaylandı" || req.Status == "Kabul Edildi" || req.Status == "Tamamlandı"
                                         || await _context.Reviews.AnyAsync(r => r.RequestID == id);

                    if (hasApprovedOffer)
                    {
                        // Alıcı veya satıcı haklarını korumak için fiziksel olarak silme!
                        // Talebin durumunu 'Kaldırıldı' yaparak yayından kaldır ve arşivle.
                        req.Status = "Kaldırıldı";
                        string auditNote = $"[Yayından Kaldırıldı / Arşivlendi: {DateTime.Now:dd.MM.yyyy HH:mm} - Sahip firma tarafından kaldırıldı. Onaylı teklif ve alıcı/satıcı hakları korundu]";
                        req.Notes = string.IsNullOrWhiteSpace(req.Notes) ? auditNote : $"{req.Notes} | {auditNote}";
                        await _context.SaveChangesAsync();

                        try
                        {
                            await _hubContext.Clients.All.SendAsync("OnRequestDeleted", new { requestId = id, isArchived = true });
                        }
                        catch (Exception hubEx)
                        {
                            Console.WriteLine($"SignalR Delete broadcast error: {hubEx.Message}");
                        }

                        var archiveMsg = "Bu talebe ait onaylanmış tedarikçi teklifi ve anlaşması bulunduğu için alıcı ve satıcı haklarını korumak adına talep veritabanından tamamen silinmedi, arşivlenerek yayından kaldırıldı.";
                        if (isAjax) return Json(new { success = true, message = archiveMsg, requestId = id, isArchived = true });
                        TempData["SuccessMessage"] = archiveMsg;
                    }
                    else
                    {
                        _context.Requests.Remove(req);
                        await _context.SaveChangesAsync();

                        try
                        {
                            await _hubContext.Clients.All.SendAsync("OnRequestDeleted", new { requestId = id, isArchived = false });
                        }
                        catch (Exception hubEx)
                        {
                            Console.WriteLine($"SignalR Delete broadcast error: {hubEx.Message}");
                        }

                        var succMsg = $"#{id} numaralı talep veritabanından başarıyla silindi.";
                        if (isAjax) return Json(new { success = true, message = succMsg, requestId = id, isArchived = false });
                        TempData["SuccessMessage"] = succMsg;
                    }
                }
                else
                {
                    var notFoundMsg = $"#{id} numaralı talep bulunamadı.";
                    if (isAjax) return Json(new { success = false, message = notFoundMsg });
                    TempData["ErrorMessage"] = notFoundMsg;
                }
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                var errDetail = "Silme işlemi sırasında hata oluştu: " + detail;
                if (isAjax) return Json(new { success = false, message = errDetail });
                TempData["ErrorMessage"] = errDetail;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Alıcıların tedarikçi tekliflerini kalem bazlı ve genel ticari şartlarla kıyaslayabileceği ekran Action'ı.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Compare(int? id)
        {
            int currentCompanyId = GetCurrentCompanyId();
            string userType = GetCurrentUserType();
            var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;
            int companyTypeId = currentCompany?.CompanyTypeID ?? (int.TryParse(User.FindFirst("CompanyTypeID")?.Value, out int ctype) ? ctype : 3);
            bool isAdmin = string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase);

            string activeMode;
            if (companyTypeId == 1 && !isAdmin)
            {
                activeMode = "Supplier";
            }
            else if (companyTypeId == 2 && !isAdmin)
            {
                activeMode = "Buyer";
            }
            else
            {
                var cookieMode = Request.Cookies["ActiveDashboardMode"];
                activeMode = !string.IsNullOrWhiteSpace(cookieMode) && string.Equals(cookieMode, "Supplier", StringComparison.OrdinalIgnoreCase)
                    ? "Supplier"
                    : "Buyer";
            }

            ViewBag.DashboardRole = activeMode;
            ViewBag.CanSwitchRole = (companyTypeId == 3 || isAdmin);
            ViewBag.CompanyTypeID = companyTypeId;

            // Alıcının taleplerini çek (dropdown seçici için)
            var allRequests = await _context.Requests
                .Include(r => r.Offers)
                .Include(r => r.Items)
                .AsNoTracking()
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var myRequests = allRequests.Where(r => isAdmin || IsRequestOwnedByCurrentCompany(r, currentCompanyId, currentCompany)).ToList();

            var availableRequestsDto = myRequests.Select(r => new RequestSelectorDto
            {
                RequestID = r.RequestID,
                ProductName = r.ProductName ?? r.RequestType ?? "Talep",
                Quantity = r.Quantity ?? 1,
                ItemCount = r.Items.Any() ? r.Items.Count : 1,
                EstimatedBudget = r.EstimatedBudget,
                OfferCount = r.Offers.Count,
                Status = r.Status ?? "Beklemede"
            }).ToList();

            // Seçilen talep belirleme
            Requests? targetRequest = null;
            if (id.HasValue && id.Value > 0)
            {
                targetRequest = myRequests.FirstOrDefault(r => r.RequestID == id.Value);
            }

            if (targetRequest == null)
            {
                targetRequest = myRequests.FirstOrDefault(r => r.Offers.Any()) ?? myRequests.FirstOrDefault();
            }

            var reqItemsList = targetRequest?.Items.OrderBy(i => i.ItemOrder).Select(i => new RequestItemDto
            {
                RequestItemID = i.RequestItemID,
                RequestID = i.RequestID,
                ItemOrder = i.ItemOrder,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                Unit = i.Unit ?? "Adet",
                EstimatedUnitPrice = i.EstimatedUnitPrice,
                ItemDescription = i.ItemDescription
            }).ToList() ?? new List<RequestItemDto>();

            var viewModel = new OfferComparisonViewModel
            {
                SelectedRequest = targetRequest,
                AvailableRequests = availableRequestsDto,
                Items = reqItemsList,
                IsOwner = targetRequest != null && IsRequestOwnedByCurrentCompany(targetRequest, currentCompanyId, currentCompany)
            };

            if (targetRequest != null)
            {
                var offersWithSuppliers = await _context.RequestOffers
                    .Include(o => o.Supplier)
                    .Where(o => o.RequestID == targetRequest.RequestID)
                    .OrderBy(o => o.OfferedPrice)
                    .ToListAsync();

                if (offersWithSuppliers.Any())
                {
                    var lowestPrice = offersWithSuppliers.Min(o => o.OfferedPrice);
                    var maxPrice = offersWithSuppliers.Max(o => o.OfferedPrice);
                    var minLeadTime = offersWithSuppliers.Min(o => o.DeliveryDays);
                    var avgPrice = offersWithSuppliers.Average(o => o.OfferedPrice);
                    var lowestOffer = offersWithSuppliers.First(o => o.OfferedPrice == lowestPrice);
                    var fastestOffer = offersWithSuppliers.First(o => o.DeliveryDays == minLeadTime);

                    int reqQty = targetRequest.Quantity.HasValue && targetRequest.Quantity.Value > 0 ? targetRequest.Quantity.Value : 1;

                    viewModel.LowestOfferPrice = lowestPrice;
                    viewModel.LowestOfferSupplierName = lowestOffer.Supplier?.CompanyName ?? lowestOffer.Supplier?.CompanyOfficial ?? $"Tedarikçi #{lowestOffer.SupplierID}";
                    viewModel.FastestDeliveryDays = minLeadTime;
                    viewModel.FastestSupplierName = fastestOffer.Supplier?.CompanyName ?? fastestOffer.Supplier?.CompanyOfficial ?? $"Tedarikçi #{fastestOffer.SupplierID}";
                    viewModel.AverageOfferPrice = Math.Round(avgPrice, 2);
                    viewModel.MaxOfferPrice = maxPrice;

                    if (targetRequest.EstimatedBudget.HasValue && targetRequest.EstimatedBudget.Value > 0)
                    {
                        viewModel.PotentialSavingsComparedToBudget = targetRequest.EstimatedBudget.Value - lowestPrice;
                    }

                    if (maxPrice > lowestPrice)
                    {
                        viewModel.PotentialSavingsComparedToMax = maxPrice - lowestPrice;
                    }

                    viewModel.Offers = offersWithSuppliers.Select(o =>
                    {
                        var supName = o.Supplier?.CompanyName ?? o.Supplier?.CompanyOfficial ?? $"Tedarikçi #{o.SupplierID}";
                        var unitPrice = Math.Round(o.OfferedPrice / reqQty, 2);
                        var parsed = ParseSupplierNotes(o.SupplierNotes);
                        var fallbackReason = o.Status == "Reddedildi" ? (parsed.RejectionReason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.") : null;

                        decimal? budgetVariance = targetRequest.EstimatedBudget.HasValue && targetRequest.EstimatedBudget.Value > 0
                            ? Math.Round(((o.OfferedPrice - targetRequest.EstimatedBudget.Value) / targetRequest.EstimatedBudget.Value) * 100, 1)
                            : null;

                        return new OfferComparisonItemDto
                        {
                            OfferID = o.OfferID,
                            SupplierID = o.SupplierID,
                            SupplierName = supName,
                            SupplierOfficial = o.Supplier?.CompanyOfficial,
                            SupplierPhone = o.Supplier?.CompanyPhone,
                            SupplierMail = o.Supplier?.CompanyMail,
                            SupplierCity = o.Supplier?.TaxOffice,
                            OfferedPrice = o.OfferedPrice,
                            UnitOfferPrice = unitPrice,
                            DeliveryDays = o.DeliveryDays,
                            ValidityDays = o.ValidityDays,
                            OfferDate = o.OfferDate,
                            Status = o.Status,
                            SupplierNotes = parsed.CleanNotes,
                            RejectionReason = fallbackReason,
                            IsRevised = parsed.IsRevised,
                            PreviousPrice = parsed.PreviousPrice,
                            PreviousRejectionReason = parsed.PreviousRejectionReason,
                            IsLowestPrice = o.OfferedPrice == lowestPrice,
                            IsFastestDelivery = o.DeliveryDays == minLeadTime,
                            BudgetVariancePct = budgetVariance,
                            SavingsAmountComparedToMax = maxPrice > o.OfferedPrice ? maxPrice - o.OfferedPrice : 0
                        };
                    }).ToList();
                }
            }

            return View(viewModel);
        }
    }
}
