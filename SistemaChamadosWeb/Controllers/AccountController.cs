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
            HttpContext.Session.SetString("UsuarioTipo", user.Tipo);

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
            // valida campos obrigatórios
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                TempData["MensagemErro"] = "Todos os campos são obrigatórios.";
                return RedirectToAction("Register");
            }

            // valida formato do e-mail
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                TempData["MensagemErro"] = "Informe um e-mail válido.";
                return RedirectToAction("Register");
            }

            // valida senha mínima
            if (senha.Length < 6)
            {
                TempData["MensagemErro"] = "A senha deve ter pelo menos 6 caracteres.";
                return RedirectToAction("Register");
            }

            var existente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (existente != null)
            {
                TempData["MensagemErro"] = "Já existe uma conta com este e-mail.";
                return RedirectToAction("Register");
            }

            var novoUsuario = new Usuario
            {
                Nome = nome,
                Email = email,
                Senha = senha,
                Tipo = "Usuario"
            };

            _db.Usuarios.Add(novoUsuario);
            await _db.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Conta criada com sucesso! Agora faça login.";
            return RedirectToAction("Login");
        }


    }
}
