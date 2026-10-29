using System.Globalization;
using System.Security.Claims;
using DashboardFinanceiroPessoal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DashboardFinanceiroPessoal.Controllers;

[Authorize]
[ApiController]
[Route("api/financeiro/relatorios")]
public sealed class RelatoriosController(AppDbContext db) : ControllerBase
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf([FromQuery] int ano, [FromQuery] int mes)
    {
        if (ano is < 2000 or > 2100 || mes is < 1 or > 12)
            return BadRequest(new { message = "Mês inválido." });

        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await db.Usuarios.Where(u => u.Id == usuarioId)
            .Select(u => new { u.Nome }).SingleAsync();
        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1);
        var transacoes = await db.Transacoes
            .Where(t => t.UsuarioId == usuarioId && t.DataTransacao >= inicio && t.DataTransacao < fim)
            .OrderBy(t => t.DataTransacao).ThenBy(t => t.Id)
            .Select(t => new
            {
                t.DataTransacao, t.Descricao, t.Valor,
                Categoria = t.Categoria.Nome, Tipo = t.Categoria.Tipo,
                Conta = t.Conta == null ? "—" : t.Conta.Nome
            }).ToListAsync();

        var receitas = transacoes.Where(t => t.Tipo == "Receita").Sum(t => t.Valor);
        var despesas = transacoes.Where(t => t.Tipo == "Despesa").Sum(t => t.Valor);
        var categorias = transacoes.Where(t => t.Tipo == "Despesa")
            .GroupBy(t => t.Categoria).Select(g => new { Nome = g.Key, Valor = g.Sum(t => t.Valor) })
            .OrderByDescending(g => g.Valor).ToList();
        var periodo = inicio.ToString("MMMM 'de' yyyy", PtBr);

        var documento = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.7f, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontSize(9).FontColor("#26372D"));
            page.Header().Column(header =>
            {
                header.Item().Text("Relatório financeiro").SemiBold().FontSize(20).FontColor("#06432F");
                header.Item().PaddingTop(4).Text($"{usuario.Nome}  -  {periodo}").FontColor("#66796E");
                header.Item().PaddingTop(8).LineHorizontal(1).LineColor("#D9E6DD");
            });
            page.Content().PaddingVertical(16).Column(col =>
            {
                col.Spacing(18);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Receitas\n{Moeda(receitas)}").SemiBold().FontSize(11).FontColor("#2E805F");
                    row.RelativeItem().Text($"Despesas\n{Moeda(despesas)}").SemiBold().FontSize(11).FontColor("#B34848");
                    row.RelativeItem().Text($"Resultado\n{Moeda(receitas - despesas)}").SemiBold().FontSize(11);
                });
                col.Item().Column(section =>
                {
                    section.Item().Text("Despesas por categoria").SemiBold().FontSize(12).FontColor("#06432F");
                    if (categorias.Count == 0)
                        section.Item().PaddingTop(7).Text("Nenhuma despesa registrada neste mês.");
                    foreach (var categoria in categorias)
                        section.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text(categoria.Nome);
                            row.AutoItem().Text(Moeda(categoria.Valor));
                        });
                });
                col.Item().Column(section =>
                {
                    section.Item().Text("Transações").SemiBold().FontSize(12).FontColor("#06432F");
                    if (transacoes.Count == 0)
                    {
                        section.Item().PaddingTop(7).Text("Nenhuma transação registrada neste mês.");
                        return;
                    }
                    section.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(53);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(67);
                        });
                        table.Header(header =>
                        {
                            foreach (var titulo in new[] { "Data", "Descrição", "Categoria", "Conta", "Tipo", "Valor" })
                                header.Cell().Background("#E8F2EC").Padding(5).Text(titulo).SemiBold();
                        });
                        foreach (var item in transacoes)
                        {
                            foreach (var valor in new[]
                            {
                                item.DataTransacao.ToString("dd/MM/yy", PtBr), item.Descricao,
                                item.Categoria, item.Conta, item.Tipo,
                                (item.Tipo == "Despesa" ? "-" : "+") + Moeda(item.Valor)
                            })
                                table.Cell().BorderBottom(0.5f).BorderColor("#E4E9E5").Padding(5).Text(valor);
                        }
                    });
                });
            });
            page.Footer().AlignRight().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
            });
        }));

        return File(documento.GeneratePdf(), "application/pdf", $"relatorio-{ano:D4}-{mes:D2}.pdf");
    }
}
