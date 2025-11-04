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

        
        // GET: Chamados/MeusChamados
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
                DataAbertura = DateTime.UtcNow,
                UsuarioId = UsuarioId.Value
            };

            _db.Chamados.Add(chamado);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        // GET: /Chamados/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (UsuarioId == null) return RedirectToAction("Login", "Account");

            var chamado = await _db.Chamados
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null) return NotFound();

            // regra: usuário comum só pode ver o próprio chamado
            if (!IsAdmin && chamado.UsuarioId != UsuarioId.Value)
                return Forbid();

            // carregar comentários do chamado (mais recentes primeiro)
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
            if (UsuarioId == null)
                return RedirectToAction("Login", "Account");

            if (!IsAdmin)
                return Forbid(); // apenas o TI pode comentar

            if (string.IsNullOrWhiteSpace(texto))
            {
                TempData["MensagemErro"] = "O comentário não pode estar vazio.";
                return RedirectToAction("Details", new { id = chamadoId });
            }

            var chamado = await _db.Chamados.FindAsync(chamadoId);
            if (chamado == null)
                return NotFound();

            // 🚫 Bloqueia se o chamado já estiver fechado
            if (chamado.Status == "Fechado")
            {
                TempData["MensagemErro"] = "Não é possível adicionar comentários em chamados finalizados.";
                return RedirectToAction("Details", new { id = chamadoId });
            }

            // ⚙️ Se o chamado estiver "Aberto", muda automaticamente para "Em Andamento"
            if (chamado.Status == "Aberto")
            {
                chamado.Status = "Em Andamento";
                _db.Chamados.Update(chamado);
            }

            // Cria o comentário
            var comentario = new ChamadoComentario
            {
                ChamadoId = chamadoId,
                UsuarioId = UsuarioId.Value,
                Texto = texto.Trim(),
                DataHora = DateTime.UtcNow
             };

            _db.Comentarios.Add(comentario);
            foreach (var entry in _db.ChangeTracker.Entries()
         .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                var props = entry.Entity.GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

                foreach (var prop in props)
                {
                    var value = prop.GetValue(entry.Entity);
                    if (value is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
                        prop.SetValue(entry.Entity, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                }
            }

            await _db.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Comentário adicionado com sucesso!";
            return RedirectToAction("Details", new { id = chamadoId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(int id, string novoStatus)
        {
            var chamado = await _db.Chamados.FindAsync(id);
            if (chamado == null) return NotFound();

            // Validação: não permite alterar se estiver fechado
            if (chamado.Status == "Fechado")
            {
                TempData["MensagemErro"] = "O chamado já está finalizado e não pode ser alterado.";
                return RedirectToAction("Details", new { id });
            }

            chamado.Status = novoStatus;
            await _db.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Status atualizado com sucesso!";
            return RedirectToAction("Details", new { id });
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
                TempData["MensagemErro"] = "Este chamado já foi encerrado anteriormente.";
                return RedirectToAction("Details", new { id });
            }

            chamado.Status = "Fechado";
            await _db.SaveChangesAsync();

            // adiciona o comentário final
            if (!string.IsNullOrWhiteSpace(motivo))
            {
                var comentarioFinal = new ChamadoComentario
                {
                    ChamadoId = chamado.Id,
                    UsuarioId = UsuarioId.Value,
                    Texto = "🧰 " + motivo.Trim(),
                    DataHora = DateTime.UtcNow
                };
                _db.Comentarios.Add(comentarioFinal);
                await _db.SaveChangesAsync();
            }

            TempData["MensagemSucesso"] = "Chamado finalizado com sucesso!";
            return RedirectToAction("Details", new { id });
        }

    }
}

