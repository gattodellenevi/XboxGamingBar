namespace Shared.Data
{
    public struct RunningGameOptions
    {
        public int ProcessId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string AumId { get; set; }
        public uint FPS { get; set; }
        public bool IsForeground { get; set; }
    }
}
