using APINet.Data;
using APINet.Models;
using Microsoft.EntityFrameworkCore;

namespace APINet.Repositories
{
    public class CuentaRepository : ICuentaRepository
    {
        private readonly AppDbContext _context;

        public CuentaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CuentaBancaria>> ListarAsync()
        {
            return await _context.CuentasBancarias.ToListAsync();
        }

        public async Task<CuentaBancaria?> ObtenerPorIdAsync(int id)
        {
            return await _context.CuentasBancarias.FindAsync(id);
        }

        public async Task<CuentaBancaria?> ObtenerPorNumeroAsync(string numeroCuenta)
        {
            return await _context.CuentasBancarias
                .FirstOrDefaultAsync(c => c.NumeroCuenta == numeroCuenta);
        }

        public async Task<CuentaBancaria> CrearAsync(CuentaBancaria cuenta)
        {
            _context.CuentasBancarias.Add(cuenta);
            await _context.SaveChangesAsync();
            return cuenta;
        }

        public async Task ActualizarAsync(CuentaBancaria cuenta)
        {
            _context.Entry(cuenta).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(CuentaBancaria cuenta)
        {
            _context.CuentasBancarias.Remove(cuenta);
            await _context.SaveChangesAsync();
        }

        public async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
