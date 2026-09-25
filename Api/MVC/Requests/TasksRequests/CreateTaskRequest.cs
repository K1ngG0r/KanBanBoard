namespace Api.Requests;

public class CreateTaskRequest
{
    public string Name {  get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }
}
