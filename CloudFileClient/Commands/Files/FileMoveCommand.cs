namespace CloudFileClient.Commands.Files;

public class FileMoveCommand
{
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        
    }
}