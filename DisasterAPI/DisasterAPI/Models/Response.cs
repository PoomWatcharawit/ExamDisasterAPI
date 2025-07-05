namespace DisasterAPI.Models
{
    public class Response
    {
        public string Message { set; get; }
        public bool Success { set; get; }
        public object Content { set; get; }

        public Response(bool success, string message)
        {
            this.Success = success;
            this.Message = message;
        }
        public Response(bool status, string message, object data)
        {
            this.Success = status;
            this.Message = message;
            this.Content = data;
        }
        public bool IsValid()
        {
            return Success;
        }

        public string message()
        {
            return Message;
        }
    }
}
