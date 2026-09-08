using System.Collections.Generic;

namespace satinalmaportali2.Models
{
    public class UrunKategorileriViewModel
    {
        public List<CategoryDirectoryNodeViewModel> RootCategories { get; set; } = new();
        public int TotalRootCategories { get; set; }
        public int TotalCategories { get; set; }
        public int TotalProducts { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalRequests { get; set; }
    }

    public class CategoryDirectoryNodeViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? ParentCategoryID { get; set; }
        public string? Icon { get; set; }
        public int Level { get; set; }
        public int ProductCount { get; set; }
        public int DirectProductCount { get; set; }
        public int TotalDescendantsCount { get; set; }
        public List<CategoryDirectoryNodeViewModel> Children { get; set; } = new();
    }
}
