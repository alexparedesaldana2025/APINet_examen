using APINet.Controllers;
using APINet.Models;
using APINet.Service;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Test_n00365453
{
    /// <summary>
    /// Pruebas unitarias de la capa Controller.
    /// Se simula (Mock) ICuentaService para aislar el controlador.
    /// </summary>
    [TestFixture]
    public class CuentaControllerTests
    {
        private Mock<ICuentaService> _serviceMock = null!;
        private CuentaController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _serviceMock = new Mock<ICuentaService>();
            _controller = new CuentaController(_serviceMock.Object);
        }

        private static CuentaBancaria NuevaCuenta(int id = 1, string numero = "001-0001") => new()
        {
            IdCuenta = id,
            NumeroCuenta = numero,
            Titular = "Juan Pérez",
            TipoCuenta = "Ahorros",
            Saldo = 1500m,
            Moneda = "PEN"
        };

        private static string? LeerMensaje(object? value) =>
            value?.GetType().GetProperty("mensaje")?.GetValue(value) as string;

        // ---------------- GET (listar) ----------------

        [Test]
        public async Task GetAll_DeberiaRetornarListaDeCuentas_SiExistenCuentas()
        {
            // Arrange
            var cuentas = new List<CuentaBancaria> { NuevaCuenta(1, "001-0001"), NuevaCuenta(2, "001-0002") };
            _serviceMock.Setup(s => s.ListarAsync()).ReturnsAsync(cuentas);

            // Act
            var result = await _controller.GetAll();

            // Assert
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value, Is.EquivalentTo(cuentas));
            _serviceMock.Verify(s => s.ListarAsync(), Times.Once);
        }

        [Test]
        public async Task GetAll_DeberiaRetornarListaVacia_SiNoExistenCuentas()
        {
            // Arrange
            _serviceMock.Setup(s => s.ListarAsync()).ReturnsAsync(new List<CuentaBancaria>());

            // Act
            var result = await _controller.GetAll();

            // Assert
            Assert.That(result.Value, Is.Empty);
        }

        // ---------------- GET por id ----------------

        [Test]
        public async Task GetById_DeberiaRetornarCuenta_SiExiste()
        {
            // Arrange
            var cuenta = NuevaCuenta();
            _serviceMock.Setup(s => s.ObtenerPorIdAsync(1)).ReturnsAsync(cuenta);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            Assert.That(result.Value, Is.SameAs(cuenta));
            Assert.That(result.Result, Is.Null);
        }

        [Test]
        public async Task GetById_DeberiaRetornarNotFound_SiNoExiste()
        {
            // Arrange
            _serviceMock.Setup(s => s.ObtenerPorIdAsync(99)).ReturnsAsync((CuentaBancaria?)null);

            // Act
            var result = await _controller.GetById(99);

            // Assert
            Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        }

        // ---------------- POST (crear) ----------------

        [Test]
        public async Task Create_DeberiaRetornarCreatedAtAction_SiDatosValidos()
        {
            // Arrange
            var entrada = NuevaCuenta(0);
            var creada = NuevaCuenta(5);
            _serviceMock.Setup(s => s.CrearAsync(entrada)).ReturnsAsync(creada);

            // Act
            var result = await _controller.Create(entrada);

            // Assert
            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
            var created = (CreatedAtActionResult)result;
            Assert.That(created.ActionName, Is.EqualTo(nameof(CuentaController.GetById)));
            Assert.That(created.RouteValues!["id"], Is.EqualTo(5));
            Assert.That(created.Value, Is.SameAs(creada));
        }

        [Test]
        public async Task Create_DeberiaRetornarBadRequest_SiServicioLanzaExcepcion()
        {
            // Arrange
            var entrada = NuevaCuenta(0);
            _serviceMock.Setup(s => s.CrearAsync(entrada))
                        .ThrowsAsync(new InvalidOperationException("Ya existe una cuenta con el número '001-0001'."));

            // Act
            var result = await _controller.Create(entrada);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            var bad = (BadRequestObjectResult)result;
            Assert.That(bad.Value, Is.EqualTo("Ya existe una cuenta con el número '001-0001'."));
        }

        // ---------------- PUT (actualizar) ----------------

        [Test]
        public async Task Update_DeberiaRetornarNoContent_SiActualizacionExitosa()
        {
            // Arrange
            var dto = new ActualizarCuentaDto { Titular = "Nuevo", TipoCuenta = "Corriente", Moneda = "USD" };
            _serviceMock.Setup(s => s.ActualizarAsync(1, dto)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(1, dto);

            // Assert
            Assert.That(result, Is.TypeOf<NoContentResult>());
            _serviceMock.Verify(s => s.ActualizarAsync(1, dto), Times.Once);
        }

        [Test]
        public async Task Update_DeberiaRetornarBadRequest_SiCuentaNoExiste()
        {
            // Arrange
            var dto = new ActualizarCuentaDto { Titular = "Nuevo" };
            _serviceMock.Setup(s => s.ActualizarAsync(99, dto))
                        .ThrowsAsync(new InvalidOperationException("Cuenta no encontrada."));

            // Act
            var result = await _controller.Update(99, dto);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo("Cuenta no encontrada."));
        }

        // ---------------- DELETE ----------------

        [Test]
        public async Task Delete_DeberiaRetornarNoContent_SiEliminacionExitosa()
        {
            // Arrange
            _serviceMock.Setup(s => s.EliminarAsync(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.That(result, Is.TypeOf<NoContentResult>());
            _serviceMock.Verify(s => s.EliminarAsync(1), Times.Once);
        }

        [Test]
        public async Task Delete_DeberiaRetornarBadRequest_SiCuentaNoExiste()
        {
            // Arrange
            _serviceMock.Setup(s => s.EliminarAsync(99))
                        .ThrowsAsync(new InvalidOperationException("Cuenta no encontrada."));

            // Act
            var result = await _controller.Delete(99);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo("Cuenta no encontrada."));
        }

        // ---------------- POST depositar ----------------

        [Test]
        public async Task Depositar_DeberiaRetornarOkConMensaje_SiDepositoExitoso()
        {
            // Arrange
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 100m };
            _serviceMock.Setup(s => s.DepositarAsync(request)).ReturnsAsync("Depósito exitoso");

            // Act
            var result = await _controller.Depositar(request);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(LeerMensaje(((OkObjectResult)result).Value), Is.EqualTo("Depósito exitoso"));
        }

        [Test]
        public async Task Depositar_DeberiaRetornarBadRequest_SiMontoInvalido()
        {
            // Arrange
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 0m };
            _serviceMock.Setup(s => s.DepositarAsync(request))
                        .ThrowsAsync(new ArgumentException("El monto debe ser mayor que cero."));

            // Act
            var result = await _controller.Depositar(request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo("El monto debe ser mayor que cero."));
        }

        // ---------------- POST retirar ----------------

        [Test]
        public async Task Retirar_DeberiaRetornarOkConMensaje_SiRetiroExitoso()
        {
            // Arrange
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 50m };
            _serviceMock.Setup(s => s.RetirarAsync(request)).ReturnsAsync("Retiro exitoso");

            // Act
            var result = await _controller.Retirar(request);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(LeerMensaje(((OkObjectResult)result).Value), Is.EqualTo("Retiro exitoso"));
        }

        [Test]
        public async Task Retirar_DeberiaRetornarBadRequest_SiFondosInsuficientes()
        {
            // Arrange
            var request = new Transaccion { NumeroCuenta = "001-0001", Monto = 99999m };
            _serviceMock.Setup(s => s.RetirarAsync(request))
                        .ThrowsAsync(new InvalidOperationException("Fondos insuficientes."));

            // Act
            var result = await _controller.Retirar(request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo("Fondos insuficientes."));
        }
    }
}
