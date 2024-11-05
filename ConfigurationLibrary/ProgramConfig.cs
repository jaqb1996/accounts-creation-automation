namespace ConfigurationLibrary
{
    public class ProgramConfig
    {
        public RunningSectionOption RunActiveDirectory { get; set; }
        public RunningSectionOption RunAmms { get; set; }
        public RunningSectionOption RunPControl { get; set; }
        public RunningSectionOption RunZimbra { get; set; }
    }

    public enum RunningSectionOption
    {
        Run,
        DoNotRun,
        Ask
    }
}
