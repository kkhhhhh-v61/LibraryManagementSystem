using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem;

public class ISBNValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success!;

        string isbn = value.ToString()!;

        // 1. Remove hyphens and spaces to get raw numbers
        string cleanIsbn = isbn.Replace("-", "").Replace(" ", "").ToUpper();

        // 2. Check Length
        if (cleanIsbn.Length != 10 && cleanIsbn.Length != 13)
        {
            return new ValidationResult("ISBN must be exactly 10 or 13 digits.");
        }

        // 3. Route to corresponding algorithm validation
        string? error = cleanIsbn.Length == 13
                      ? ValidateISBN13(cleanIsbn)
                      : ValidateISBN10(cleanIsbn);

        if (error != null)
        {
            return new ValidationResult(error);
        }

        return ValidationResult.Success!;
    }

    // Alhgorithm for ISBN-13 (mod 10)
    private static string? ValidateISBN13(string isbn)
    {
        // 1. Check format for only digits
        if (!long.TryParse(isbn, out _))
        {
            return "ISBN-13 must only contain digits.";
        }

        // 2. Checksum
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = int.Parse(isbn[i].ToString());
            // Weight 1 for even indices (0, 2, 4...), Weight 3 for odd indices (1, 3, 5...)
            sum += (i % 2 == 0) ? digit * 1 : digit * 3;
        }

        int remainder = sum % 10;
        int checkDigit = (10 - remainder) % 10;

        if (checkDigit != int.Parse(isbn[12].ToString()))
        {
            return "The check digit (last digit) is mathematically incorrect.";
        }

        return null; // Success
    }

    // Algorithm for ISBN-10 (mod 11)
    private static string? ValidateISBN10(string isbn)
    {
        // 1. Check format (first 9 characters must be digits)
        for (int i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return "The first 9 characters must be digits.";
            }
        }

        // 2. Check format (last character must be digit or 'X')
        char lastChar = isbn[9];
        if (lastChar != 'X' && !char.IsDigit(lastChar))
        {
            return "The last character must be a digit or 'X'.";
        }

        // 3. Checksum
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += int.Parse(isbn[i].ToString()) * (10 - i);
        }

        if (lastChar == 'X')
        {
            sum += 10;
        }
        else
        {
            sum += int.Parse(lastChar.ToString());
        }

        if (sum % 11 != 0)
        {
            return "The check digit (last digit) is mathematically incorrect.";
        }

        return null; // Success
    }
}
