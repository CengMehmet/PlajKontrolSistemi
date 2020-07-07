using System;
using System.ComponentModel.DataAnnotations;

namespace PlajKontrolSistemi
{
    internal class Aboneler
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string aboneAdSoyad { get; set; }
        [StringLength(50)]
        public string aboneKart { get; set; }
        [StringLength(50)]
        public string aboneTipi { get; set; }
        public DateTime ? aboneBitisTarih { get; set; }
        public float ? aboneBakiye { get; set; }
        public int ? aboneKalanGiris { get; set; }
        public bool ? aboneSinirsizErisim { get; set; }
    }
}