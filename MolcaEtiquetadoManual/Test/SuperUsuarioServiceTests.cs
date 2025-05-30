using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Services;

namespace MolcaEtiquetadoManual.Tests
{
    [TestClass]
    public class SuperUsuarioServiceTests
    {
        private Mock<ILogService> _mockLogService;
        private SuperUsuarioService _superUsuarioService;

        [TestInitialize]
        public void Setup()
        {
            _mockLogService = new Mock<ILogService>();
            _superUsuarioService = new SuperUsuarioService(_mockLogService.Object);
        }

        [TestMethod]
        public void ValidarSuperUsuario_ConNombreIncorrecto_DebeRetornarNull()
        {
            // Arrange
            string nombreUsuario = "usuario_normal";
            string contraseña = "cualquier_contraseña";

            // Act
            var resultado = _superUsuarioService.ValidarSuperUsuario(nombreUsuario, contraseña);

            // Assert
            Assert.IsNull(resultado);
        }

        [TestMethod]
        public void ValidarSuperUsuario_ConNombreCorrectoYContraseñaIncorrecta_DebeRetornarNull()
        {
            // Arrange
            string nombreUsuario = "ketan";
            string contraseña = "contraseña_incorrecta";

            // Act
            var resultado = _superUsuarioService.ValidarSuperUsuario(nombreUsuario, contraseña);

            // Assert
            Assert.IsNull(resultado);
        }

        [TestMethod]
        public void ValidarSuperUsuario_ConCredencialesCorrectas_DebeRetornarUsuario()
        {
            // Arrange
            string nombreUsuario = "ketan";
            string contraseñaCorrecta = _superUsuarioService.ObtenerContraseñaActual();

            // Act
            var resultado = _superUsuarioService.ValidarSuperUsuario(nombreUsuario, contraseñaCorrecta);

            // Assert
            Assert.IsNotNull(resultado);
            Assert.AreEqual("ketan", resultado.NombreUsuario);
            Assert.AreEqual("Super Administrador", resultado.Rol);
            Assert.AreEqual(-1, resultado.Id);
            Assert.IsTrue(resultado.Activo);
        }

        [TestMethod]
        public void GenerarContraseñaParaFecha_ConFechaConocida_DebeGenerarContraseñaCorrecta()
        {
            // Arrange
            var fecha = new DateTime(2025, 5, 22); // 22/05/25
            string contraseñaEsperada = "520522"; // 220525 al revés

            // Act
            string contraseñaGenerada = _superUsuarioService.GenerarContraseñaParaFecha(fecha);

            // Assert
            Assert.AreEqual(contraseñaEsperada, contraseñaGenerada);
        }

        [TestMethod]
        public void EsSuperUsuario_ConNombreKetan_DebeRetornarTrue()
        {
            // Arrange
            string nombreUsuario = "ketan";

            // Act
            bool resultado = _superUsuarioService.EsSuperUsuario(nombreUsuario);

            // Assert
            Assert.IsTrue(resultado);
        }

        [TestMethod]
        public void EsSuperUsuario_ConNombreKetanEnMayusculas_DebeRetornarTrue()
        {
            // Arrange
            string nombreUsuario = "KETAN";

            // Act
            bool resultado = _superUsuarioService.EsSuperUsuario(nombreUsuario);

            // Assert
            Assert.IsTrue(resultado);
        }

        [TestMethod]
        public void EsSuperUsuario_ConOtroNombre_DebeRetornarFalse()
        {
            // Arrange
            string nombreUsuario = "admin";

            // Act
            bool resultado = _superUsuarioService.EsSuperUsuario(nombreUsuario);

            // Assert
            Assert.IsFalse(resultado);
        }

        [TestMethod]
        public void ObtenerContraseñaActual_DebeGenerarContraseña6Digitos()
        {
            // Act
            string contraseña = _superUsuarioService.ObtenerContraseñaActual();

            // Assert
            Assert.IsNotNull(contraseña);
            Assert.AreEqual(6, contraseña.Length);
            Assert.IsTrue(contraseña.All(char.IsDigit), "La contraseña debe contener solo dígitos");
        }

        [TestMethod]
        public void ValidarSuperUsuario_ConNombreNulo_DebeRetornarNull()
        {
            // Arrange
            string nombreUsuario = null;
            string contraseña = "cualquier_contraseña";

            // Act
            var resultado = _superUsuarioService.ValidarSuperUsuario(nombreUsuario, contraseña);

            // Assert
            Assert.IsNull(resultado);
        }

        [TestMethod]
        public void ValidarSuperUsuario_ConContraseñaNula_DebeRetornarNull()
        {
            // Arrange
            string nombreUsuario = "ketan";
            string contraseña = null;

            // Act
            var resultado = _superUsuarioService.ValidarSuperUsuario(nombreUsuario, contraseña);

            // Assert
            Assert.IsNull(resultado);
        }

        [TestMethod]
        public void GenerarContraseñaParaFecha_ConDiferentesFechas_DebeGenerarContraseñasCorrectas()
        {
            // Test casos múltiples
            var casos = new[]
            {
                new { Fecha = new DateTime(2025, 1, 1), Esperada = "520100" }, // 01/01/25 -> 010125 -> 520100
                new { Fecha = new DateTime(2025, 12, 31), Esperada = "523112" }, // 31/12/25 -> 311225 -> 523112
                new { Fecha = new DateTime(2025, 6, 15), Esperada = "520615" }  // 15/06/25 -> 150625 -> 520615
            };

            foreach (var caso in casos)
            {
                // Act
                string resultado = _superUsuarioService.GenerarContraseñaParaFecha(caso.Fecha);

                // Assert
                Assert.AreEqual(caso.Esperada, resultado,
                    $"Falla para fecha {caso.Fecha:dd/MM/yyyy}. Esperada: {caso.Esperada}, Obtenida: {resultado}");
            }
        }
    }
}