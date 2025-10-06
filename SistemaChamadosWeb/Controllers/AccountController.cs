using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaChamadosWeb.Data;
using SistemaChamadosWeb.Models;
using System.Threading.Tasks;
using System.Linq;

namespace SistemaChamadosWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string senha)
        {
            var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null || user.Senha != senha)
            {
                ViewBag.Erro = "Usuário ou senha inválidos.";
                return View();
            }

            // LOGIN OK
            HttpContext.Session.SetInt32("UsuarioId", user.Id);
            HttpContext.Session.SetString("UsuarioNome", user.Nome);

            return RedirectToAction("Index", "Chamados");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string nome, string email, string senha)
        {
            // verifica se já existe
            var existente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (existente != null)
            {
                ViewBag.Erro = "Já existe uma conta com este email.";
                return View();
            }

            var novoUsuario = new Usuario
            {
                Nome = nome,
                Email = email,
                Senha = senha // aqui está em texto puro, depois podemos melhorar
            };

            _db.Usuarios.Add(novoUsuario);
            await _db.SaveChangesAsync();

            ViewBag.Sucesso = "Conta criada com sucesso! Agora faça login.";
            return RedirectToAction("Login");
        }
    }
}
