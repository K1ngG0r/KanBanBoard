namespace Api.Requests;

public class ChangeStatusTaskRequest
{
    public Guid TaskId{ get; set; }

    public string NewStatus{ get;set;}
}
