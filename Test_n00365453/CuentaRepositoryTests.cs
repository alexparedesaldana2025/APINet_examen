using APINet.Data;
using APINet.Models;
using APINet.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Test_n00365453
{
    /// <summary>
    /// Pruebas unitarias de la capa Repository.
    /// Como AppDbContext (EF Core) no se puede simular de forma práctica con Moq
    /// (DbSet, FindAsync, ToListAsync, etc.), se usa el proveedor EF Core InMemory:
    /// una base de datos en memoria, aislada por prueba, que reemplaza a SQL Server.
    /// </summary>
    [TestFixture]
    public class CuentaRepositoryTests
    {
        private DbContextOptions<AppDbContext> _options = null!;

        [SetUp]
        public void SetUp()
        {
            // Una base de datos en memoria distinta para cada prueba
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private AppDbContext NuevoContexto() => new(_options);

        private static CuentaBancaria NuevaCuenta(string numero = "001-0001", decimal saldo = 1500m) => new()
        {
            NumeroCuenta = numero,
            Titular = "Juan Pérez",
            TipoCuenta = "Ahorros",
            Saldo = saldo,
            Moneda = "PEN"
        };

        private async Task SembrarAsync(params CuentaBancaria[] cuentas)
        {
            using var ctx = NuevoContexto();
            ctx.CuentasBancarias.AddRange(cuentas);
            await ctx.SaveChangesAsync();
        }

        // ---------------- ListarAsync ----------------

        [Test]
        public async Task ListarAsync_DeberiaRetornarTodasLasCuentas_SiExistenRegistros()
        {
            // Arrange
            await SembrarAsync(NuevaCuenta("001-0001"), NuevaCuenta("001-0002"), NuevaCuenta("001-0003"));
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);

            // Act
            var result = await repo.ListarAsync();

            // Assert
            Assert.That(result, Has.Count.EqualTo(3));
        }

        [Test]
        public async Task ListarAsync_DeberiaRetornarListaVacia_SiNoHayRegistros()
        {
            // Arrange
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);

            // Act
            var result = await repo.ListarAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }

        // ---------------- ObtenerPorIdAsync ----------------

        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarCuenta_SiExiste()
        {
            // Arrange
            var cuenta = NuevaCuenta("001-0001");
            await SembrarAsync(cuenta);
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);

            // Act
            var result = await repo.ObtenerPorIdAsync(cuenta.IdCuenta);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.NumeroCuenta, Is.EqualTo("001-0001"));
        }

        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarNull_SiNoExiste()
        {
            // Arrange
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);

            // Act
            var result = await repo.ObtenerPorIdAsync(999);

            // Assert
            Assert.That(result, Is.Null);
        }

        // ---------------- ObtenerPorNumeroAsync ----------------

        [Test]
        public async Task ObtenerPorNumeroAsync_DeberiaRetornarCuenta_SiExiste()
        {
            // Arrange
            await SembrarAsync(NuevaCuenta("001-0001"), NuevaCuenta("001-0002"));
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);

            // Act
            var result = await repo.ObtenerPorNumeroAsync("001-0002");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.NumeroCuenta, Is.EqualTo("001-0002"));
        }

        [Test]
        public async Task ObtenerPorNumeroAsync_DeberiaRetornarNull_SiNoExiste()
        {
            // Arrange
            await SembrarAsync(NuevaCuenta("001-0001"));
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);

            // Act
            var result = await repo.ObtenerPorNumeroAsync("NO-EXISTE");

            // Assert
            Assert.That(result, Is.Null);
        }

        // ---------------- CrearAsync ----------------

        [Test]
        public async Task CrearAsync_DeberiaPersistirCuentaYAsignarId()
        {
            // Arrange
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);
            var cuenta = NuevaCuenta("001-0050", 300m);

            // Act
            var result = await repo.CrearAsync(cuenta);

            // Assert
            Assert.That(result.IdCuenta, Is.GreaterThan(0));
            using var verificacion = NuevoContexto();
            var guardada = await verificacion.CuentasBancarias.SingleAsync();
            Assert.That(guardada.NumeroCuenta, Is.EqualTo("001-0050"));
            Assert.That(guardada.Saldo, Is.EqualTo(300m));
        }

        // ---------------- ActualizarAsync ----------------

        [Test]
        public async Task ActualizarAsync_DeberiaPersistirLosCambios()
        {
            // Arrange
            var original = NuevaCuenta("001-0001");
            await SembrarAsync(original);
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);
            var cuenta = (await repo.ObtenerPorIdAsync(original.IdCuenta))!;
            cuenta.Titular = "Titular Modificado";
            cuenta.Moneda = "USD";

            // Act
            await repo.ActualizarAsync(cuenta);

            // Assert
            using var verificacion = NuevoContexto();
            var actualizada = await verificacion.CuentasBancarias.FindAsync(original.IdCuenta);
            Assert.That(actualizada!.Titular, Is.EqualTo("Titular Modificado"));
            Assert.That(actualizada.Moneda, Is.EqualTo("USD"));
        }

        // ---------------- EliminarAsync ----------------

        [Test]
        public async Task EliminarAsync_DeberiaQuitarLaCuentaDeLaBaseDeDatos()
        {
            // Arrange
            var cuenta = NuevaCuenta("001-0001");
            await SembrarAsync(cuenta, NuevaCuenta("001-0002"));
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);
            var aEliminar = (await repo.ObtenerPorIdAsync(cuenta.IdCuenta))!;

            // Act
            await repo.EliminarAsync(aEliminar);

            // Assert
            using var verificacion = NuevoContexto();
            Assert.That(await verificacion.CuentasBancarias.CountAsync(), Is.EqualTo(1));
            Assert.That(await verificacion.CuentasBancarias.FindAsync(cuenta.IdCuenta), Is.Null);
        }

        // ---------------- GuardarCambiosAsync ----------------

        [Test]
        public async Task GuardarCambiosAsync_DeberiaPersistirModificacionesPendientes()
        {
            // Arrange
            var original = NuevaCuenta("001-0001", 1000m);
            await SembrarAsync(original);
            using var ctx = NuevoContexto();
            var repo = new CuentaRepository(ctx);
            var cuenta = (await repo.ObtenerPorIdAsync(original.IdCuenta))!;
            cuenta.Saldo = 2500m;

            // Act
            await repo.GuardarCambiosAsync();

            // Assert
            using var verificacion = NuevoContexto();
            var guardada = await verificacion.CuentasBancarias.FindAsync(original.IdCuenta);
            Assert.That(guardada!.Saldo, Is.EqualTo(2500m));
        }
    }
}
