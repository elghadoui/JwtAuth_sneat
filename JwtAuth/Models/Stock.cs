using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApi.Models
{
    [Table("tbl_stock")]
    public class Stock
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("refver")]
        public int? Refver { get; set; }

        [Column("refverreel")]
        public int? Refverreel { get; set; }

        [Column("nomprod")]
        [StringLength(255)]
        public string Nomprod { get; set; }

        [Column("nomver")]
        [StringLength(255)]
        public string Nomver { get; set; }

        [Column("poidini")]
        public double? Poidini { get; set; }

        [Column("pdjr")]
        public double? Pdjr { get; set; }

        [Column("cumultg")]
        public double? Cumultg { get; set; }

        [Column("stockstat")]
        public double? Stockstat { get; set; }
        [Column("tcondi")]
        public double? conditionnement { get; set; }

        [Column("estimat")]
        public double? Estimat { get; set; }

        [Column("soldverge")]
        public double? Soldverge { get; set; }

        [Column("codvar")]
        public int? Codvar { get; set; }

        [Column("nomvar")]
        [StringLength(100)]
        public string Nomvar { get; set; }

        [Column("user")]
        [StringLength(50)]
        public string User { get; set; }

        [Column("station")]
        [StringLength(100)]
        public string Station { get; set; } = "COOPERATIVE ZAOUIA";

        [Column("activ")]
        [StringLength(150)]
        public string Activ { get; set; } = "Station de Conditionnement";

        [Column("camp")]
        [StringLength(150)]
        public string Camp { get; set; } = "24-25";

        [Column("dteupdate")]
        public DateTime? Dteupdate { get; set; }
    }
}
