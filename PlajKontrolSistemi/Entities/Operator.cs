using System.ComponentModel.DataAnnotations;

namespace PlajKontrolSistemi
{
    internal class Operator
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string kullaniciAdi { get; set; }
        [StringLength(50)]
        public string kullaniciSifre { get; set; }
        [StringLength(50)]
        public string adSoyad { get; set; }
        public int kullaniciYetki { get; set; }
    }
}