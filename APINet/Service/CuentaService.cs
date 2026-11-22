using APINet.Models;
using APINet.Repositories;

namespace APINet.Service
{
    public class CuentaService : ICuentaService
    {
        private readonly ICuentaRepository _data;

        public CuentaService(ICuentaRepository data)
        {
            _data = data;
        }

        public async Task<List<CuentaBancaria>> ListarAsync() => await _data.ListarAsync();

        public async Task<CuentaBancaria?> ObtenerPorIdAsync(int id)
        {
            return await _data.ObtenerPorIdAsync(id);
        }

        public async Task<CuentaBancaria> CrearAsync(CuentaBancaria cuenta)
        {
            if (string.IsNullOrWhiteSpace(cuenta.Titular))
                throw new ArgumentException("El titular es obligatorio.");

            if (string.IsNullOrWhiteSpace(cuenta.NumeroCuenta))
                throw new ArgumentException("El número de cuenta es obligatorio.");

            // Validación: número de cuenta único
            var existente = await _data.ObtenerPorNumeroAsync(cuenta.NumeroCuenta);
            if (existente != null)
                throw new InvalidOperationException($"Ya existe una cuenta con el número '{cuenta.NumeroCuenta}'.");

            cuenta.Saldo = cuenta.Saldo < 0 ? 0 : cuenta.Saldo;

            return await _data.CrearAsync(cuenta);
        }

        public async Task ActualizarAsync(int id, ActualizarCuentaDto cuenta)
        {

            var cuentaExistente = await _data.ObtenerPorIdAsync(id);
            if (cuentaExistente == null)
                throw new InvalidOperationException("Cuenta no encontrada.");


            // ✅ Validar campos modificables
            if (string.IsNullOrWhiteSpace(cuenta.Titular))
                throw new ArgumentException("El titular no puede estar vacío.");

            // Actualizar solo los campos permitidos
            cuentaExistente.Titular = cuenta.Titular;
            cuentaExistente.TipoCuenta = cuenta.TipoCuenta;
            cuentaExistente.Moneda = cuenta.Moneda;
    

            await _data.ActualizarAsync(cuentaExistente);
        }

        public async Task EliminarAsync(int id)
        {
            var cuenta = await _data.ObtenerPorIdAsync(id);
            if (cuenta == null)
                throw new InvalidOperationException("Cuenta no encontrada.");

            await _data.EliminarAsync(cuenta);
        }

        public async Task<string> DepositarAsync(Transaccion request)
        {
            if (request.Monto <= 0)
                throw new ArgumentException("El monto debe ser mayor que cero.");

            var cuenta = await _data.ObtenerPorNumeroAsync(request.NumeroCuenta);
            if (cuenta == null)
                throw new InvalidOperationException("Cuenta no encontrada.");

            cuenta.Saldo += request.Monto;
            await _data.GuardarCambiosAsync();

            return $"Depósito exitoso en la cuenta {cuenta.NumeroCuenta}. Nuevo saldo: {cuenta.Saldo}";
        }

        public async Task<string> RetirarAsync(Transaccion request)
        {
            if (request.Monto <= 0)
                throw new ArgumentException("El monto debe ser mayor que cero.");

            var cuenta = await _data.ObtenerPorNumeroAsync(request.NumeroCuenta);
            if (cuenta == null)
                throw new InvalidOperationException("Cuenta no encontrada.");

            if (cuenta.Saldo < request.Monto)
                throw new InvalidOperationException("Fondos insuficientes.");

            cuenta.Saldo -= request.Monto;
            await _data.GuardarCambiosAsync();

            return $"Retiro exitoso en la cuenta {cuenta.NumeroCuenta}. Nuevo saldo: {cuenta.Saldo}";
        }
    }

    }
