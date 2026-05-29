using Assignment1_Repository.Models;

namespace Assignmet1_Presentation.Models;

public class ChatIndexViewModel
{
    public Subject? Subject { get; set; }
    public List<Session> Sessions { get; set; } = new();
}
