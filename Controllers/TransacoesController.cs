using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DashboardFinanceiroPessoal.Data;
using DashboardFinanceiroPessoal.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DashboardFinanceiroPessoal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransacoesController : ControllerBase
    {
        private readonly AppDbContext _context; // Variável que guarda a conexão com o banco de dados.

        public TransacoesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transacao>>> GetTransacoes()
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            return await _context.Transacoes.Where(t => t.UsuarioId == id)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Transacao>> PostTransacao(Transacao transacao)
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (transacao.Valor <= 0 || !await _context.Categorias.AnyAsync(c => c.Id == transacao.CategoriaId && c.UsuarioId == id)
                || transacao.ContaId is not null && !await _context.Contas.AnyAsync(c => c.Id == transacao.ContaId && c.UsuarioId == id))
                return BadRequest();

            transacao.Id = 0;
            transacao.UsuarioId = id;
            transacao.Usuario = null!;
            transacao.Categoria = null!;
            transacao.Conta = null;


            _context.Transacoes.Add(transacao);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTransacoes), new { id = transacao.Id }, transacao);
        }
    }
}
