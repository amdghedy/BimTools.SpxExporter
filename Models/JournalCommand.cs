namespace BimTools.SpxExporter.Models
{
    public sealed class JournalCommand
    {
        public string CommandId { get; set; }

        public string ClassName { get; set; }

        public bool LooksLikeSynchro { get; set; }

        public string Display
        {
            get { return string.IsNullOrEmpty(ClassName) ? CommandId : CommandId + "   [" + ClassName + "]"; }
        }
    }
}
