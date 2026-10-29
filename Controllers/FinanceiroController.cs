using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DashboardFinanceiroPessoal.Data;
using DashboardFinanceiroPessoal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardFinanceiroPessoal.Controllers;

[Authorize]
[ApiController]
[Route("api/financeiro")]
public sealed class FinanceiroController(AppDbContext db) : ControllerBase
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("categorias")]
    public async Task<IActionResult> Categorias() => Ok(await db.Categorias
        .Where(c => c.UsuarioId == UsuarioId).OrderBy(c => c.Nome)
        .Select(c => new { c.Id, c.Nome, c.Tipo }).ToListAsync());

    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria(CategoriaInput input)
    {
        if (input.Tipo is not ("Receita" or "Despesa")) return BadRequest(new { message = "Tipo inválido." });
        var nome = input.Nome.Trim();
        if (await db.Categorias.AnyAsync(c => c.UsuarioId == UsuarioId && c.Nome == nome && c.Tipo == input.Tipo))
            return Conflict(new { message = "Esta categoria já existe." });
        var categoria = new Categoria { Nome = nome, Tipo = input.Tipo, UsuarioId = UsuarioId };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync();
        return Ok(new { categoria.Id, categoria.Nome, categoria.Tipo });
    }

    [HttpGet("contas")]
    public async Task<IActionResult> Contas()
    {
        var contas = await db.Contas.Where(c => c.UsuarioId == UsuarioId).OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.Instituicao, c.Tipo, c.SaldoInicial }).ToListAsync();
        var movimentos = await db.Transacoes.Where(t => t.UsuarioId == UsuarioId && t.ContaId != null)
            .Select(t => new { t.ContaId, t.Valor, Tipo = t.Categoria.Tipo }).ToListAsync();
        return Ok(contas.Select(c => new { c.Id, c.Nome, c.Instituicao, c.Tipo,
            Saldo = c.SaldoInicial + movimentos.Where(m => m.ContaId == c.Id)
                .Sum(m => m.Tipo == "Receita" ? m.Valor : -m.Valor) }));
    }

    [HttpPost("contas")]
    public async Task<IActionResult> CriarConta(ContaInput input)
    {
        if (input.Tipo is not ("Corrente" or "Poupança" or "Carteira" or "Outro"))
            return BadRequest(new { message = "Tipo de conta inválido." });
        var conta = new Conta { Nome = input.Nome.Trim(), Instituicao = input.Instituicao.Trim(),
            Tipo = input.Tipo, SaldoInicial = input.SaldoInicial, UsuarioId = UsuarioId };
        db.Contas.Add(conta);
        await db.SaveChangesAsync();
        return Ok(new { conta.Id, conta.Nome, conta.Instituicao, conta.Tipo, Saldo = conta.SaldoInicial });
    }

    [HttpGet("transacoes")]
    public async Task<IActionResult> Transacoes() => Ok(await db.Transacoes
        .Where(t => t.UsuarioId == UsuarioId).OrderByDescending(t => t.DataTransacao).ThenByDescending(t => t.Id)
        .Select(t => new { t.Id, t.Descricao, t.Valor, t.DataTransacao, t.CategoriaId,
            Categoria = t.Categoria.Nome, Tipo = t.Categoria.Tipo, t.ContaId,
            Conta = t.Conta == null ? null : t.Conta.Nome }).ToListAsync());

    [HttpPost("transacoes")]
    public async Task<IActionResult> CriarTransacao(TransacaoInput input)
    {
        if (!await db.Categorias.AnyAsync(c => c.Id == input.CategoriaId && c.UsuarioId == UsuarioId))
            return BadRequest(new { message = "Selecione uma categoria da sua conta." });
        if (!await db.Contas.AnyAsync(c => c.Id == input.ContaId && c.UsuarioId == UsuarioId))
            return BadRequest(new { message = "Selecione uma conta válida." });
        var transacao = new Transacao { Descricao = input.Descricao.Trim(), Valor = input.Valor,
            DataTransacao = input.DataTransacao.Date, CategoriaId = input.CategoriaId,
            ContaId = input.ContaId, UsuarioId = UsuarioId };
        db.Transacoes.Add(transacao);
        await db.SaveChangesAsync();
        return Ok(new { transacao.Id });
    }

    [HttpGet("orcamentos")]
    public async Task<IActionResult> Orcamentos([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        if (m < 1 || m > 12 || a < 2000 || a > 2100) return BadRequest(new { message = "Mês inválido." });
        var itens = await db.Orcamentos.Where(o => o.UsuarioId == UsuarioId && o.Ano == a && o.Mes == m)
            .Select(o => new { o.Id, o.Nome, o.Limite, o.CategoriaId, Categoria = o.Categoria.Nome }).ToListAsync();
        var inicio = new DateTime(a, m, 1);
        var gastos = await db.Transacoes.Where(t => t.UsuarioId == UsuarioId && t.DataTransacao >= inicio
            && t.DataTransacao < inicio.AddMonths(1) && t.Categoria.Tipo == "Despesa")
            .GroupBy(t => t.CategoriaId).Select(g => new { CategoriaId = g.Key, Valor = g.Sum(t => t.Valor) }).ToListAsync();
        return Ok(itens.Select(o => new { o.Id, o.Nome, o.Limite, o.CategoriaId, o.Categoria,
            Gasto = gastos.FirstOrDefault(g => g.CategoriaId == o.CategoriaId)?.Valor ?? 0 }));
    }

    [HttpPost("orcamentos")]
    public async Task<IActionResult> CriarOrcamento(OrcamentoInput input)
    {
        if (!await db.Categorias.AnyAsync(c => c.Id == input.CategoriaId && c.UsuarioId == UsuarioId && c.Tipo == "Despesa"))
            return BadRequest(new { message = "Selecione uma categoria de despesa." });
        if (await db.Orcamentos.AnyAsync(o => o.UsuarioId == UsuarioId && o.CategoriaId == input.CategoriaId
            && o.Ano == input.Ano && o.Mes == input.Mes))
            return Conflict(new { message = "Já existe orçamento para esta categoria neste mês." });
        var item = new Orcamento { Nome = input.Nome.Trim(), Limite = input.Limite, Mes = input.Mes,
            Ano = input.Ano, CategoriaId = input.CategoriaId, UsuarioId = UsuarioId };
        db.Orcamentos.Add(item);
        await db.SaveChangesAsync();
        return Ok(new { item.Id });
    }

    [HttpGet("metas")]
    public async Task<IActionResult> Metas()
    {
        var metas = await db.Metas.Where(m => m.UsuarioId == UsuarioId)
            .OrderBy(m => m.Prazo).Select(m => new { m.Id, m.Nome, m.ValorObjetivo, m.Prazo,
                Guardado = m.Depositos.Sum(d => (decimal?)d.Valor) ?? 0 }).ToListAsync();
        return Ok(metas);
    }

    [HttpPost("metas")]
    public async Task<IActionResult> CriarMeta(MetaInput input)
    {
        var meta = new Meta { Nome = input.Nome.Trim(), ValorObjetivo = input.ValorObjetivo,
            Prazo = input.Prazo.Date, UsuarioId = UsuarioId };
        db.Metas.Add(meta);
        await db.SaveChangesAsync();
        return Ok(new { meta.Id });
    }

    [HttpPost("metas/{id:int}/depositos")]
    public async Task<IActionResult> Depositar(int id, DepositoInput input)
    {
        if (!await db.Metas.AnyAsync(m => m.Id == id && m.UsuarioId == UsuarioId)) return NotFound();
        var deposito = new DepositoMeta { MetaId = id, Valor = input.Valor, Data = input.Data.Date,
            Observacao = input.Observacao?.Trim() ?? string.Empty };
        db.DepositosMeta.Add(deposito);
        await db.SaveChangesAsync();
        return Ok(new { deposito.Id });
    }
}

