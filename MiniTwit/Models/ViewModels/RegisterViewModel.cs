using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;

namespace MiniTwit.Models.ViewModels
{
    public class RegisterViewModel
    {
        [BindProperty]
        [StringLength(16)]
        [Required(ErrorMessage = "You have to enter a username")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "You have to enter a valid email address")]
        [StringLength(32)]
        [EmailAddress(ErrorMessage = "You have to enter a valid email address")]
        public string Email { get; set; }

        [BindProperty]
        [StringLength(32)]
        [Required(ErrorMessage = "You have to enter a password")]
        public string Password { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please confirm your password")]
        [StringLength(32)]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match")]
        public string Password2 { get; set; }
    }
}
