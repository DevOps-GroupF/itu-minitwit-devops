using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using MiniTwit.Validations.User;

namespace MiniTwit.Models.ViewModels
{
    public class SimulatorRegisterViewModel
    {   
        [UserName]
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z0-9_ ]+$", 
         ErrorMessage = "Characters are not allowed.")]
        [StringLength(16)]
        [Required(ErrorMessage = "You have to enter a username")]
        public string username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "You have to enter a valid email address")]
        [StringLength(32)]
        [EmailAddress(ErrorMessage = "You have to enter a valid email address")]
        public string email { get; set; }

        [BindProperty]
        [StringLength(32)]
        [Required(ErrorMessage = "You have to enter a password")]
        public string pwd { get; set; }
    }
}
