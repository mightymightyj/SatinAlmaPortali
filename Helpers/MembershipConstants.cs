using System;

namespace satinalmaportali2.Helpers
{
    public static class MembershipConstants
    {
        public const int TierBedava = 1;
        public const int TierPro = 2;
        public const int TierKurumsal = 3;

        public const string NameBedava = "Bedava";
        public const string NamePro = "Pro";
        public const string NameKurumsal = "Kurumsal";

        // Limitler
        public const int BedavaMaxRequests = 10;
        public const int BedavaMaxProducts = 10;
        public const int BedavaMaxImagesPerProduct = 3;
        public const decimal BedavaPrice = 0m;

        public const int ProMaxRequests = 100;
        public const int ProMaxProducts = 100;
        public const int ProMaxImagesPerProduct = 20;
        public const decimal ProPrice = 990m;

        public const int KurumsalMaxRequests = int.MaxValue;
        public const int KurumsalMaxProducts = int.MaxValue;
        public const int KurumsalMaxImagesPerProduct = int.MaxValue;
        public const decimal KurumsalPrice = 2990m;

        public static int GetMaxRequests(int? membershipTypeId) => (membershipTypeId ?? TierBedava) switch
        {
            TierKurumsal => KurumsalMaxRequests,
            TierPro => ProMaxRequests,
            _ => BedavaMaxRequests
        };

        public static int GetMaxProducts(int? membershipTypeId) => (membershipTypeId ?? TierBedava) switch
        {
            TierKurumsal => KurumsalMaxProducts,
            TierPro => ProMaxProducts,
            _ => BedavaMaxProducts
        };

        public static int GetMaxImagesPerProduct(int? membershipTypeId) => (membershipTypeId ?? TierBedava) switch
        {
            TierKurumsal => KurumsalMaxImagesPerProduct,
            TierPro => ProMaxImagesPerProduct,
            _ => BedavaMaxImagesPerProduct
        };

        public static string GetTierName(int? membershipTypeId) => (membershipTypeId ?? TierBedava) switch
        {
            TierKurumsal => NameKurumsal,
            TierPro => NamePro,
            _ => NameBedava
        };

        public static decimal GetPrice(int? membershipTypeId) => (membershipTypeId ?? TierBedava) switch
        {
            TierKurumsal => KurumsalPrice,
            TierPro => ProPrice,
            _ => BedavaPrice
        };
    }
}
