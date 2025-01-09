namespace CommunicationTool.Model
{
    public class BluetoothDevice
    {
        public string? Name { get; set; }
        public string Addr { get; set; } = null!;

        public override string ToString()
        {
            return Name ?? Addr;
        }
    }
}