public sealed class CategoriaInput
{
    [Required, StringLength(80)] public string Nome { get; init; } = string.Empty;
    [Required] public string Tipo { get; init; } = string.Empty;
}
public sealed class ContaInput
{
    [Required, StringLength(100)] public string Nome { get; init; } = string.Empty;
    [StringLength(100)] public string Instituicao { get; init; } = string.Empty;
    [Required] public string Tipo { get; init; } = string.Empty;
    public decimal SaldoInicial { get; init; }
}
public sealed class TransacaoInput
{
    [Required, StringLength(180)] public string Descricao { get; init; } = string.Empty;
    [Range(0.01, 999999999)] public decimal Valor { get; init; }
    [Range(1, int.MaxValue)] public int CategoriaId { get; init; }
    [Range(1, int.MaxValue)] public int ContaId { get; init; }
    [Required] public DateTime DataTransacao { get; init; }
}
public sealed class OrcamentoInput
{
    [Required, StringLength(100)] public string Nome { get; init; } = string.Empty;
    [Range(0.01, 999999999)] public decimal Limite { get; init; }
    [Range(1, 12)] public int Mes { get; init; }
    [Range(2000, 2100)] public int Ano { get; init; }
    [Range(1, int.MaxValue)] public int CategoriaId { get; init; }
}
public sealed class MetaInput
{
    [Required, StringLength(100)] public string Nome { get; init; } = string.Empty;
    [Range(0.01, 999999999)] public decimal ValorObjetivo { get; init; }
    [Required] public DateTime Prazo { get; init; }
}
public sealed class DepositoInput
{
    [Range(0.01, 999999999)] public decimal Valor { get; init; }
    [Required] public DateTime Data { get; init; }
    [StringLength(300)] public string? Observacao { get; init; }
}
