using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApi.Data;
using JwtAuthApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DossierExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DossierExportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/dossierexport/list
        [HttpGet("list")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<object>> GetAllDossiers(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? navire,
            [FromQuery] string? codpay,
            [FromQuery] string? station,
            [FromQuery] string? rsclient,
            [FromQuery] string? codvar,
            [FromQuery] string? refexp,
            [FromQuery] string? exporter,
            [FromQuery] string? search,
            [FromQuery] string sortBy = "dtedep",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par date
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                // Filtrage par navire
                if (!string.IsNullOrWhiteSpace(navire))
                {
                    query = query.Where(d => d.Navire != null && d.Navire.Contains(navire));
                }

                // Filtrage par pays
                if (!string.IsNullOrWhiteSpace(codpay))
                {
                    query = query.Where(d => d.Codpay != null && d.Codpay.Contains(codpay));
                }

                // Filtrage par station
                if (!string.IsNullOrWhiteSpace(station))
                {
                    query = query.Where(d => d.Stations != null && d.Stations.Contains(station));
                }

                // Filtrage par client
                if (!string.IsNullOrWhiteSpace(rsclient))
                {
                    query = query.Where(d => d.Rsclient != null && d.Rsclient.Contains(rsclient));
                }

                // Filtrage par variété
                if (!string.IsNullOrWhiteSpace(codvar))
                {
                    query = query.Where(d => d.Codvar != null && d.Codvar.Contains(codvar));
                }

                // Filtrage par référence exportateur
                if (!string.IsNullOrWhiteSpace(refexp))
                {
                    query = query.Where(d => d.Refexp != null && d.Refexp.Contains(refexp));
                }

                // Filtrage par exportateur
                if (!string.IsNullOrWhiteSpace(exporter))
                {
                    query = query.Where(d => d.Exporter != null && d.Exporter.Contains(exporter));
                }

                // Recherche globale
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(d =>
                        (d.Numdos != null && d.Numdos.Contains(search)) ||
                        (d.Numtc != null && d.Numtc.Contains(search)) ||
                        (d.Refexp != null && d.Refexp.Contains(search)) ||
                        (d.Exporter != null && d.Exporter.Contains(search)) ||
                        (d.Navire != null && d.Navire.Contains(search)) ||
                        (d.Rsclient != null && d.Rsclient.Contains(search))
                    );
                }

                // Tri
                query = sortBy.ToLower() switch
                {
                    "dtedep" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(d => d.Dtedep)
                        : query.OrderByDescending(d => d.Dtedep),
                    "numdos" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(d => d.Numdos)
                        : query.OrderByDescending(d => d.Numdos),
                    "navire" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(d => d.Navire)
                        : query.OrderByDescending(d => d.Navire),
                    "nompay" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(d => d.Nompay)
                        : query.OrderByDescending(d => d.Nompay),
                    _ => query.OrderByDescending(d => d.Dtedep)
                };

                // Compter le total avant pagination
                var totalCount = await query.CountAsync();

                // Pagination
                var dossiers = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    data = dossiers,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des dossiers", error = ex.Message });
            }
        }

        // GET: api/dossierexport/stats
        [HttpGet("stats")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetGlobalStats(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? station)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par période
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                // Filtrage par station
                if (!string.IsNullOrWhiteSpace(station))
                {
                    query = query.Where(d => d.Stations != null && d.Stations.Contains(station));
                }

                var totalDossiers = await query.Select(d => d.Numdos).Distinct().CountAsync();
                var totalPalettes = await query.SumAsync(d => d.Nbrpal ?? 0);
                var totalColis = await query.SumAsync(d => d.Nbrcol ?? 0);
                var totalPoids = await query.SumAsync(d => d.Pdscom ?? 0);
                var navireCount = await query.Where(d => d.Navire != null).Select(d => d.Navire).Distinct().CountAsync();
                var paysCount = await query.Where(d => d.Codpay != null).Select(d => d.Codpay).Distinct().CountAsync();
                var clientsCount = await query.Where(d => d.Rsclient != null).Select(d => d.Rsclient).Distinct().CountAsync();
                var lastExportDate = await query.MaxAsync(d => (DateTime?)d.Dtedep);

                return Ok(new
                {
                    totalDossiers,
                    totalPalettes,
                    totalColis,
                    totalPoids,
                    navireCount,
                    paysCount,
                    clientsCount,
                    lastExportDate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques", error = ex.Message });
            }
        }

        // GET: api/dossierexport/stats/timeline
        [HttpGet("stats/timeline")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetTimelineStats(
            [FromQuery] string period = "month",
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par période
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                var data = await query.ToListAsync();

                object result;

                switch (period.ToLower())
                {
                    case "day":
                        result = data
                            .Where(d => d.Dtedep.HasValue)
                            .GroupBy(d => d.Dtedep!.Value.Date)
                            .Select(g => new
                            {
                                period = g.Key.ToString("yyyy-MM-dd"),
                                totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                                totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                                totalColis = g.Sum(x => x.Nbrcol ?? 0),
                                totalPoids = g.Sum(x => x.Pdscom ?? 0)
                            })
                            .OrderBy(x => x.period)
                            .ToList();
                        break;

                    case "week":
                        result = data
                            .Where(d => d.Dtedep.HasValue)
                            .GroupBy(d => new {
                                Year = d.Dtedep!.Value.Year,
                                Week = System.Globalization.ISOWeek.GetWeekOfYear(d.Dtedep.Value)
                            })
                            .Select(g => new
                            {
                                period = $"{g.Key.Year}-W{g.Key.Week:D2}",
                                totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                                totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                                totalColis = g.Sum(x => x.Nbrcol ?? 0),
                                totalPoids = g.Sum(x => x.Pdscom ?? 0)
                            })
                            .OrderBy(x => x.period)
                            .ToList();
                        break;

                    case "month":
                        result = data
                            .Where(d => d.Dtedep.HasValue)
                            .GroupBy(d => new {
                                Year = d.Dtedep!.Value.Year,
                                Month = d.Dtedep.Value.Month
                            })
                            .Select(g => new
                            {
                                period = $"{g.Key.Year}-{g.Key.Month:D2}",
                                totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                                totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                                totalColis = g.Sum(x => x.Nbrcol ?? 0),
                                totalPoids = g.Sum(x => x.Pdscom ?? 0)
                            })
                            .OrderBy(x => x.period)
                            .ToList();
                        break;

                    default:
                        result = new List<object>();
                        break;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques temporelles", error = ex.Message });
            }
        }

        // GET: api/dossierexport/stats/by-country
        [HttpGet("stats/by-country")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByCountry(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par période
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                var byCountry = await query
                    .Where(d => d.Codpay != null)
                    .GroupBy(d => new { d.Codpay, d.Nompay })
                    .Select(g => new
                    {
                        codpay = g.Key.Codpay,
                        nompay = g.Key.Nompay,
                        totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                        totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                        totalColis = g.Sum(x => x.Nbrcol ?? 0),
                        totalPoids = g.Sum(x => x.Pdscom ?? 0)
                    })
                    .OrderByDescending(x => x.totalPoids)
                    .Take(limit)
                    .ToListAsync();

                return Ok(byCountry);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques par pays", error = ex.Message });
            }
        }

        // GET: api/dossierexport/stats/by-product
        [HttpGet("stats/by-product")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByProduct(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par période
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                var byProduct = await query
                    .Where(d => d.Codvar != null)
                    .GroupBy(d => new { d.Codvar, d.Produit })
                    .Select(g => new
                    {
                        codvar = g.Key.Codvar,
                        produit = g.Key.Produit,
                        totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                        totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                        totalColis = g.Sum(x => x.Nbrcol ?? 0),
                        totalPoids = g.Sum(x => x.Pdscom ?? 0)
                    })
                    .OrderByDescending(x => x.totalPoids)
                    .Take(limit)
                    .ToListAsync();

                return Ok(byProduct);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques par produit", error = ex.Message });
            }
        }

        // GET: api/dossierexport/stats/by-navire
        [HttpGet("stats/by-navire")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByNavire(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par période
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                var byNavire = await query
                    .Where(d => d.Navire != null)
                    .GroupBy(d => d.Navire)
                    .Select(g => new
                    {
                        navire = g.Key,
                        totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                        totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                        totalPoids = g.Sum(x => x.Pdscom ?? 0),
                        lastDeparture = g.Max(x => x.Dtedep)
                    })
                    .OrderByDescending(x => x.lastDeparture)
                    .ToListAsync();

                return Ok(byNavire);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques par navire", error = ex.Message });
            }
        }

        // GET: api/dossierexport/stats/by-station
        [HttpGet("stats/by-station")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByStation(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par période
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                var byStation = await query
                    .Where(d => d.Stations != null)
                    .GroupBy(d => d.Stations)
                    .Select(g => new
                    {
                        station = g.Key,
                        totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                        totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                        totalColis = g.Sum(x => x.Nbrcol ?? 0),
                        totalPoids = g.Sum(x => x.Pdscom ?? 0)
                    })
                    .OrderByDescending(x => x.totalPoids)
                    .ToListAsync();

                return Ok(byStation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques par station", error = ex.Message });
            }
        }

        // GET: api/dossierexport/stats/by-client
        [HttpGet("stats/by-client")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByClient(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.DossierExports.AsQueryable();

                // Filtrage par période
                if (dateFrom.HasValue)
                {
                    query = query.Where(d => d.Dtedep >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(d => d.Dtedep <= dateTo.Value);
                }

                var byClient = await query
                    .Where(d => d.Rsclient != null)
                    .GroupBy(d => d.Rsclient)
                    .Select(g => new
                    {
                        client = g.Key,
                        rsclient = g.Key,
                        totalDossiers = g.Select(x => x.Numdos).Distinct().Count(),
                        totalPalettes = g.Sum(x => x.Nbrpal ?? 0),
                        totalColis = g.Sum(x => x.Nbrcol ?? 0),
                        totalPoids = g.Sum(x => x.Pdscom ?? 0)
                    })
                    .OrderByDescending(x => x.totalPoids)
                    .Take(limit)
                    .ToListAsync();

                return Ok(byClient);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques par client", error = ex.Message });
            }
        }

        // GET: api/dossierexport/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<DossierExport>> GetDossierById(string id)
        {
            try
            {
                var dossier = await _context.DossierExports.FindAsync(id);

                if (dossier == null)
                {
                    return NotFound(new { message = "Dossier non trouvé" });
                }

                return Ok(dossier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération du dossier", error = ex.Message });
            }
        }
    }
}
