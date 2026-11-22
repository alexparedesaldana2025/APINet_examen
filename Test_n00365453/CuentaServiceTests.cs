using APINet.Models;
using APINet.Repositories;
using APINet.Service;
using Moq;

namespace Test_n00365453
{
    /// <summary>
    /// Pruebas unitarias de la capa Service (reglas de negocio).
    /// Se simula (Mock) ICuentaRepository para aislar el servicio.
    /// </summary>
    [TestFixture]
    public class CuentaServiceTests
    {
        private Mock<ICuentaRepository> _repoMock = null!;
        private CuentaService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<ICuentaRepository>();
            _service = new CuentaService(_repoMock.Object);
        }

        private static CuentaBancaria NuevaCuenta(int id = 1, string numero = "001-0001", decimal saldo = 1500m) => new()
        {
            IdCuenta = id,
            NumeroCuenta = numero,
            Titular = "Juan Pérez",
            TipoCuenta = "Ahorros",
            Saldo = saldo,
            Moneda = "PEN"
        };

        // ---------------- Listar / ObtenerPorId ----------------

        [Test]
        public async Task ListarAsync_DeberiaRetornarCuentasDelRepositorio()
        {
            // Arrange
            var cuentas = new List<CuentaBancaria> { NuevaCuenta(1), NuevaCuenta(2, "001-0002") };
            _repoMock.Setup(r => r.ListarAsync()).ReturnsAsync(cuentas);

            // Act
            var result = await _service.ListarAsync();

            // Assert
            Assert.That(result, Is.EquivalentTo(cuentas));
            _repoMock.Verify(r => r.ListarAsync(), Times.Once);
        }

        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarCuenta_SiExiste()
        {
            // Arrange
            var cuenta = NuevaCuenta();
            _repoMock.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(cuenta);

            // Act
            var result = await _service.ObtenerPorIdAsync(1);

            // Assert
            Assert.That(result, Is.SameAs(cuenta));
        }

        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarNull_SiNoExiste()
        {
            // Arrange
            _repoMock.Setup(r => r.ObtenerPorIdAsync(99)).ReturnsAsync((CuentaBancaria?)null);

            // Act
            var result = await _service.ObtenerPorIdAsync(99);

            // Assert
            Assert.That(result, Is.Null);
        }

        // ---------------- CrearAsync ----------------

        [Test]
        public async Task CrearAsync_DeberiaCrearCuenta_SiDatosValidos()
        {
            // Arrange
            var cuenta = NuevaCuenta(0, "001-0010", 200m);
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("001-0010")).ReturnsAsync((CuentaBancaria?)null);
            _repoMock.Setup(r => r.CrearAsync(cuenta)).ReturnsAsync(cuenta);

            // Act
            var result = await _service.CrearAsync(cuenta);

