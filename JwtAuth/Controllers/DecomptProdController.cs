using JwtAuthApi.Data;
using JwtAuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DecomptProdController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DecomptProdController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/decomptprod
        [HttpGet]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? station = null,
            [FromQuery] int? codvar = null,
            [FromQuery] string? nomadh = null,
            [FromQuery] string? sortBy = "date_creation",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.DecomptProds.AsQueryable();

                // Filtres de recherche générale
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d =>
                        d.Nomadh.Contains(search) ||
                        d.Nomvar.Contains(search) ||
                        d.Stations.Contains(search));
                }

                // Filtres de date
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.DateCreation >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.DateCreation <= dateTo.Value);
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(d => stationList.Any(st => d.Stations.Contains(st)));
                    }
                }

                // Filtre par code variété
                if (codvar.HasValue)
                {
                    query = query.Where(d => d.Codvar == codvar.Value);
                }

                // Filtre par nom adhérent
                if (!string.IsNullOrEmpty(nomadh))
                {
                    query = query.Where(d => d.Nomadh.Contains(nomadh));
                }

                // Tri
                query = sortBy?.ToLower() switch
                {
                    "nomadh" => sortOrder == "asc" ? query.OrderBy(d => d.Nomadh) : query.OrderByDescending(d => d.Nomadh),
                    "nomvar" => sortOrder == "asc" ? query.OrderBy(d => d.Nomvar) : query.OrderByDescending(d => d.Nomvar),
                    "pdreception" => sortOrder == "asc" ? query.OrderBy(d => d.PdReception) : query.OrderByDescending(d => d.PdReception),
                    "pdcond" => sortOrder == "asc" ? query.OrderBy(d => d.Pdcond) : query.OrderByDescending(d => d.Pdcond),
                    "date_creation" => sortOrder == "asc" ? query.OrderBy(d => d.DateCreation) : query.OrderByDescending(d => d.DateCreation),
                    _ => query.OrderByDescending(d => d.DateCreation)
                };

                var totalCount = await query.CountAsync();
                var data = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    data,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des données", error = ex.Message });
            }
        }

        // GET: api/decomptprod/stats/global
        [HttpGet("stats/global")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetGlobalStats(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.DecomptProds.AsQueryable();

                // Filtres de date
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.DateCreation >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.DateCreation <= dateTo.Value);
                }

                // Filtre par code variété
                if (codvar.HasValue)
                {
                    query = query.Where(d => d.Codvar == codvar.Value);
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(d => stationList.Any(st => d.Stations.Contains(st)));
                    }
                }

                var stats = await query
                    .GroupBy(d => 1)
                    .Select(g => new
                    {
                        totalEnregistrements = g.Count(),
                        poidsReceptionTotal = g.Sum(d => d.PdReception),
                        poidsCondTotal = g.Sum(d => d.Pdcond),
                        exportCatITotal = g.Sum(d => d.ExpCatI),
                        exportCatIITotal = g.Sum(d => d.ExpCatII),
                        poidsEcartTotal = g.Sum(d => d.PdEcart),
                        freinteTotal = g.Sum(d => d.Freinte),
                        nombreAdherents = g.Select(d => d.Nomadh).Distinct().Count(),
                        nombreVarietes = g.Select(d => d.Codvar).Distinct().Count(),
                        nombreStations = g.Select(d => d.Stations).Distinct().Count(),
                        tauxRendement = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.Pdcond) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0,
                        tauxFreinte = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.Freinte) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0
                    })
                    .FirstOrDefaultAsync();

                return Ok(stats ?? new
                {
                    totalEnregistrements = 0,
                    poidsReceptionTotal = 0m,
                    poidsCondTotal = 0m,
                    exportCatITotal = 0m,
                    exportCatIITotal = 0m,
                    poidsEcartTotal = 0m,
                    freinteTotal = 0m,
                    nombreAdherents = 0,
                    nombreVarietes = 0,
                    nombreStations = 0,
                    tauxRendement = 0m,
                    tauxFreinte = 0m
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du calcul des statistiques globales", error = ex.Message });
            }
        }

        // GET: api/decomptprod/stats/by-adherent
        [HttpGet("stats/by-adherent")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByAdherent(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.DecomptProds.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.DateCreation >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.DateCreation <= dateTo.Value);
                }

                if (codvar.HasValue)
                {
                    query = query.Where(d => d.Codvar == codvar.Value);
                }

                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(d => stationList.Any(st => d.Stations.Contains(st)));
                    }
                }

                var byAdherent = await query
                    .Where(d => d.Nomadh != null)
                    .GroupBy(d => new { d.Refver, d.Nomadh })
                    .Select(g => new
                    {
                        refver = g.Key.Refver,
                        nomadh = g.Key.Nomadh,
                        totalEnregistrements = g.Count(),
                        poidsReception = g.Sum(d => d.PdReception),
                        poidsCond = g.Sum(d => d.Pdcond),
                        exportCatI = g.Sum(d => d.ExpCatI),
                        exportCatII = g.Sum(d => d.ExpCatII),
                        poidsEcart = g.Sum(d => d.PdEcart),
                        freinte = g.Sum(d => d.Freinte),
                        tauxRendement = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.Pdcond) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0
                    })
                    .OrderByDescending(x => x.poidsReception)
                    .Take(limit)
                    .ToListAsync();

                return Ok(byAdherent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des stats par adhérent", error = ex.Message });
            }
        }

        // GET: api/decomptprod/stats/by-variete
        [HttpGet("stats/by-variete")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByVariete(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.DecomptProds.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.DateCreation >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.DateCreation <= dateTo.Value);
                }

                if (codvar.HasValue)
                {
                    query = query.Where(d => d.Codvar == codvar.Value);
                }

                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(d => stationList.Any(st => d.Stations.Contains(st)));
                    }
                }

                var byVariete = await query
                    .Where(d => d.Nomvar != null)
                    .GroupBy(d => new { d.Codvar, d.Nomvar })
                    .Select(g => new
                    {
                        codvar = g.Key.Codvar,
                        nomvar = g.Key.Nomvar,
                        totalEnregistrements = g.Count(),
                        poidsReception = g.Sum(d => d.PdReception),
                        poidsCond = g.Sum(d => d.Pdcond),
                        exportCatI = g.Sum(d => d.ExpCatI),
                        exportCatII = g.Sum(d => d.ExpCatII),
                        poidsEcart = g.Sum(d => d.PdEcart),
                        freinte = g.Sum(d => d.Freinte),
                        tauxRendement = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.Pdcond) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0,
                        tauxExportCatI = g.Sum(d => d.Pdcond) > 0
                            ? Math.Round((g.Sum(d => d.ExpCatI) / g.Sum(d => d.Pdcond)) * 100, 2)
                            : 0
                    })
                    .OrderByDescending(x => x.poidsReception)
                    .Take(limit)
                    .ToListAsync();

                return Ok(byVariete);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des stats par variété", error = ex.Message });
            }
        }

        // GET: api/decomptprod/stats/by-station
        [HttpGet("stats/by-station")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByStation(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.DecomptProds.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.DateCreation >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.DateCreation <= dateTo.Value);
                }

                if (codvar.HasValue)
                {
                    query = query.Where(d => d.Codvar == codvar.Value);
                }

                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(d => stationList.Any(st => d.Stations.Contains(st)));
                    }
                }

                var byStation = await query
                    .Where(d => d.Stations != null)
                    .GroupBy(d => d.Stations)
                    .Select(g => new
                    {
                        station = g.Key,
                        totalEnregistrements = g.Count(),
                        poidsReception = g.Sum(d => d.PdReception),
                        poidsCond = g.Sum(d => d.Pdcond),
                        exportCatI = g.Sum(d => d.ExpCatI),
                        exportCatII = g.Sum(d => d.ExpCatII),
                        poidsEcart = g.Sum(d => d.PdEcart),
                        freinte = g.Sum(d => d.Freinte),
                        nombreAdherents = g.Select(d => d.Nomadh).Distinct().Count(),
                        tauxRendement = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.Pdcond) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0
                    })
                    .OrderByDescending(x => x.poidsReception)
                    .ToListAsync();

                return Ok(byStation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des stats par station", error = ex.Message });
            }
        }

        // Endpoint timeline retiré car la table contient des cumuls avec date de mise à jour
        // L'évolution temporelle n'a pas de sens pour ce type de données

        // GET: api/decomptprod/stats/rendement
        [HttpGet("stats/rendement")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetRendementStats(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.DecomptProds.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.DateCreation >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.DateCreation <= dateTo.Value);
                }

                if (codvar.HasValue)
                {
                    query = query.Where(d => d.Codvar == codvar.Value);
                }

                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(d => stationList.Any(st => d.Stations.Contains(st)));
                    }
                }

                var stats = await query
                    .GroupBy(d => 1)
                    .Select(g => new
                    {
                        tauxRendementGlobal = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.Pdcond) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0,
                        tauxFreinteGlobal = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.Freinte) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0,
                        tauxEcartGlobal = g.Sum(d => d.PdReception) > 0
                            ? Math.Round((g.Sum(d => d.PdEcart) / g.Sum(d => d.PdReception)) * 100, 2)
                            : 0,
                        tauxExportCatI = g.Sum(d => d.Pdcond) > 0
                            ? Math.Round((g.Sum(d => d.ExpCatI) / g.Sum(d => d.Pdcond)) * 100, 2)
                            : 0,
                        tauxExportCatII = g.Sum(d => d.Pdcond) > 0
                            ? Math.Round((g.Sum(d => d.ExpCatII) / g.Sum(d => d.Pdcond)) * 100, 2)
                            : 0,
                        poidsReceptionTotal = g.Sum(d => d.PdReception),
                        poidsCondTotal = g.Sum(d => d.Pdcond),
                        freinteTotal = g.Sum(d => d.Freinte),
                        ecartTotal = g.Sum(d => d.PdEcart)
                    })
                    .FirstOrDefaultAsync();

                return Ok(stats ?? new
                {
                    tauxRendementGlobal = 0m,
                    tauxFreinteGlobal = 0m,
                    tauxEcartGlobal = 0m,
                    tauxExportCatI = 0m,
                    tauxExportCatII = 0m,
                    poidsReceptionTotal = 0m,
                    poidsCondTotal = 0m,
                    freinteTotal = 0m,
                    ecartTotal = 0m
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du calcul des statistiques de rendement", error = ex.Message });
            }
        }

        // GET: api/decomptprod/stats/export-categories
        [HttpGet("stats/export-categories")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetExportCategoriesStats(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.DecomptProds.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.DateCreation >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.DateCreation <= dateTo.Value);
                }

                if (codvar.HasValue)
                {
                    query = query.Where(d => d.Codvar == codvar.Value);
                }

                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(d => stationList.Any(st => d.Stations.Contains(st)));
                    }
                }

                var stats = await query
                    .GroupBy(d => 1)
                    .Select(g => new
                    {
                        categories = new[]
                        {
                            new { name = "Export Cat I", value = g.Sum(d => d.ExpCatI) },
                            new { name = "Export Cat II", value = g.Sum(d => d.ExpCatII) },
                            new { name = "Écart", value = g.Sum(d => d.PdEcart) },
                            new { name = "Freinte", value = g.Sum(d => d.Freinte) }
                        },
                        total = g.Sum(d => d.PdReception)
                    })
                    .FirstOrDefaultAsync();

                return Ok(stats ?? new
                {
                    categories = new[]
                    {
                        new { name = "Export Cat I", value = 0m },
                        new { name = "Export Cat II", value = 0m },
                        new { name = "Écart", value = 0m },
                        new { name = "Freinte", value = 0m }
                    },
                    total = 0m
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du calcul des statistiques d'export par catégories", error = ex.Message });
            }
        }

        // GET: api/decomptprod/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<DecomptProd>> GetById(string id)
        {
            try
            {
                var decomptProd = await _context.DecomptProds.FindAsync(id);

                if (decomptProd == null)
                {
                    return NotFound(new { message = "Enregistrement non trouvé" });
                }

                return Ok(decomptProd);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'enregistrement", error = ex.Message });
            }
        }
    }
}
