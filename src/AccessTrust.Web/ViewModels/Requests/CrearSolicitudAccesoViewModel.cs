using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessTrust.Web.ViewModels.Requests;

public class CrearSolicitudAccesoViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un recurso.")]
    public string RecursoId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(500, ErrorMessage = "El motivo no debe superar los 500 caracteres.")]
    public string Motivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La duración solicitada es obligatoria.")]
    [Range(1, 480, ErrorMessage = "La duración debe estar entre 1 y 480 minutos.")]
    public int DuracionSolicitadaMin { get; set; } = 60;

    [Required(ErrorMessage = "La prioridad es obligatoria.")]
    public string Prioridad { get; set; } = "Normal";

    public List<SelectListItem> RecursosDisponibles { get; set; } = new();

    public List<SelectListItem> PrioridadesDisponibles { get; set; } = new()
    {
        new SelectListItem { Value = "Baja", Text = "Baja" },
        new SelectListItem { Value = "Normal", Text = "Normal" },
        new SelectListItem { Value = "Alta", Text = "Alta" }
    };
}