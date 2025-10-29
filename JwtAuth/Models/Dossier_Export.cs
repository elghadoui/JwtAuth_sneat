using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApi.Models
{
    [Table("dossier_export")]
    public class DossierExport
    {
        [Key]
        [Column("id")]
        [StringLength(200)]
        public string Id { get; set; }

        [Required]
        [Column("numdos")]
        [StringLength(100)]
        public string Numdos { get; set; }

        [Column("numtc")]
        [StringLength(100)]
        public string Numtc { get; set; }

        [Column("navire")]
        [StringLength(100)]
        public string Navire { get; set; }

        [Column("dtedep")]
        public DateTime? Dtedep { get; set; }

        [Column("codpay")]
        [StringLength(50)]
        public string Codpay { get; set; }

        [Column("nompay")]
        [StringLength(100)]
        public string Nompay { get; set; }

        [Column("coddes")]
        [StringLength(50)]
        public string Coddes { get; set; }

        [Column("rsclient")]
        [StringLength(255)]
        public string Rsclient { get; set; }

        [Column("transite")]
        [StringLength(255)]
        public string Transite { get; set; }

        [Column("transpor")]
        [StringLength(255)]
        public string Transpor { get; set; }

        [Column("refexp")]
        [StringLength(100)]
        public string Refexp { get; set; }

        [Column("exporter")]
        [StringLength(255)]
        public string Exporter { get; set; }

        [Column("codvar")]
        [StringLength(50)]
        public string Codvar { get; set; }

        [Column("produit")]
        [StringLength(255)]
        public string Produit { get; set; }

        [Column("nbrpal")]
        public decimal? Nbrpal { get; set; }

        [Column("nbrcol")]
        public decimal? Nbrcol { get; set; }

        [Column("pdscom")]
        public decimal? Pdscom { get; set; }

        [Column("typtrp")]
        [StringLength(50)]
        public string Typtrp { get; set; }

        [Column("stations")]
        [StringLength(50)]
        public string Stations { get; set; } = "zaouia";

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
