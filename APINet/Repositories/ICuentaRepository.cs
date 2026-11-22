using APINet.Models;

namespace APINet.Repositories
{
    public interface ICuentaRepository
    {
        Task<List<CuentaBancaria>> ListarAsync();
        Task<CuentaBancaria?> ObtenerPorIdAsync(int id);
        Task<CuentaBancaria?> ObtenerPorNumeroAsync(string numeroCuenta);
        Task<CuentaBancaria> CrearAsync(CuentaBancaria cuenta);
        Task ActualizarAsync(CuentaBancaria cuenta);
        Task EliminarAsync(CuentaBancaria cuenta);
        Task GuardarCambiosAsync();
    }
}
