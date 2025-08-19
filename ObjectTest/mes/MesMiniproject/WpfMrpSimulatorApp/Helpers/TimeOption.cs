namespace WpfMrpSimulatorApp.Helpers
{
    public class TimeOption
    {
        public TimeOnly Time { get; set; }  //  "09:00" != 09:00 9:00 AM
        public string Display { get; set; }
    }
}
