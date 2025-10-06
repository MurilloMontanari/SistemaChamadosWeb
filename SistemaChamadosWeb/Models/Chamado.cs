using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaChamadosWeb.Models
{
    [Table("chamados")]
    public class Chamado
    {
        [Key]
        [Column("id")]   // <- no banco é "id"
        public int Id { get; set; }

        [Column("titulo")]
        public string Titulo { get; set; }

        [Column("descricao")]
        public string Descricao { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("prioridade")]
        public string Prioridade { get; set; }

        [Column("data_abertura")]
        public DateTime DataAbertura { get; set; }

        [Column("usuarioid")]
        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; }  // Navegação para o usuário   
    }
}

