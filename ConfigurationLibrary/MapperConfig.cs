namespace ConfigurationLibrary
{
    public class MapperConfig
    {
        public string PagerEmptyValue { get; set; }
        public Dictionary<(string, string), string[]> AdGroupsForPositionAndArea { get; set; }
        public Dictionary<string, string[]> AdGroupsForPosition { get; set; }
        public Dictionary<string, string> PathForDepartment { get; set; }
        public Dictionary<(string, string), string[]> PcControlRightsForPositionAndArea { get; set; }
        public string[] PcControlRightsForAllUsers { get; set; }
        public Dictionary<string, string> AmmsPersonnelUnitForArea { get; set; }
        public Dictionary<(string, string), string[]> AmmsGroupsForPositionAndArea { get; set; }
        public Dictionary<string, string[]> AmmsGroupsForPosition { get; set; }
        public Dictionary<string, string> AmmsPersonnelKindForPosition { get; set; }
        public Dictionary<string, string> AmmsPersonnelFunctionForPosition { get; set; }
    }
}
