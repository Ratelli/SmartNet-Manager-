namespace SmartNetManager.Modelos;

// Classe exigida pelo professor
public class Dispositivo
{
    public string MacAddress { get; set; } = string.Empty; // Ex: "AA:BB:CC:11:22"
    public string EnderecoIP { get; set; } = string.Empty; // Ex: "192.168.0.10"
    public string Nome { get; set; } = string.Empty;       // Ex: "PC-Contabilidade"
    public string Fabricante { get; set; } = string.Empty; // Ex: "Dell", "Apple", "Samsung"
    public double ConsumoBanda { get; set; }               // Em Mbps
    public DateTime DataConexao { get; set; }
}
