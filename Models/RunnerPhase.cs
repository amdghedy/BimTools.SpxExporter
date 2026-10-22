namespace BimTools.SpxExporter.Models
{
    public enum RunnerPhase
    {
        WaitingForStart = 0,
        NextItem = 1,
        AwaitActivation = 2,
        Export = 3,
        Polling = 4,
        PostProcess = 5,
        Restoring = 6,
        WaitingForEvent = 7
    }
}
