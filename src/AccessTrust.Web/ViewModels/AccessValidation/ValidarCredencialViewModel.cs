using System.ComponentModel.DataAnnotations;

namespace AccessTrust.Web.ViewModels.AccessValidation;

public class ValidarCredencialViewModel
{
    [Required]
    public string RecursoId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string RecursoSensibilidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El token temporal es obligatorio.")]
    [StringLength(200, ErrorMessage = "El token temporal no puede superar los 200 caracteres.")]
    public string TokenPlano { get; set; } = string.Empty;
}