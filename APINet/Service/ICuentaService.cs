using APINet.Models;

namespace APINet.Service
{
    public interface ICuentaService
    {
        Task<List<CuentaBancaria>> ListarAsync();
        Task<CuentaBancaria?> ObtenerPorIdAsync(int id);
        Task<CuentaBancaria> CrearAsync(CuentaBancaria cuenta);
        Task ActualizarAsync(int id, ActualizarCuentaDto cuenta);
        Task EliminarAsync(int id);
        Task<string> DepositarAsync(Transaccion request);
        Task<string> RetirarAsync(Transaccion request);
    }
}
