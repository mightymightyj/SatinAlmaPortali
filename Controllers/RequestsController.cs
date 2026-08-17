using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;

namespace satinalmaportali2.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Talepler sekmesi ana listeleme görünümü.
        /// Doğrudan veritabanındaki gerçek kayıtları listeler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? type, string? status)
        {
            var requestList = new List<RequestListItemViewModel>();

            try
            {
                // Veritabanındaki gerçek talepleri çek
                var dbRequests = await _context.Requests.AsNoTracking().ToListAsync();

                if (dbRequests != null && dbRequests.Any())
                {
                    foreach (var req in dbRequests)
                    {
                        // Sadece girilmiş gerçek talepleri listele
                        string buyerName = !string.IsNullOrWhiteSpace(req.BuyerInfo) 
                            ? req.BuyerInfo 
                            : (req.SenderID.HasValue ? $"Firma #{req.SenderID}" : "Kurumsal Alıcı");

                        string prodName = !string.IsNullOrWhiteSpace(req.ProductName)
                            ? req.ProductName
                            : (!string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Belirtilmemiş Ürün");

                        requestList.Add(new RequestListItemViewModel
                        {
                            RequestID = req.RequestID,
                            RequestDate = req.RequestDate ?? DateTime.Now,
                            BuyerInfo = buyerName,
                            ProductName = prodName,
                            Quantity = req.Quantity ?? 1,
                            RequestType = !string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Satın Alma",
                            Status = !string.IsNullOrWhiteSpace(req.Status) ? req.Status : "Beklemede",
                            Notes = req.Notes
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Veritabanından talepler yüklenirken bir hata oluştu: " + ex.Message;
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
                Requests = requestList.OrderByDescending(r => r.RequestDate).ToList(),
                SearchQuery = search,
                SelectedType = type,
                SelectedStatus = status
            };

            return View(viewModel);
        }

        /// <summary>
        /// Yeni Talep Oluşturma POST Action
        /// Formdan gelen alıcı bilgileri, aranan ürün ve adet bilgilerini veritabanına kaydeder.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRequestInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Lütfen tüm zorunlu alanları (Alıcı Bilgisi, Aranan Ürün ve Adet) doldurunuz.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var newRequest = new Requests
                {
                    RequestDate = DateTime.Now,
                    BuyerInfo = input.BuyerInfo,
                    ProductName = input.ProductName,
                    Quantity = input.Quantity,
                    RequestType = string.IsNullOrWhiteSpace(input.RequestType) ? "Satın Alma" : input.RequestType,
                    Status = "Beklemede",
                    Notes = input.Notes
                };

                _context.Requests.Add(newRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"'{input.ProductName}' ürün talebiniz ({input.Quantity} Adet) veritabanına başarıyla kaydedildi!";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Talep veritabanına kaydedilirken bir hata oluştu: " + detail;
            }

            return RedirectToAction(nameof(Index));
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
                var req = await _context.Requests.FindAsync(id);
                if (req != null)
                {
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
    }
}
