using System;

namespace ShopIt.Authentication.Application.Models;

public record CredentialValidationRequest(string Username, string Password);
