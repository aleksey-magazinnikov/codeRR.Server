﻿using System.ComponentModel.DataAnnotations;

namespace codeRR.Server.Web.Models.Account
{
    public class LoginViewModel
    {
        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        [Required]
        public string UserName { get; set; }
    }
}