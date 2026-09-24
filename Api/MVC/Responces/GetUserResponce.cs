using Domain.Models;

namespace Api.MVC.Responces;

public class GetUserResponce
{
 
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Login { get; set; }
    public string Name { get; set; }
    public List<Domain.Models.Task> Tasks { get; set; } = new List<Domain.Models.Task>();

    public GetUserResponce(Account user)
    {
        Login = user.Login;
        Name = user.Name;
        Tasks = user.Tasks;
    }
}
