namespace APINet.Models
{
    public class CuentaBancaria
    {
        public int IdCuenta { get; set; }
        public string NumeroCuenta { get; set; }
        public string Titular { get; set; }
        public string TipoCuenta { get; set; }
        public decimal Saldo { get; set; }
        public string Moneda { get; set; }
    }
}
