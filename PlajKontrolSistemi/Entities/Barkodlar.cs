using System;
using System.ComponentModel.DataAnnotations;

namespace PlajKontrolSistemi
{
    internal class Barkodlar
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string barkod { get; set; }
    }
}