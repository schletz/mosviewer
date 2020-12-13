using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Mosviewer.Domain;
using Mosviewer.Infrastructure;

namespace Mosviewer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly MosClient _client;
        public List<MosFile> Files = new();
        public List<MosStationData> StationDatas = new();

        public IndexModel(MosClient client)
        {
            _client = client;
        }

        public async Task OnGetAsync()
        {
            Files = await _client.GetFilelisting();
            StationDatas = await _client.ReadStationData(Files.OrderByDescending(f => f.LastUpdate).First());

        }
    }
}
