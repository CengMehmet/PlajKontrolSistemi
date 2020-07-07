using System;
using System.ComponentModel.DataAnnotations;

namespace PlajKontrolSistemi
{
    internal class HareketKaydi
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string barkod { get; set; }
        public DateTime ? barkodTarih { get; set; }
        public DateTime ? girisTarih { get; set; }
        public decimal ? ucret { get; set; }
        [StringLength(50)]
        public string operatorAdi { get; set; }
        public int ? durum { get; set; }
        [StringLength(50)]
        public string girisNoktasi { get; set; }
        [StringLength(50)]
        public string aboneAdi { get; set; }
        [StringLength(50)]
        public string aciklama { get; set; }
    }
}