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
    public class TbReceptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TbReceptionController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }
        // GET: api/stock/list
        [HttpGet("list")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult<IEnumerable<TbReception>>> GetAllStock()
        {
            var tbrecp = await _context.TbReceptions
                .OrderByDescending(s => s.Codvar + s.Refver)
                .ToListAsync();

            return Ok(tbrecp);
        }


        // GET: api/stock/stats
        [HttpGet("stats")]
        [Authorize(Roles = "super-user")]
        public async Task<ActionResult> GetTbrecpStats()
        {
            var totalItems = await _context.TbReceptions.CountAsync();
            //var totalPoidini = await _context.TbReceptions.SumAsync(s => s.Poidini ?? 0);
            var totalCumultg = await _context.TbReceptions.SumAsync(s => s.Pdrectotal ?? 0);
            var totalStockstat = await _context.TbReceptions.SumAsync(s => s.Stockstat ?? 0);

            var byVariete = await _context.TbReceptions
                .GroupBy(s => s.Codvar)
                .Select(g => new {
                    Codvar = g.Key,
                    Count = g.Count(),
                    TotalStock = g.Sum(s => s.Stockstat ?? 0)
                })
                .ToListAsync();

            var byVerger = await _context.TbReceptions
                .GroupBy(s => s.Refver)
                .Select(g => new {
                    Refver = g.Key,
                    Count = g.Count(),
                    TotalStock = g.Sum(s => s.Stockstat ?? 0)
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                //totalPoidini,
                totalCumultg,
                totalStockstat,
                byVariete,
                byVerger
            });
        }
    }
}
