public class RequestModel
{
    public string RequestId { get; set; }
    public string ServiceName { get; set; }
    public string Endpoint { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
    public string HttpMethod { get; set; }
}