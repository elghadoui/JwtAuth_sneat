using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApi.Models
{
    /// <summary>
    /// Modèle représentant l'entité 'tb_reception' (réception des produits).
    /// L'ID est la clé primaire unique (VARCHAR(50)).
    /// </summary>
    [Table("tb_reception")]
    public class TbReception
    {
        // -------------------------------------------------------------------
        // CLÉ PRIMAIRE
        // -------------------------------------------------------------------

        /// <summary>
        /// Identifiant unique (VARCHAR(50)). Clé Primaire.
        /// Généré via la concaténation dans la procédure stockée (refver + codvar + station).
        /// </summary>
        [Key]
        [Required]
        [StringLength(50)]
        public string Id { get; set; } = string.Empty;


        // -------------------------------------------------------------------
        // COLONNES COMPOSANTES DE L'ID (maintenant colonnes régulières)
        // -------------------------------------------------------------------

        /// <summary>
        /// Référence du verger (refver). INT NOT NULL.
        /// </summary>
        [Required]
        public int Refver { get; set; }

        /// <summary>
        /// Code de la variété (codvar). INT NOT NULL.
        /// </summary>
        [Required]
        public int Codvar { get; set; }

        /// <summary>
        /// Station (station). VARCHAR(50) NOT NULL.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Station { get; set; } = "zaouia"; // Valeur par défaut


        // -------------------------------------------------------------------
        // DONNÉES DESCRIPTIVES ET AGRÉGÉES
        // -------------------------------------------------------------------

        /// <summary>
        /// Nom du verger (nomver). VARCHAR(50) NULL.
        /// </summary>
        [StringLength(50)]
        public string? Nomver { get; set; }

        /// <summary>
        /// Producteur (producteur). VARCHAR(50) NULL.
        /// </summary>
        [StringLength(50)]
        public string? Producteur { get; set; }

        /// <summary>
        /// Nom de la variété (nomvar). VARCHAR(50) NULL.
        /// </summary>
        [StringLength(50)]
        public string? Nomvar { get; set; }

        /// <summary>
        /// Poids du produit pesé (pdpese). DOUBLE NULL DEFAULT '0'.
        /// </summary>
        [Column(TypeName = "double")]
        public double? Pdpese { get; set; } = 0.0;

        /// <summary>
        /// Poids reçu jour (pdrecjr). DOUBLE NULL DEFAULT '0'.
        /// </summary>
        [Column(TypeName = "double")]
        public double? Pdrecjr { get; set; } = 0.0;

        /// <summary>
        /// Poids reçu total (pdrectotal). DOUBLE NULL DEFAULT '0'.
        /// </summary>
        [Column(TypeName = "double")]
        public double? Pdrectotal { get; set; } = 0.0;

        /// <summary>
        /// Poids conditionné (pdcond). DOUBLE NULL DEFAULT '0'.
        /// </summary>
        [Column(TypeName = "double")]
        public double? Pdcond { get; set; } = 0.0;

        /// <summary>
        /// Stock statutaire (stockstat). DOUBLE NULL DEFAULT '0'.
        /// </summary>
        [Column(TypeName = "double")]
        public double? Stockstat { get; set; } = 0.0;

        /// <summary>
        /// Estimation (estima). DOUBLE NULL DEFAULT '0'.
        /// </summary>
        [Column(TypeName = "double")]
        public double? Estima { get; set; } = 0.0;

        /// <summary>
        /// Solde du verger (soldverg). DOUBLE NULL DEFAULT '0'.
        /// </summary>
        [Column(TypeName = "double")]
        public double? Soldverg { get; set; } = 0.0;

        /// <summary>
        /// Campagne (camp). VARCHAR(50) NULL.
        /// </summary>
        [StringLength(50)]
        public string? Camp { get; set; }

        /// <summary>
        /// Date de mise à jour (dtupdate). DATETIME NULL.
        /// </summary>
        public DateTime? Dtupdate { get; set; }
    }
}