using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models.ViewModels;

namespace MiniTwit.Pages.Shared.Components.Messages;

public class MessagesViewComponent : ViewComponent
{
    public MessagesViewComponent() { }

    private string getGravatar(string email, int size = 80)
    {
        MD5 hasher = MD5.Create();
        byte[] bytes_data = hasher.ComputeHash(Encoding.Default.GetBytes(email));
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < bytes_data.Length; i++)
        {
            result.Append(bytes_data[i].ToString("x2"));
        }
        return "http://www.gravatar.com/avatar/"
            + result.ToString()
            + "?d=identicon&s="
            + size.ToString();
    }

    public IViewComponentResult Invoke(IEnumerable<TwitViewModel> messages)
    {
        return View("Default", messages);
    }
}
