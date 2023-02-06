using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Mosviewer.Domain;
using Mosviewer.Infrastructure;
using Mosviewer.Services;

namespace Mosviewer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly MosService _service;

        public List<Station> Stations = new();

        public IndexModel(MosService service)
        {
            _service = service;
        }

        public void OnGetAsync()
        {
            Stations = _service.GetAllStations();
        }

    }
}
