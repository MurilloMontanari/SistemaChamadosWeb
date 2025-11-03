using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaChamadosWeb.Models
{
    [Table("comentarios")]
    public class ChamadoComentario
    {
        public int id { get; set; }

        [Column("chamadoid")]
        public int ChamadoId { get; set; }

        [Column("usuarioid")]
        public int UsuarioId { get; set; }

        [Column("texto")]
        public string Texto { get; set; }

        [Column("datahora")]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        // navegações (opcional, mas útil)
        public Chamado Chamado { get; set; }
        public Usuario Usuario { get; set; }
    }
}
