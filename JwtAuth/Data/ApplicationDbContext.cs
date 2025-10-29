using JwtAuthApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace JwtAuthApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Stock> Stocks { get; set; }
        public DbSet<TbReception> TbReceptions { get; set; }
        public DbSet<DossierExport> DossierExports { get; set; }
        public DbSet<RapportVente> RapportVentes { get; set; }
        public DbSet<DecomptProd> DecomptProds { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser entity
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
            });
            builder.Entity<TbReception>()
                .Property(e => e.Id)
                .HasMaxLength(50);
            // Configuration pour DossierExport
            builder.Entity<DossierExport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Numdos).HasDatabaseName("idx_numdos");
                entity.HasIndex(e => e.Dtedep).HasDatabaseName("idx_dtedep");
                entity.HasIndex(e => e.Navire).HasDatabaseName("idx_navire");
                entity.HasIndex(e => e.Nompay).HasDatabaseName("idx_nompay");
                entity.HasIndex(e => e.Codpay).HasDatabaseName("idx_codpay");
                entity.HasIndex(e => e.Coddes).HasDatabaseName("idx_coddes");
                entity.HasIndex(e => e.Codvar).HasDatabaseName("idx_codvar");
                entity.HasIndex(e => e.Stations).HasDatabaseName("idx_stations");

                entity.Property(e => e.Nbrpal).HasPrecision(10, 2);
                entity.Property(e => e.Nbrcol).HasPrecision(10, 2);
                entity.Property(e => e.Pdscom).HasPrecision(15, 3);
                entity.Property(e => e.Stations).HasDefaultValue("zaouia");
                entity.Property(e => e.DateCreation).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
            builder.Entity<DecomptProd>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DateCreation)
                     .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
            // Configuration pour RapportVente
            builder.Entity<RapportVente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NumVnt).HasDatabaseName("idx_numvnt");
                entity.HasIndex(e => e.DateVente).HasDatabaseName("idx_datevente");
                entity.HasIndex(e => e.Station).HasDatabaseName("idx_station_vente");
                entity.HasIndex(e => e.CodVar).HasDatabaseName("idx_codvar_vente");
                entity.HasIndex(e => e.RefAch).HasDatabaseName("idx_refach");

                entity.Property(e => e.Station).HasDefaultValue("zaouia");
                entity.Property(e => e.MontantReglement).HasDefaultValue(0);
                entity.Property(e => e.DateCreation).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.DateMiseAJour).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}