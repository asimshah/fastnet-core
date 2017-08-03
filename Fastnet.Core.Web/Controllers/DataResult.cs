namespace Fastnet.Core.Web.Controllers
{
    public class DataResult
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string exceptionMessage { get; set; }
        public object data { get; set; }
        public override string ToString()
        {
            if(success)
            {
                string text = "";
                if(this.data != null)
                {
                    text = $", {this.data?.ToString()}{(string.IsNullOrWhiteSpace(message) ? "" : "message=" + message)}";
                }
                return $"{{success{text}}}";
            }
            else
            {
                return $"{{failed, {(string.IsNullOrWhiteSpace(message) ? "" : "message=" + message)}, {(string.IsNullOrWhiteSpace(exceptionMessage) ? "" : "exceptionMessage=" + exceptionMessage)} }}";
            }
        }
    }

}
