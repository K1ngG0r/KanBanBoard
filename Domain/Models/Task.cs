namespace Domain.Models;

public class Task
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name {  get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }

    public Guid UserId { get; set; }

    public string Status { get; set; } 

    public Task() { }

    public Task(string name, string desc, string shortDesc, string status)
    {
        Name = name;
        Description = desc;
        ShortDescription = shortDesc;
        Status = status;
    }
}
