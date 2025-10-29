using JwtAuthApi.Data;
using JwtAuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RapportVenteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RapportVenteController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/rapportvente
        [HttpGet]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? station = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? refach = null,
            [FromQuery] string? codtype = null,
            [FromQuery] string? sortBy = "date_vente",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                // Filtres de recherche
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(v =>
                        v.NumVnt.Contains(search) ||
                        v.Acheteurs.Contains(search) ||
                        v.Varietes.Contains(search) ||
                        v.RefAch.Contains(search));
                }

                // Filtres de date
                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                if (!string.IsNullOrEmpty(codvar))
                {
                    // Support pour plusieurs variétés séparées par des virgules
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                if (!string.IsNullOrEmpty(refach))
                {
                    query = query.Where(v => v.RefAch.Contains(refach));
                }

                if (!string.IsNullOrEmpty(codtype))
                {
                    query = query.Where(v => v.CodType.Contains(codtype));
                }

                // Tri
                query = sortBy?.ToLower() switch
                {
                    "numvnt" => sortOrder == "asc" ? query.OrderBy(v => v.NumVnt) : query.OrderByDescending(v => v.NumVnt),
                    "date_vente" => sortOrder == "asc" ? query.OrderBy(v => v.DateVente) : query.OrderByDescending(v => v.DateVente),
                    "acheteurs" => sortOrder == "asc" ? query.OrderBy(v => v.Acheteurs) : query.OrderByDescending(v => v.Acheteurs),
                    "montant_vente" => sortOrder == "asc" ? query.OrderBy(v => v.MontantVente) : query.OrderByDescending(v => v.MontantVente),
                    _ => query.OrderByDescending(v => v.DateVente)
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
                return StatusCode(500, new { message = "Erreur lors de la récupération des ventes", error = ex.Message });
            }
        }

        // GET: api/rapportvente/stats/global
        [HttpGet("stats/global")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetGlobalStats(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par variétés multiples
                if (!string.IsNullOrEmpty(codvar))
                {
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                var stats = await query
                    .GroupBy(v => 1)
                    .Select(g => new
                    {
                        totalVentes = g.Count(),
                        poidsTotalBrut = g.Sum(v => v.PoidBrut ?? 0),
                        poidsTotalPese = g.Sum(v => v.PoidPese ?? 0),
                        chiffreAffaires = g.Sum(v => v.MontantVente ?? 0),
                        montantRegle = g.Sum(v => v.MontantReglement),
                        soldeRestant = g.Sum(v => v.SoldVente ?? 0),
                        nombreAcheteurs = g.Select(v => v.RefAch).Distinct().Count(),
                        nombreStations = g.Select(v => v.Station).Distinct().Count(),
                        nombreVarietes = g.Select(v => v.CodVar).Distinct().Count(),
                        prixMoyenKg = g.Average(v => v.PrxKg ?? 0)
                    })
                    .FirstOrDefaultAsync();

                return Ok(stats ?? new
                {
                    totalVentes = 0,
                    poidsTotalBrut = 0m,
                    poidsTotalPese = 0m,
                    chiffreAffaires = 0m,
                    montantRegle = 0m,
                    soldeRestant = 0m,
                    nombreAcheteurs = 0,
                    nombreStations = 0,
                    nombreVarietes = 0,
                    prixMoyenKg = 0m
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du calcul des statistiques", error = ex.Message });
            }
        }

        // GET: api/rapportvente/stats/timeline
        [HttpGet("stats/timeline")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetTimelineStats(
            [FromQuery] string period = "week",
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par variétés multiples
                if (!string.IsNullOrEmpty(codvar))
                {
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                var data = await query.ToListAsync();

                var grouped = period.ToLower() switch
                {
                    "day" => data.GroupBy(v => v.DateVente.HasValue ? v.DateVente.Value.Date : DateTime.MinValue)
                                .Select(g => new
                                {
                                    period = g.Key.ToString("dd/MM/yyyy"),
                                    totalVentes = g.Count(),
                                    poidsPese = g.Sum(v => v.PoidPese ?? 0),
                                    chiffreAffaires = g.Sum(v => v.MontantVente ?? 0),
                                    montantRegle = g.Sum(v => v.MontantReglement)
                                })
                                .OrderBy(x => x.period)
                                .ToList(),

                    "month" => data.GroupBy(v => v.DateVente.HasValue ? new DateTime(v.DateVente.Value.Year, v.DateVente.Value.Month, 1) : DateTime.MinValue)
                                .Select(g => new
                                {
                                    period = g.Key.ToString("MM/yyyy"),
                                    totalVentes = g.Count(),
                                    poidsPese = g.Sum(v => v.PoidPese ?? 0),
                                    chiffreAffaires = g.Sum(v => v.MontantVente ?? 0),
                                    montantRegle = g.Sum(v => v.MontantReglement)
                                })
                                .OrderBy(x => x.period)
                                .ToList(),

                    _ => data.GroupBy(v => v.DateVente.HasValue ?
                                    System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                        v.DateVente.Value,
                                        System.Globalization.CalendarWeekRule.FirstDay,
                                        DayOfWeek.Monday) : 0)
                                .Select(g => new
                                {
                                    period = $"Semaine {g.Key}",
                                    totalVentes = g.Count(),
                                    poidsPese = g.Sum(v => v.PoidPese ?? 0),
                                    chiffreAffaires = g.Sum(v => v.MontantVente ?? 0),
                                    montantRegle = g.Sum(v => v.MontantReglement)
                                })
                                .OrderBy(x => x.period)
                                .ToList()
                };

                return Ok(grouped);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'évolution", error = ex.Message });
            }
        }

        // GET: api/rapportvente/stats/by-station
        [HttpGet("stats/by-station")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByStation(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par variétés multiples
                if (!string.IsNullOrEmpty(codvar))
                {
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                var byStation = await query
                    .Where(v => v.Station != null)
                    .GroupBy(v => v.Station)
                    .Select(g => new
                    {
                        station = g.Key,
                        totalVentes = g.Count(),
                        poidsPese = g.Sum(v => v.PoidPese ?? 0),
                        chiffreAffaires = g.Sum(v => v.MontantVente ?? 0),
                        montantRegle = g.Sum(v => v.MontantReglement),
                        soldeRestant = g.Sum(v => v.SoldVente ?? 0)
                    })
                    .OrderByDescending(x => x.chiffreAffaires)
                    .ToListAsync();

                return Ok(byStation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des stats par station", error = ex.Message });
            }
        }

        // GET: api/rapportvente/stats/by-variete
        [HttpGet("stats/by-variete")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByVariete(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par variétés multiples
                if (!string.IsNullOrEmpty(codvar))
                {
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                var byVariete = await query
                    .Where(v => v.Varietes != null || v.CodVar != null)
                    .GroupBy(v => new { v.CodVar, v.Varietes })
                    .Select(g => new
                    {
                        codvar = g.Key.CodVar,
                        varietes = g.Key.Varietes,
                        totalVentes = g.Count(),
                        poidsPese = g.Sum(v => v.PoidPese ?? 0),
                        chiffreAffaires = g.Sum(v => v.MontantVente ?? 0),
                        prixMoyen = g.Average(v => v.PrxKg ?? 0)
                    })
                    .OrderByDescending(x => x.poidsPese)
                    .Take(limit)
                    .ToListAsync();

                return Ok(byVariete);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des stats par variété", error = ex.Message });
            }
        }

        // GET: api/rapportvente/stats/by-acheteur
        [HttpGet("stats/by-acheteur")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStatsByAcheteur(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par variétés multiples
                if (!string.IsNullOrEmpty(codvar))
                {
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                var byAcheteur = await query
                    .Where(v => v.Acheteurs != null || v.RefAch != null)
                    .GroupBy(v => new { v.RefAch, v.Acheteurs })
                    .Select(g => new
                    {
                        refach = g.Key.RefAch,
                        acheteurs = g.Key.Acheteurs,
                        totalVentes = g.Count(),
                        poidsPese = g.Sum(v => v.PoidPese ?? 0),
                        chiffreAffaires = g.Sum(v => v.MontantVente ?? 0),
                        montantRegle = g.Sum(v => v.MontantReglement),
                        soldeRestant = g.Sum(v => v.SoldVente ?? 0)
                    })
                    .OrderByDescending(x => x.chiffreAffaires)
                    .Take(limit)
                    .ToListAsync();

                return Ok(byAcheteur);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des stats par acheteur", error = ex.Message });
            }
        }

        // GET: api/rapportvente/stats/average-price
        [HttpGet("stats/average-price")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetAveragePriceByTypeAndVariete(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par variétés multiples
                if (!string.IsNullOrEmpty(codvar))
                {
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                var averagePrices = await query
                    .Where(v => v.TypeEcart != null && v.Varietes != null && v.PrxKg.HasValue)
                    .GroupBy(v => new { v.TypeEcart, v.Varietes, v.CodVar })
                    .Select(g => new
                    {
                        typeEcart = g.Key.TypeEcart,
                        varietes = g.Key.Varietes,
                        codvar = g.Key.CodVar,
                        prixMoyen = g.Average(v => v.PrxKg ?? 0),
                        poidsPese = g.Sum(v => v.PoidPese ?? 0),
                        nombreVentes = g.Count()
                    })
                    .OrderByDescending(x => x.poidsPese)
                    .ToListAsync();

                return Ok(averagePrices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des prix moyens", error = ex.Message });
            }
        }

        // GET: api/rapportvente/stats/price-timeline
        [HttpGet("stats/price-timeline")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetPriceTimelineStats(
            [FromQuery] string period = "week",
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? codvar = null,
            [FromQuery] string? station = null)
        {
            try
            {
                var query = _context.RapportVentes.AsQueryable();

                if (dateFrom.HasValue)
                {
                    query = query.Where(v => v.DateVente >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(v => v.DateVente <= dateTo.Value);
                }

                // Filtre par variétés multiples
                if (!string.IsNullOrEmpty(codvar))
                {
                    var codvarList = codvar.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(c => c.Trim())
                                          .ToList();

                    if (codvarList.Any())
                    {
                        query = query.Where(v => codvarList.Any(cv => v.CodVar.Contains(cv)));
                    }
                }

                // Filtre par stations multiples
                if (!string.IsNullOrEmpty(station))
                {
                    var stationList = station.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();

                    if (stationList.Any())
                    {
                        query = query.Where(v => stationList.Any(st => v.Station.Contains(st)));
                    }
                }

                var data = await query.Where(v => v.PrxKg.HasValue && v.PrxKg > 0).ToListAsync();

                if (!data.Any())
                {
                    return Ok(new List<object>());
                }

                var grouped = period.ToLower() switch
                {
                    "day" => data.GroupBy(v => v.DateVente.HasValue ? v.DateVente.Value.Date : DateTime.MinValue)
                                .Select(g => new
                                {
                                    period = g.Key.ToString("dd/MM/yyyy"),
                                    prixMoyen = Math.Round(g.Average(v => v.PrxKg.Value), 2)
                                })
                                .OrderBy(x => x.period)
                                .ToList(),

                    "month" => data.GroupBy(v => v.DateVente.HasValue ? new DateTime(v.DateVente.Value.Year, v.DateVente.Value.Month, 1) : DateTime.MinValue)
                                .Select(g => new
                                {
                                    period = g.Key.ToString("MM/yyyy"),
                                    prixMoyen = Math.Round(g.Average(v => v.PrxKg.Value), 2)
                                })
                                .OrderBy(x => x.period)
                                .ToList(),

                    _ => data.GroupBy(v => v.DateVente.HasValue ?
                                    System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                        v.DateVente.Value,
                                        System.Globalization.CalendarWeekRule.FirstDay,
                                        DayOfWeek.Monday) : 0)
                                .Select(g => new
                                {
                                    period = $"Semaine {g.Key}",
                                    prixMoyen = Math.Round(g.Average(v => v.PrxKg.Value), 2)
                                })
                                .OrderBy(x => x.period)
                                .ToList()
                };

                return Ok(grouped);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'évolution des prix", error = ex.Message });
            }
        }

        // GET: api/rapportvente/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<RapportVente>> GetById(string id)
        {
            try
            {
                var vente = await _context.RapportVentes.FindAsync(id);

                if (vente == null)
                {
                    return NotFound(new { message = "Vente non trouvée" });
                }

                return Ok(vente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération de la vente", error = ex.Message });
            }
        }
    }
}
