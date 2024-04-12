using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MiniTwit.Validations.User
{
    public class UserNameAttribute : ValidationAttribute
    {
        public UserNameAttribute() { }

        public string GetErrorMessage(string username) => $"you cannot name yourself {username}";

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext
        )
        {
            if (value == null || value.ToString() == null)
            {
                // Handle the case where 'value' is null
                return new ValidationResult("Value cannot be null");
            }

            string? _tmp = value.ToString();
            string username = _tmp ?? "";

            if (NotAllowedWords().Contains(username.ToLower()))
            {
                return new ValidationResult(GetErrorMessage(username));
            }
            return ValidationResult.Success;
        }

        private static List<String> NotAllowedWords()
        {
            return new List<String>()
            {
                "api",
                "register",
                "public",
                "login",
                "logout",
                "follow",
                "flw",
                "metrics",
            };
        }
    }
}

