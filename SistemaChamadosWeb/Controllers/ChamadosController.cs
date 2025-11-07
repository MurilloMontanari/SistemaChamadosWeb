using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaChamadosWeb.Data;
using SistemaChamadosWeb.Models;

namespace SistemaChamadosWeb.Controllers
{
    public class ChamadosController : Controller
    {
        private readonly AppDbContext _db;
        public ChamadosController(AppDbContext db) => _db = db;

        private int? UsuarioId => HttpContext.Session.GetInt32("UsuarioId");
        private string UsuarioTipo => HttpContext.Session.GetString("UsuarioTipo") ?? "Usuario";
        private bool IsAdmin => UsuarioTipo == "Admin";

        public async Task<IActionResult> Index()
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");

            IQueryable<Chamado> query = _db.Chamados;

            if (!IsAdmin)
                query = query.Where(c => c.UsuarioId == UsuarioId);

            var chamados = await query
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(chamados);
        }

        public async Task<IActionResult> MeusChamados()
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");

            var chamados = await _db.Chamados
                .Where(c => c.UsuarioId == UsuarioId)
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(chamados);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string titulo, string descricao, string prioridade)
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");

            var chamado = new Chamado
            {
                Titulo = titulo,
                Descricao = descricao,
                Prioridade = prioridade ?? "Média",
                Status = "Aberto",
                DataAbertura = DateTime.Now, // ✅ Sempre UTC
                UsuarioId = UsuarioId.Value
            };

            _db.Chamados.Add(chamado);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");

            var chamado = await _db.Chamados
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null) return NotFound();
            if (!IsAdmin && chamado.UsuarioId != UsuarioId.Value) return Forbid();

            var comentarios = await _db.Comentarios
                .Where(cc => cc.ChamadoId == id)
                .OrderByDescending(cc => cc.DataHora)
                .Include(cc => cc.Usuario)
                .ToListAsync();

            ViewBag.Comentarios = comentarios;
            ViewBag.IsAdmin = IsAdmin;

            return View(chamado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarComentario(int chamadoId, string texto)
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            if (string.IsNullOrWhiteSpace(texto))
            {
                TempData["MensagemErro"] = "O comentário não pode estar vazio.";
                return RedirectToAction("Details", new { id = chamadoId });
            }

            var chamado = await _db.Chamados.FindAsync(chamadoId);
            if (chamado == null) return NotFound();

            if (chamado.Status == "Fechado")
            {
                TempData["MensagemErro"] = "Não é possível adicionar comentários em chamados finalizados.";
                return RedirectToAction("Details", new { id = chamadoId });
            }

            if (chamado.Status == "Aberto")
            {
                chamado.Status = "Em Andamento";
                _db.Chamados.Update(chamado);
            }

            var comentario = new ChamadoComentario
            {
                ChamadoId = chamadoId,
                UsuarioId = UsuarioId.Value,
                Texto = texto.Trim(),
                DataHora = DateTime.Now // ✅ Sempre UTC
            };

            _db.Comentarios.Add(comentario);
            await _db.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Comentário adicionado com sucesso!";
            return RedirectToAction("Details", new { id = chamadoId });
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarChamado(int id, string motivo)
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            var chamado = await _db.Chamados.FindAsync(id);
            if (chamado == null) return NotFound();

            if (chamado.Status == "Fechado")
            {
                TempData["MensagemErro"] = "Este chamado já foi encerrado.";
                return RedirectToAction("Details", new { id });
            }

            chamado.Status = "Fechado";
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(motivo))
            {
                var comentarioFinal = new ChamadoComentario
                {
                    ChamadoId = chamado.Id,
                    UsuarioId = UsuarioId.Value,
                    Texto = "🧰 " + motivo.Trim(),
                    DataHora = DateTime.Now // ✅ Sempre UTC
                };

                _db.Comentarios.Add(comentarioFinal);
                await _db.SaveChangesAsync();
            }

            TempData["MensagemSucesso"] = "Chamado finalizado com sucesso!";
            return RedirectToAction("Details", new { id });
        }
    }
}
