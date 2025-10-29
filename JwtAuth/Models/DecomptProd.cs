using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApi.Models
{
   
    [Table("decompt_prod")]
    public class DecomptProd
    {
        [Key]
        [Column("id")]
        [StringLength(50)]
        public string Id { get; set; }

        [Column("refver")]
        public int Refver { get; set; }

        [Column("nomadh")]
        [StringLength(100)]
        public string Nomadh { get; set; }

        [Column("codvar")]
        public int Codvar { get; set; }

        [Column("nomvar")]
        [StringLength(100)]
        public string Nomvar { get; set; }

        [Column("pdReception")]
        [Precision(15, 2)]
        public decimal PdReception { get; set; }

        [Column("pdcond")]
        [Precision(15, 2)]
        public decimal Pdcond { get; set; }

        [Column("expCatI")]
        [Precision(15, 2)]
        public decimal ExpCatI { get; set; }

        [Column("expCatII")]
        [Precision(15, 2)]
        public decimal ExpCatII { get; set; }

        [Column("pdEcart")]
        [Precision(15, 2)]
        public decimal PdEcart { get; set; }

        [Column("freinte")]
        [Precision(15, 2)]
        public decimal Freinte { get; set; }

        [Column("stations")]
        [StringLength(100)]
        public string Stations { get; set; }

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
