namespace Domain.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Login {  get; set; }
    
    public string Name { get; set; }
    
    public List<Task> Tasks { get; set; }
    
    public string HashPassword {  get; set; }
    public Account() { }
    public Account(string username, string login, string hashPassword)
    {
        Name = username ?? $"User_{Id.ToString()}";
        Login = login;
        HashPassword = hashPassword;
        Tasks = new List<Task>();
    }
}