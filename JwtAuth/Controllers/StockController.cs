// Controllers/StockController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApi.Data;
using JwtAuthApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;

namespace JwtAuthApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public StockController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: api/stock/list
        [HttpGet("list")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<IEnumerable<Stock>>> GetAllStock()
        {
            var stocks = await _context.Stocks
                .OrderByDescending(s => s.Codvar+s.Refverreel)
                .ToListAsync();

            return Ok(stocks);
        }

        // GET: api/stock/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<Stock>> GetStock(int id)
        {
            var stock = await _context.Stocks.FindAsync(id);

            if (stock == null)
            {
                return NotFound(new { message = "Article de stock non trouvé" });
            }

            return Ok(stock);
        }

        // POST: api/stock/sync
        // Synchroniser les données depuis l'API externe
        [HttpPost("sync")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> SyncFromExternalApi([FromBody] SyncRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(request.ApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Échec de récupération des données depuis l'API externe" });
                }

                var content = await response.Content.ReadAsStringAsync();
                var externalData = JsonSerializer.Deserialize<List<ExternalStockData>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (externalData == null || !externalData.Any())
                {
                    return BadRequest(new { message = "Aucune donnée reçue de l'API externe" });
                }

                int itemsAdded = 0;
                int itemsUpdated = 0;

                // Transformer et sauvegarder les données
                foreach (var item in externalData)
                {
                    var stock = new Stock
                    {
                        Refver = item.refver,
                        Refverreel = item.refverreel,
                        Nomprod = item.nomprod,
                        Nomver = item.nomver,
                        Poidini = item.poidini,
                        Pdjr = item.pdjr,
                        Cumultg = item.cumultg,
                        Stockstat = item.stockstat,
                        Estimat = item.estimat,
                        Soldverge = item.soldverge,
                        Codvar = item.codvar,
                        Nomvar = item.nomvar,
                        User = item.user,
                        Station = item.station ?? "COOPERATIVE ZAOUIA",
                        Activ = item.activ ?? "Station de Conditionnement",
                        Camp = item.camp ?? "24-25",
                        Dteupdate = DateTime.Now
                    };

                    // Vérifier si l'article existe déjà (par Refver et Nomprod)
                    var existing = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.Refver == stock.Refver && s.Nomprod == stock.Nomprod);

                    if (existing != null)
                    {
                        // Mettre à jour
                        existing.Refverreel = stock.Refverreel;
                        existing.Nomver = stock.Nomver;
                        existing.Poidini = stock.Poidini;
                        existing.Pdjr = stock.Pdjr;
                        existing.Cumultg = stock.Cumultg;
                        existing.Stockstat = stock.Stockstat;
                        existing.Estimat = stock.Estimat;
                        existing.Soldverge = stock.Soldverge;
                        existing.Codvar = stock.Codvar;
                        existing.Nomvar = stock.Nomvar;
                        existing.User = stock.User;
                        existing.Station = stock.Station;
                        existing.Activ = stock.Activ;
                        existing.Camp = stock.Camp;
                        existing.Dteupdate = DateTime.Now;

                        itemsUpdated++;
                    }
                    else
                    {
                        // Ajouter nouveau
                        _context.Stocks.Add(stock);
                        itemsAdded++;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Stock synchronisé avec succès",
                    itemsProcessed = externalData.Count,
                    itemsAdded = itemsAdded,
                    itemsUpdated = itemsUpdated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la synchronisation: {ex.Message}" });
            }
        }

        // POST: api/stock/create
        [HttpPost("create")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<Stock>> CreateStock([FromBody] StockCreateDto stockDto)
        {
            var stock = new Stock
            {
                Refver = stockDto.Refver,
                Refverreel = stockDto.Refverreel,
                Nomprod = stockDto.Nomprod,
                Nomver = stockDto.Nomver,
                Poidini = stockDto.Poidini,
                Pdjr = stockDto.Pdjr,
                Cumultg = stockDto.Cumultg,
                Stockstat = stockDto.Stockstat,
                Estimat = stockDto.Estimat,
                Soldverge = stockDto.Soldverge,
                Codvar = stockDto.Codvar,
                Nomvar = stockDto.Nomvar,
                User = stockDto.User,
                Station = stockDto.Station ?? "COOPERATIVE ZAOUIA",
                Activ = stockDto.Activ ?? "Station de Conditionnement",
                Camp = stockDto.Camp ?? "24-25",
                Dteupdate = DateTime.Now
            };

            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStock), new { id = stock.Id }, stock);
        }

        // PUT: api/stock/update/{id}
        [HttpPut("update/{id}")]
        [Authorize(Roles = "super-user")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateDto stockDto)
        {
            var stock = await _context.Stocks.FindAsync(id);

            if (stock == null)
            {
                return NotFound(new { message = "Article de stock non trouvé" });
            }

            stock.Refver = stockDto.Refver;
            stock.Refverreel = stockDto.Refverreel;
            stock.Nomprod = stockDto.Nomprod;
            stock.Nomver = stockDto.Nomver;
            stock.Poidini = stockDto.Poidini;
            stock.Pdjr = stockDto.Pdjr;
            stock.Cumultg = stockDto.Cumultg;
            stock.Stockstat = stockDto.Stockstat;
            
            stock.Estimat = stockDto.Estimat;
            stock.Soldverge = stockDto.Soldverge;
            stock.Codvar = stockDto.Codvar;
            stock.Nomvar = stockDto.Nomvar;
            stock.User = stockDto.User;
            stock.Station = stockDto.Station;
            stock.Activ = stockDto.Activ;
            stock.Camp = stockDto.Camp;
            stock.Dteupdate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock mis à jour avec succès" });
        }

        // DELETE: api/stock/delete/{id}
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "super-user")]
        public async Task<IActionResult> DeleteStock(int id)
        {
            var stock = await _context.Stocks.FindAsync(id);

            if (stock == null)
            {
                return NotFound(new { message = "Article de stock non trouvé" });
            }

            _context.Stocks.Remove(stock);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock supprimé avec succès" });
        }

        // GET: api/stock/stats
        [HttpGet("stats")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetStockStats()
        {
            var totalItems = await _context.Stocks.CountAsync();
            var totalPoidini = await _context.Stocks.SumAsync(s => s.Poidini ?? 0);
            var totalCumultg = await _context.Stocks.SumAsync(s => s.Cumultg ?? 0);
            var totalStockstat = await _context.Stocks.SumAsync(s => s.Stockstat ?? 0);

            var byVariete = await _context.Stocks
                .GroupBy(s => s.Nomvar)
                .Select(g => new {
                    Variete = g.Key,
                    Count = g.Count(),
                    TotalStock = g.Sum(s => s.Stockstat ?? 0)
                })
                .ToListAsync();

            var byVerger = await _context.Stocks
                .GroupBy(s => s.Nomver)
                .Select(g => new {
                    Verger = g.Key,
                    Count = g.Count(),
                    TotalStock = g.Sum(s => s.Stockstat ?? 0)
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                totalPoidini,
                totalCumultg,
                totalStockstat,
                byVariete,
                byVerger
            });
        }

        // GET: api/stock/filter
        [HttpGet("filter")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> FilterStock(
            [FromQuery] string camp = null,
            [FromQuery] string nomvar = null,
            [FromQuery] string nomver = null,
            [FromQuery] string station = null)
        {
            var query = _context.Stocks.AsQueryable();

            if (!string.IsNullOrEmpty(camp))
                query = query.Where(s => s.Camp == camp);

            if (!string.IsNullOrEmpty(nomvar))
                query = query.Where(s => s.Nomvar == nomvar);

            if (!string.IsNullOrEmpty(nomver))
                query = query.Where(s => s.Nomver == nomver);

            if (!string.IsNullOrEmpty(station))
                query = query.Where(s => s.Station == station);

            var results = await query
                .OrderByDescending(s => s.Dteupdate)
                .ToListAsync();

            return Ok(results);
        }
    }

    // DTOs
    public class SyncRequest
    {
        public string ApiUrl { get; set; }
    }

    public class ExternalStockData
    {
        public int? refver { get; set; }
        public int? refverreel { get; set; }
        public string nomprod { get; set; }
        public string nomver { get; set; }
        public double? poidini { get; set; }
        public double? pdjr { get; set; }
        public double? cumultg { get; set; }
        public double? stockstat { get; set; }
        public double? estimat { get; set; }
        public double? soldverge { get; set; }
        public int? codvar { get; set; }
        public string nomvar { get; set; }
        public string user { get; set; }
        public string station { get; set; }
        public string activ { get; set; }
        public string camp { get; set; }
    }

    public class StockCreateDto
    {
        public int? Refver { get; set; }
        public int? Refverreel { get; set; }
        public string Nomprod { get; set; }
        public string Nomver { get; set; }
        public double? Poidini { get; set; }
        public double? Pdjr { get; set; }
        public double? Cumultg { get; set; }
        public double? Stockstat { get; set; }
        public double? Estimat { get; set; }
        public double? Soldverge { get; set; }
        public int? Codvar { get; set; }
        public string Nomvar { get; set; }
        public string User { get; set; }
        public string Station { get; set; }
        public string Activ { get; set; }
        public string Camp { get; set; }
    }

    public class StockUpdateDto
    {
        public int? Refver { get; set; }
        public int? Refverreel { get; set; }
        public string Nomprod { get; set; }
        public string Nomver { get; set; }
        public double? Poidini { get; set; }
        public double? Pdjr { get; set; }
        public double? Cumultg { get; set; }
        public double? Stockstat { get; set; }
        public double? Estimat { get; set; }
        public double? Soldverge { get; set; }
        public int? Codvar { get; set; }
        public string Nomvar { get; set; }
        public string User { get; set; }
        public string Station { get; set; }
        public string Activ { get; set; }
        public string Camp { get; set; }
    }
}
