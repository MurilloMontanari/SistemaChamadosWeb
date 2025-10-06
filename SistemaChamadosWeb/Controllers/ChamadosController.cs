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

        public async Task<IActionResult> Index()
        {
            var chamados = await _db.Chamados
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
            var chamado = await _db.Chamados
                                   .Include(c => c.Usuario) // agora sim!
                                   .FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                return NotFound();
            }

            return View(chamado);
        }


    }
}

