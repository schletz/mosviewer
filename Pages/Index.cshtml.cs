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
        private readonly MosRepository _repo;

        //public List<MosFile> Files = new();
        public List<Station> Stations = new();

        public IndexModel(MosRepository repo)
        {
            _repo = repo;
        }

        public void OnGetAsync()
        {
            //Files = await _client.GetFilelisting();
            //await _client.ReadStationData(Files.OrderByDescending(f => f.LastUpdate).First());
            Stations = _repo.GetStationsWithValues(s => s.Id.StartsWith("110"))
                .OrderBy(s=>s.Id)
                .ToList();
        }
    }
}
