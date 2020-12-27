using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mosviewer.Domain;
using Mosviewer.Services;

namespace Mosviewer.Pages
{
    public class StationModel : PageModel
    {
        private readonly MosService _service;
        public Station Station { get; private set; } = new();
        public Modelinfo Modelinfo { get; private set; } = new();
        public Dictionary<string, MosService.Forecast> Forecasts { get; private set; } = new();
        public string? Message { get; private set; }
        public StationModel(MosService service)
        {
            _service = service;
        }

        public IActionResult OnGet(string id)
        {
            Modelinfo = _service.GetModelinfo() ?? new();

            var station = _service.GetStation(id);
            if (station == null)
            {
                Message = $"Die Station {id} wurde nicht gefunden.";
                return Page();
            }
            Station = station;
            ViewData["Title"] = $"{station.Id} {station.Name}";

            var forecasts = _service.GetForecast(id, station.Lng);
            if (forecasts == null)
            {
                Message = $"Die Station {id} wurde nicht gefunden.";
                return Page();
            }
            Forecasts = forecasts;
            return Page();
        }
    }
}
