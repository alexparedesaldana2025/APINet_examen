using APINet.Models;
using APINet.Service;
using Microsoft.AspNetCore.Mvc;

namespace APINet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuentaController : ControllerBase
    {
        private readonly ICuentaService _service;

        public CuentaController(ICuentaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CuentaBancaria>>> GetAll()
        {
            return await _service.ListarAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CuentaBancaria>> GetById(int id)
        {
            var cuenta = await _service.ObtenerPorIdAsync(id);
            if (cuenta == null) return NotFound();
            return cuenta;
        }

        [HttpPost]
        public async Task<ActionResult> Create(CuentaBancaria cuenta)
        {
            try
            {
                var creada = await _service.CrearAsync(cuenta);
                return CreatedAtAction(nameof(GetById), new { id = creada.IdCuenta }, creada);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ActualizarCuentaDto cuenta)
        {
            try
            {
                await _service.ActualizarAsync(id, cuenta);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.EliminarAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("depositar")]
        public async Task<IActionResult> Depositar([FromBody] Transaccion request)
        {
            try
            {
                var mensaje = await _service.DepositarAsync(request);
                return Ok(new { mensaje });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("retirar")]
        public async Task<IActionResult> Retirar([FromBody] Transaccion request)
        {
            try
            {
                var mensaje = await _service.RetirarAsync(request);
                return Ok(new { mensaje });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }

    }
