using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public RequestsController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

            if (string.IsNullOrEmpty(tab))
            {
                tab = "all";
            }

            var requestList = new List<RequestListItemViewModel>();

            try
            {
                // Firma isimlerini getir
                var companies = await _context.Companies
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.CompanyID, c => c.CompanyName ?? c.CompanyOfficial ?? $"Firma #{c.CompanyID}");

                // Veritabanındaki gerçek talepleri ve teklifleri çek
                var dbRequests = await _context.Requests
                    .Include(r => r.Offers)
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
                            : (!string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Belirtilmemiş Ürün");

                        bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);
                        var myOffer = currentCompanyId > 0 ? req.Offers.FirstOrDefault(o => o.SupplierID == currentCompanyId) : null;
                        var bestOffer = req.Offers.Any() ? req.Offers.Min(o => o.OfferedPrice) : (decimal?)null;
                        var (cleanNotes, myRejectionReason) = ParseSupplierNotes(myOffer?.SupplierNotes);

                        requestList.Add(new RequestListItemViewModel
                        {
                            RequestID = req.RequestID,
                            RequestDate = req.RequestDate ?? DateTime.Now,
                            SenderID = req.SenderID,
                            RecieverID = req.RecieverID,
                            BuyerInfo = buyerName,
                            ProductName = prodName,
                            Quantity = req.Quantity ?? 1,
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
                            MyOfferRejectionReason = myRejectionReason ?? (myOffer?.Status == "Reddedildi" ? "Talep için başka bir tedarikçinin teklifi kabul edildi." : null)
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

            // Sekme Filtrelemesi (Tab)
            if (tab == "my-requests" && currentCompanyId > 0)
            {
                requestList = requestList.Where(r => r.IsOwner).ToList();
            }
            else if (tab == "incoming-offers" && currentCompanyId > 0)
            {
                requestList = requestList.Where(r => r.IsOwner && r.OfferCount > 0).ToList();
            }
            else if (tab == "my-bids" && currentCompanyId > 0)
            {
                requestList = requestList.Where(r => r.HasMyOffer).ToList();
            }

            // Arama ve filtreleme
            if (!string.IsNullOrWhiteSpace(search))
            {
                var query = search.Trim().ToLower();
                requestList = requestList.Where(r =>
                    r.BuyerInfo.ToLower().Contains(query) ||
                    r.ProductName.ToLower().Contains(query) ||
                    r.RequestType.ToLower().Contains(query) ||
                    (r.Notes != null && r.Notes.ToLower().Contains(query))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(type) && type != "Tümü")
            {
                requestList = requestList.Where(r => r.RequestType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                requestList = requestList.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
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
        /// Yeni Talep Oluşturma POST Action
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRequestInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Lütfen tüm zorunlu alanları (Ürün Adı ve Adet) doldurunuz.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;
                string companyName = currentCompany?.CompanyName ?? currentCompany?.CompanyOfficial ?? GetCurrentCompanyName();
                string finalBuyerInfo = !string.IsNullOrWhiteSpace(companyName) ? companyName : (!string.IsNullOrWhiteSpace(input.BuyerInfo) ? input.BuyerInfo : "Kurumsal Alıcı");

                var newRequest = new Requests
                {
                    RequestDate = DateTime.UtcNow,
                    SenderID = currentCompanyId > 0 ? currentCompanyId : null,
                    BuyerInfo = finalBuyerInfo,
                    ProductName = input.ProductName,
                    Quantity = input.Quantity,
                    RequestType = string.IsNullOrWhiteSpace(input.RequestType) ? "Satın Alma" : input.RequestType,
                    CategoryID = input.CategoryID,
                    EstimatedBudget = input.EstimatedBudget,
                    TargetDeliveryDate = input.TargetDeliveryDate,
                    Status = "Beklemede",
                    Notes = input.Notes
                };

                _context.Requests.Add(newRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"'{input.ProductName}' ürün talebiniz ({input.Quantity} Adet) başarıyla oluşturuldu ve tedarikçilere açıldı!";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Talep veritabanına kaydedilirken bir hata oluştu: " + detail;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Dashboard veya sayfa üzerinden AJAX ile Talep Oluşturma Endpoint'i
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] CreateRequestInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.ProductName) || input.Quantity <= 0)
            {
                return Json(new { success = false, message = "Lütfen ürün adı ve geçerli bir miktar giriniz." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;
                string companyName = currentCompany?.CompanyName ?? currentCompany?.CompanyOfficial ?? GetCurrentCompanyName();
                string finalBuyerInfo = !string.IsNullOrWhiteSpace(companyName) ? companyName : (!string.IsNullOrWhiteSpace(input.BuyerInfo) ? input.BuyerInfo : "Kurumsal Alıcı");

                var newRequest = new Requests
                {
                    RequestDate = DateTime.UtcNow,
                    SenderID = currentCompanyId > 0 ? currentCompanyId : null,
                    BuyerInfo = finalBuyerInfo,
                    ProductName = input.ProductName,
                    Quantity = input.Quantity,
                    RequestType = string.IsNullOrWhiteSpace(input.RequestType) ? "Satın Alma" : input.RequestType,
                    CategoryID = input.CategoryID,
                    EstimatedBudget = input.EstimatedBudget,
                    TargetDeliveryDate = input.TargetDeliveryDate,
                    Status = "Beklemede",
                    Notes = input.Notes
                };

                _context.Requests.Add(newRequest);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    requestId = newRequest.RequestID,
                    message = $"'{newRequest.ProductName}' talebi başarıyla oluşturuldu!"
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

                var request = await _context.Requests.Include(r => r.Offers).FirstOrDefaultAsync(r => r.RequestID == input.RequestID);
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
                    .FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == currentCompanyId);

                if (existingOffer != null)
                {
                    // Alıcı teklifi onayladıktan sonra kilitlenir
                    if (string.Equals(existingOffer.Status, "Kabul Edildi", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existingOffer.Status, "Onaylandı", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ErrorMessage"] = "Bu teklif alıcı tarafından onaylanmış olduğu için üzerinde değişiklik yapılamaz.";
                        return RedirectToAction(nameof(Index));
                    }

                    bool wasRejected = string.Equals(existingOffer.Status, "Reddedildi", StringComparison.OrdinalIgnoreCase);

                    // Güncelle / Revize et
                    existingOffer.OfferedPrice = input.OfferedPrice;
                    existingOffer.DeliveryDays = input.DeliveryDays;
                    existingOffer.ValidityDays = input.ValidityDays;
                    existingOffer.SupplierNotes = input.SupplierNotes;
                    existingOffer.OfferDate = DateTime.UtcNow;
                    existingOffer.Status = "Beklemede";

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

                // Alıcı firmaya e-posta bildirimini arka planda gönder
                await TriggerOfferReceivedEmailNotificationAsync(request, currentCompany, input);
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

                var request = await _context.Requests.Include(r => r.Offers).FirstOrDefaultAsync(r => r.RequestID == input.RequestID);
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

                var existingOffer = await _context.RequestOffers
                    .FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == currentCompanyId);

                if (existingOffer != null)
                {
                    // Alıcı teklifi onayladıktan sonra düzenlenemez
                    if (string.Equals(existingOffer.Status, "Kabul Edildi", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existingOffer.Status, "Onaylandı", StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { success = false, message = "Bu teklif alıcı tarafından onaylandığı için artık düzenlenemez." });
                    }

                    bool wasRejected = string.Equals(existingOffer.Status, "Reddedildi", StringComparison.OrdinalIgnoreCase);

                    existingOffer.OfferedPrice = input.OfferedPrice;
                    existingOffer.DeliveryDays = input.DeliveryDays;
                    existingOffer.ValidityDays = input.ValidityDays;
                    existingOffer.SupplierNotes = input.SupplierNotes;
                    existingOffer.OfferDate = DateTime.UtcNow;
                    existingOffer.Status = "Beklemede";

                    if (request.Status == "Beklemede")
                    {
                        request.Status = "Teklif Alındı";
                    }

                    await _context.SaveChangesAsync();
                    await TriggerOfferReceivedEmailNotificationAsync(request, currentCompany, input);

                    return Json(new
                    {
                        success = true,
                        message = wasRejected
                            ? $"Reddedilen teklifiniz {input.OfferedPrice:N2} ₺ olarak revize edildi ve alıcıya tekrar iletildi!"
                            : $"Teklifiniz {input.OfferedPrice:N2} ₺ olarak başarıyla güncellendi!"
                    });
                }
                else
                {
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

                    await _context.SaveChangesAsync();
                    await TriggerOfferReceivedEmailNotificationAsync(request, currentCompany, input);

                    return Json(new { success = true, message = $"{input.OfferedPrice:N2} ₺ tutarındaki teklifiniz başarıyla iletildi!" });
                }
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Hata: " + detail });
            }
        }

        /// <summary>
        /// Talebi açan alıcı firmaya yeni teklif geldiğinde otomatik zengin HTML e-postası tetikler.
        /// </summary>
        private async Task TriggerOfferReceivedEmailNotificationAsync(
            Requests request,
            Company? currentCompany,
            SubmitOfferInputModel input)
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

                // 3. Hedef alıcı e-posta adresi mevcutsa arka planda MailKit ile gönder
                if (!string.IsNullOrWhiteSpace(targetEmail))
                {
                    string supplierName = currentCompany?.CompanyName 
                        ?? currentCompany?.CompanyOfficial 
                        ?? GetCurrentCompanyName();

                    string requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=incoming-offers";
                    string rfqCode = $"REQ-{request.RequestID:D4}";
                    string prodName = request.ProductName ?? "Satın Alma Talebi";
                    int qty = request.Quantity ?? 1;
                    decimal offerPrice = input.OfferedPrice;
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
                                requestsUrl: requestsUrl);
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
        /// Tedarikçi notları içerisinden sistem veya kullanıcı red gerekçesini ayrıştırır.
        /// </summary>
        public static (string? CleanNotes, string? RejectionReason) ParseSupplierNotes(string? rawNotes)
        {
            if (string.IsNullOrWhiteSpace(rawNotes))
                return (null, null);

            const string tag = "[Red Gerekçesi:";
            int idx = rawNotes.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int endIdx = rawNotes.IndexOf("]", idx, StringComparison.OrdinalIgnoreCase);
                string reason;
                if (endIdx > idx)
                {
                    reason = rawNotes.Substring(idx + tag.Length, endIdx - (idx + tag.Length)).Trim();
                    string before = rawNotes.Substring(0, idx).TrimEnd(' ', '|');
                    string after = (endIdx + 1 < rawNotes.Length) ? rawNotes.Substring(endIdx + 1).TrimStart(' ', '|') : "";
                    string clean = (before + " " + after).Trim();
                    return (string.IsNullOrWhiteSpace(clean) ? null : clean, reason);
                }
                else
                {
                    reason = rawNotes.Substring(idx + tag.Length).Trim();
                    string clean = rawNotes.Substring(0, idx).TrimEnd(' ', '|');
                    return (string.IsNullOrWhiteSpace(clean) ? null : clean, reason);
                }
            }

            return (rawNotes.Trim(), null);
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
                string requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=my-bids";

                // 1. Kazanan tedarikçiye kabul e-postası
                if (winningOffer.Supplier != null && !string.IsNullOrWhiteSpace(winningOffer.Supplier.CompanyMail))
                {
                    var winEmail = winningOffer.Supplier.CompanyMail.Trim();
                    var winName = winningOffer.Supplier.CompanyName ?? winningOffer.Supplier.CompanyOfficial ?? "Tedarikçi Firma";
                    var winPrice = winningOffer.OfferedPrice;
                    var winDelivery = winningOffer.DeliveryDays;

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
                                requestsUrl: requestsUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Kabul e-posta arka plan gönderim hatası: {ex.Message}");
                        }
                    });
                }

                // 2. Elenen tedarikçilere red gerekçesi e-postası
                foreach (var rej in rejectedOffers)
                {
                    if (rej.Supplier != null && !string.IsNullOrWhiteSpace(rej.Supplier.CompanyMail))
                    {
                        var rejEmail = rej.Supplier.CompanyMail.Trim();
                        var rejName = rej.Supplier.CompanyName ?? rej.Supplier.CompanyOfficial ?? "Tedarikçi Firma";
                        var rejPrice = rej.OfferedPrice;
                        var (_, reason) = ParseSupplierNotes(rej.SupplierNotes);
                        var finalReason = reason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.";

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
                                    requestsUrl: requestsUrl);
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
        /// Tekil bir teklif reddedildiğinde ilgili tedarikçiye arka planda red gerekçesi e-postası gönderir.
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
                    string requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=my-bids";
                    var rejEmail = rejectedOffer.Supplier.CompanyMail.Trim();
                    var rejName = rejectedOffer.Supplier.CompanyName ?? rejectedOffer.Supplier.CompanyOfficial ?? "Tedarikçi Firma";
                    var finalReason = !string.IsNullOrWhiteSpace(reason) ? reason.Trim() : "Alıcı firma tarafından doğrudan reddedildi.";

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
                                requestsUrl: requestsUrl);
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
                    .Include(o => o.Request)
                    .Include(o => o.Supplier)
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

                TempData["SuccessMessage"] = $"Tebrikler! #{request.RequestID} numaralı talep için {offer.OfferedPrice:N2} ₺ tutarındaki teklif kabul edildi ve talep başarıyla ONAYLANDI.";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Teklif onaylanırken hata oluştu: " + detail;
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
                    .Include(o => o.Request)
                    .Include(o => o.Supplier)
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
                    .Include(o => o.Request)
                    .Include(o => o.Supplier)
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
                    .Include(o => o.Request)
                    .Include(o => o.Supplier)
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
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            try
            {
                int currentCompanyId = GetCurrentCompanyId();

                var req = await _context.Requests
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RequestID == id);

                if (req == null)
                {
                    return Json(new { success = false, message = "Talep bulunamadı." });
                }

                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;
                bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);

                var offersList = req.Offers.Select(o =>
                {
                    var (cleanNotes, rejectionReason) = ParseSupplierNotes(o.SupplierNotes);
                    var fallbackReason = o.Status == "Reddedildi" ? (rejectionReason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.") : null;

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
                        supplierNotes = cleanNotes ?? "",
                        rejectionReason = fallbackReason,
                        isMyOffer = o.SupplierID == currentCompanyId
                    };
                }).OrderBy(o => o.offeredPrice).ToList();

                var myOffer = offersList.FirstOrDefault(o => o.isMyOffer);

                return Json(new
                {
                    success = true,
                    requestId = req.RequestID,
                    requestCode = $"REQ-{req.RequestID:D4}",
                    requestDate = req.RequestDate?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    productName = req.ProductName ?? "",
                    buyerInfo = req.BuyerInfo ?? "",
                    quantity = req.Quantity ?? 1,
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
        public async Task<IActionResult> GetComparisonData(int id)
        {
            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                string userType = GetCurrentUserType();
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;
                bool isAdmin = string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase);

                var req = await _context.Requests
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RequestID == id);

                if (req == null)
                {
                    return Json(new { success = false, message = "Talep bulunamadı." });
                }

                bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);
                int qty = req.Quantity.HasValue && req.Quantity.Value > 0 ? req.Quantity.Value : 1;

                var rawOffers = req.Offers.Select(o =>
                {
                    var supName = o.Supplier != null ? (o.Supplier.CompanyName ?? o.Supplier.CompanyOfficial ?? "Tedarikçi Firma") : $"Firma #{o.SupplierID}";
                    var unitPrice = Math.Round(o.OfferedPrice / qty, 2);
                    var (cleanNotes, rejectionReason) = ParseSupplierNotes(o.SupplierNotes);
                    var fallbackReason = o.Status == "Reddedildi" ? (rejectionReason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.") : null;

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
                        supplierNotes = cleanNotes ?? "",
                        rejectionReason = fallbackReason,
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                string userType = GetCurrentUserType();
                bool isAdmin = userType == "SuperAdmin" || userType == "Admin";
                var currentCompany = currentCompanyId > 0 ? await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId) : null;

                var req = await _context.Requests.FindAsync(id);
                if (req != null)
                {
                    bool isOwner = IsRequestOwnedByCurrentCompany(req, currentCompanyId, currentCompany);

                    if (!isOwner && !isAdmin)
                    {
                        TempData["ErrorMessage"] = "Yalnızca kendi açtığınız talepleri silebilirsiniz.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Requests.Remove(req);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"#{id} numaralı talep veritabanından başarıyla silindi.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"#{id} numaralı talep bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Silme işlemi sırasında hata oluştu: " + detail;
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
                .AsNoTracking()
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var myRequests = allRequests.Where(r => isAdmin || IsRequestOwnedByCurrentCompany(r, currentCompanyId, currentCompany)).ToList();

            var availableRequestsDto = myRequests.Select(r => new RequestSelectorDto
            {
                RequestID = r.RequestID,
                ProductName = r.ProductName ?? r.RequestType ?? "Talep",
                Quantity = r.Quantity ?? 1,
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

            var viewModel = new OfferComparisonViewModel
            {
                SelectedRequest = targetRequest,
                AvailableRequests = availableRequestsDto,
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
                        var (cleanNotes, rejectionReason) = ParseSupplierNotes(o.SupplierNotes);
                        var fallbackReason = o.Status == "Reddedildi" ? (rejectionReason ?? "Talep için başka bir tedarikçinin teklifi kabul edildi.") : null;

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
                            SupplierNotes = cleanNotes,
                            RejectionReason = fallbackReason,
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
