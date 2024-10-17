using Newtonsoft.Json;

namespace LogGrouper.Models.Business
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ConnString { get; set; }
        public string Warehouse { get; set; }
        public string StorerName { get; set; }
        public string LoginUrl { get; set; }
        public string LabelPrefix { get; set; }
        public string GetInfoQuery { get; set; }
        public string ApiLoginUsername { get; set; }
        public string ApiLoginPassword { get; set; }
    }
}
