using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;

namespace LibraryManagementSystem;

public class Helper
{
    public static ValidationResult ValidateImage(IFormFile image)
    {
        // 1. Check the file extension
        string extension = Path.GetExtension(image.FileName).ToUpper();
        if (extension != ".JPEG" && extension != ".JPG" && extension != ".PNG")
        {
            return new ValidationResult("Only JPEG, JPG, and PNG files are allowed.");
        }

        // 2. Check the file content type
        if (image.ContentType == null || !image.ContentType.StartsWith("image/"))
        {
            return new ValidationResult("The file must be an image.");
        }

        // 3. Check the file size
        if (image.Length > 1 * 1024 * 1024)
        {
            return new ValidationResult("The file size cannot be more than 1MB.");
        }

        // 4. Check the file type using magic bytes
        byte[] magicBytes = [0xFF, 0xD8, 0xFF];
        byte[] imageBytes = new byte[magicBytes.Length];
        using (var stream = image.OpenReadStream())
        {
            stream.ReadExactly(imageBytes, 0, magicBytes.Length);
        }
        if (!magicBytes.SequenceEqual(imageBytes))
        {
            return new ValidationResult("The file must be a valid image.");
        }

        return ValidationResult.Success!;
    }

    public static string SaveImage(IFormFile image, string folder, string webRootPath)
    {
        // 1. Generate a GUID file name and force the extension to .jpg
        string filename = Guid.NewGuid().ToString("N") + ".jpg";

        // 2. Create the full file path
        string folderPath = Path.Combine(webRootPath, "images", folder);
        string fullPath = Path.Combine(folderPath, filename);

        // 3. Ensure directory exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 4. Save the Image
        using (var stream = image.OpenReadStream())
        using (var img = Image.Load(stream))
        {
            img.Save(fullPath);
        }

        return filename;
    }
}

public class ISBNValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success!;

        string isbn = value.ToString()!;

        // 1. Remove hyphens and spaces to get raw numbers
        string cleanIsbn = isbn.Replace("-", "").Trim().Replace(" ", "").ToUpper();

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
