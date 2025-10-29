using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApi.Models
{
    [Table("rapport_ventes")]
    public class RapportVente
    {
        [Key]
        [Column("id")]
        [StringLength(100)]
        public string Id { get; set; }

        [Required]
        [Column("numvnt")]
        [StringLength(50)]
        public string NumVnt { get; set; }

        [Column("station")]
        [StringLength(50)]
        public string Station { get; set; } = "zaouia";

        [Column("date_vente")]
        public DateTime? DateVente { get; set; }

        [Column("codvar")]
        [StringLength(20)]
        public string CodVar { get; set; }

        [Column("varietes")]
        [StringLength(100)]
        public string Varietes { get; set; }

        [Column("codtype")]
        [StringLength(20)]
        public string CodType { get; set; }

        [Column("type_ecart")]
        [StringLength(100)]
        public string TypeEcart { get; set; }

        [Column("refach")]
        [StringLength(50)]
        public string RefAch { get; set; }

        [Column("acheteurs")]
        [StringLength(150)]
        public string Acheteurs { get; set; }

        [Column("poid_brut", TypeName = "decimal(10,2)")]
        public decimal? PoidBrut { get; set; }

        [Column("poid_pese", TypeName = "decimal(10,2)")]
        public decimal? PoidPese { get; set; }

        [Column("difpese", TypeName = "decimal(10,2)")]
        public decimal? DifPese { get; set; }

        [Column("prxkg", TypeName = "decimal(10,2)")]
        public decimal? PrxKg { get; set; }

        [Column("montant_vente", TypeName = "decimal(12,2)")]
        public decimal? MontantVente { get; set; }

        [Column("montant_reglement", TypeName = "decimal(12,2)")]
        public decimal MontantReglement { get; set; } = 0;

        [Column("sold_vente", TypeName = "decimal(12,2)")]
        public decimal? SoldVente { get; set; }

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Column("date_mise_a_jour")]
        public DateTime DateMiseAJour { get; set; } = DateTime.Now;
    }
}