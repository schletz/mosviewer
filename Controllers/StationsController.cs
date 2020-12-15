using Microsoft.AspNetCore.Mvc;
using Mosviewer.Domain;
using Mosviewer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mosviewer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StationsController : ControllerBase
    {


        private readonly MosService _service;

        public StationsController(MosService service)
        {
            _service = service;
        }

        [HttpGet]
        public List<Station> GetStations() => _service.GetAllStations();

        [HttpGet("{id}")]
        public ActionResult<List<MosService.Forecast>> GetStationValues(string id)
        {
            var values = _service.GetStationValues(id);
            if (values == null) { return NotFound(); }

            return Ok(values);
        }

        [HttpGet("nearby/{lat}/{lng}/{dist}")]
        public List<Station> GetNearbyStations(double lat, double lng, double dist) =>
            _service.GetNearbyStations(lat, lng, dist);
    }
}