            // Assert
            Assert.That(result, Is.SameAs(cuenta));
            Assert.That(result.Saldo, Is.EqualTo(200m));
            _repoMock.Verify(r => r.CrearAsync(cuenta), Times.Once);
        }

        [Test]
        public async Task CrearAsync_DeberiaAjustarSaldoACero_SiSaldoEsNegativo()
        {
            // Arrange
            var cuenta = NuevaCuenta(0, "001-0011", -50m);
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("001-0011")).ReturnsAsync((CuentaBancaria?)null);
            _repoMock.Setup(r => r.CrearAsync(It.IsAny<CuentaBancaria>()))
                     .ReturnsAsync((CuentaBancaria c) => c);

            // Act
            var result = await _service.CrearAsync(cuenta);

            // Assert
            Assert.That(result.Saldo, Is.EqualTo(0m));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void CrearAsync_DeberiaLanzarArgumentException_SiTitularVacio(string titular)
        {
            // Arrange
            var cuenta = NuevaCuenta(0, "001-0012");
            cuenta.Titular = titular;

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CrearAsync(cuenta));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("El titular es obligatorio."));
            _repoMock.Verify(r => r.CrearAsync(It.IsAny<CuentaBancaria>()), Times.Never);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void CrearAsync_DeberiaLanzarArgumentException_SiNumeroCuentaVacio(string numero)
        {
            // Arrange
            var cuenta = NuevaCuenta(0);
            cuenta.NumeroCuenta = numero;

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.CrearAsync(cuenta));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("El número de cuenta es obligatorio."));
            _repoMock.Verify(r => r.CrearAsync(It.IsAny<CuentaBancaria>()), Times.Never);
        }

        [Test]
        public void CrearAsync_DeberiaLanzarInvalidOperationException_SiNumeroCuentaYaExiste()
        {
            // Arrange
            var cuenta = NuevaCuenta(0, "001-0001");
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("001-0001")).ReturnsAsync(NuevaCuenta());

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.CrearAsync(cuenta));

            // Assert
            Assert.That(ex!.Message, Does.Contain("001-0001"));
            _repoMock.Verify(r => r.CrearAsync(It.IsAny<CuentaBancaria>()), Times.Never);
        }

        // ---------------- ActualizarAsync ----------------

        [Test]
        public async Task ActualizarAsync_DeberiaActualizarSoloCamposPermitidos_SiCuentaExiste()
        {
            // Arrange
            var existente = NuevaCuenta(1, "001-0001", 1500m);
            var dto = new ActualizarCuentaDto { Titular = "Nuevo Titular", TipoCuenta = "Corriente", Moneda = "USD" };
            _repoMock.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(existente);

            // Act
            await _service.ActualizarAsync(1, dto);

            // Assert
            Assert.That(existente.Titular, Is.EqualTo("Nuevo Titular"));
            Assert.That(existente.TipoCuenta, Is.EqualTo("Corriente"));
            Assert.That(existente.Moneda, Is.EqualTo("USD"));
            Assert.That(existente.Saldo, Is.EqualTo(1500m));          // no se modifica
            Assert.That(existente.NumeroCuenta, Is.EqualTo("001-0001")); // no se modifica
            _repoMock.Verify(r => r.ActualizarAsync(existente), Times.Once);
        }

        [Test]
        public void ActualizarAsync_DeberiaLanzarInvalidOperationException_SiCuentaNoExiste()
        {
            // Arrange
            _repoMock.Setup(r => r.ObtenerPorIdAsync(99)).ReturnsAsync((CuentaBancaria?)null);
            var dto = new ActualizarCuentaDto { Titular = "Nuevo" };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.ActualizarAsync(99, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Cuenta no encontrada."));
            _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<CuentaBancaria>()), Times.Never);
        }

        [Test]
        public void ActualizarAsync_DeberiaLanzarArgumentException_SiTitularVacio()
        {
            // Arrange
            _repoMock.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(NuevaCuenta());
            var dto = new ActualizarCuentaDto { Titular = "  ", TipoCuenta = "Ahorros", Moneda = "PEN" };

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.ActualizarAsync(1, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("El titular no puede estar vacío."));
            _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<CuentaBancaria>()), Times.Never);
        }

        // ---------------- EliminarAsync ----------------

        [Test]
        public async Task EliminarAsync_DeberiaEliminarCuenta_SiExiste()
        {
            // Arrange
            var cuenta = NuevaCuenta();
            _repoMock.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(cuenta);

            // Act
            await _service.EliminarAsync(1);

            // Assert
            _repoMock.Verify(r => r.EliminarAsync(cuenta), Times.Once);
        }

        [Test]
        public void EliminarAsync_DeberiaLanzarInvalidOperationException_SiCuentaNoExiste()
        {
            // Arrange
            _repoMock.Setup(r => r.ObtenerPorIdAsync(99)).ReturnsAsync((CuentaBancaria?)null);

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.EliminarAsync(99));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Cuenta no encontrada."));
            _repoMock.Verify(r => r.EliminarAsync(It.IsAny<CuentaBancaria>()), Times.Never);
        }

        // ---------------- DepositarAsync ----------------

        [Test]
        public async Task DepositarAsync_DeberiaIncrementarSaldo_SiDatosValidos()
        {
            // Arrange
            var cuenta = NuevaCuenta(1, "001-0001", 1000m);
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("001-0001")).ReturnsAsync(cuenta);
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 500m };

            // Act
            var mensaje = await _service.DepositarAsync(request);

            // Assert
            Assert.That(cuenta.Saldo, Is.EqualTo(1500m));
            Assert.That(mensaje, Does.Contain("Depósito exitoso"));
            Assert.That(mensaje, Does.Contain("001-0001"));
            _repoMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void DepositarAsync_DeberiaLanzarArgumentException_SiMontoNoEsPositivo(int monto)
        {
            // Arrange
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = monto };

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.DepositarAsync(request));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("El monto debe ser mayor que cero."));
            _repoMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Test]
        public void DepositarAsync_DeberiaLanzarInvalidOperationException_SiCuentaNoExiste()
        {
            // Arrange
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("NO-EXISTE")).ReturnsAsync((CuentaBancaria?)null);
            var request = new Transaccion { NumeroCuenta = "NO-EXISTE", Monto = 100m };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.DepositarAsync(request));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Cuenta no encontrada."));
            _repoMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        // ---------------- RetirarAsync ----------------

        [Test]
        public async Task RetirarAsync_DeberiaDisminuirSaldo_SiHayFondosSuficientes()
        {
            // Arrange
            var cuenta = NuevaCuenta(1, "001-0001", 1000m);
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("001-0001")).ReturnsAsync(cuenta);
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 300m };

            // Act
            var mensaje = await _service.RetirarAsync(request);

            // Assert
            Assert.That(cuenta.Saldo, Is.EqualTo(700m));
            Assert.That(mensaje, Does.Contain("Retiro exitoso"));
            _repoMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Test]
        public async Task RetirarAsync_DeberiaPermitirRetirarTodoElSaldo_SiMontoEsIgualAlSaldo()
        {
            // Arrange
            var cuenta = NuevaCuenta(1, "001-0001", 500m);
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("001-0001")).ReturnsAsync(cuenta);
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 500m };

            // Act
            await _service.RetirarAsync(request);

            // Assert
            Assert.That(cuenta.Saldo, Is.EqualTo(0m));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void RetirarAsync_DeberiaLanzarArgumentException_SiMontoNoEsPositivo(int monto)
        {
            // Arrange
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = monto };

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.RetirarAsync(request));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("El monto debe ser mayor que cero."));
            _repoMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Test]
        public void RetirarAsync_DeberiaLanzarInvalidOperationException_SiCuentaNoExiste()
        {
            // Arrange
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("NO-EXISTE")).ReturnsAsync((CuentaBancaria?)null);
            var request = new Transaccion { NumeroCuenta = "NO-EXISTE", Monto = 100m };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RetirarAsync(request));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Cuenta no encontrada."));
            _repoMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Test]
        public void RetirarAsync_DeberiaLanzarInvalidOperationException_SiFondosInsuficientes()
        {
            // Arrange
            var cuenta = NuevaCuenta(1, "001-0001", 100m);
            _repoMock.Setup(r => r.ObtenerPorNumeroAsync("001-0001")).ReturnsAsync(cuenta);
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 200m };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RetirarAsync(request));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Fondos insuficientes."));
            Assert.That(cuenta.Saldo, Is.EqualTo(100m));
            _repoMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }
    }
}
