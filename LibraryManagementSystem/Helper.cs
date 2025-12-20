using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;
using System.Text.Json;

namespace LibraryManagementSystem;

public static class Helper
{
    public static ValidationResult ValidateImage(IFormFile image)
    {
        // 1. Check File Extension
        string extension = Path.GetExtension(image.FileName);
        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return new ValidationResult("Only JPEG, JPG, and PNG files are allowed.");
        }

        // 2. Check MIME Type
        if (image.ContentType is null || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult("The file must be an image.");
        }

        // 3. Check File Size (1MB Limit)
        if (image.Length > 1 * 1024 * 1024)
        {
            return new ValidationResult("The file size cannot exceed 1MB.");
        }

        // 4. Check Magic Bytes
        try
        {
            using var stream = image.OpenReadStream();
            byte[] header = new byte[3];
            stream.ReadExactly(header, 0, 3);

            // Valid Signatures
            bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E;

            if (!isJpeg && !isPng)
            {
                return new ValidationResult("The file signature is invalid.");
            }
        }
        catch
        {
            return new ValidationResult("Cannot read file stream.");
        }

        return ValidationResult.Success!;
    }

    public static string SaveImage(IFormFile image, string folder, string webRootPath)
    {
        // 1. Generate unique filename
        string filename = Guid.NewGuid().ToString("N") + ".jpg";

        // 2. Paths
        string folderPath = Path.Combine(webRootPath, "images", folder);
        string fullPath = Path.Combine(folderPath, filename);

        // 3. Create Directory
        Directory.CreateDirectory(folderPath);

        // 4. Save and Re-encode
        using var stream = image.OpenReadStream();
        using var img = Image.Load(stream);
        img.Save(fullPath);

        return filename;
    }

    public static string GenerateNewId(string? lastId, char prefixChar)
    {
        // 1. Normalize prefixChar
        char pChar = char.ToUpper(prefixChar);

        // 2. Empty table handling
        if (string.IsNullOrEmpty(lastId))
        {
            return $"{pChar}0001A";
        }

        // 3. Normalize lastId
        lastId = lastId.ToUpper();

        // 4. Validate Format
        if (lastId.Length != 6 || lastId[0] != pChar)
        {
            throw new InvalidOperationException($"Existing ID '{lastId}' is invalid. Expected format like '{pChar}0001A'.");
        }

        // 5. Parse Parts
        if (!int.TryParse(lastId.Substring(1, 4), out int currentNum))
        {
            throw new InvalidOperationException($"Existing ID '{lastId}' contains an invalid number section.");
        }
        char currentSuffix = lastId[5];

        // 6. Increment
        int nextNum = currentNum + 1;
        char nextSuffix = currentSuffix;
        if (nextNum > 9999)
        {
            nextNum = 1;
            nextSuffix++;
        }

        // 7. Overflow Guard
        if (nextSuffix > 'Z')
        {
            throw new InvalidOperationException($"ID capacity reached for prefix '{pChar}' (Max: {pChar}9999Z).");
        }

        // 8. Format Result
        return $"{pChar}{nextNum:D4}{nextSuffix}";
    }

    public class ISBNValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string isbn) return ValidationResult.Success!;

            // 1. Normalize
            string cleanISBN = isbn.Replace("-", "").Trim().Replace(" ", "").ToUpper();

            // 2. Route Validation
            return cleanISBN.Length switch
            {
                13 => ValidateISBN13(cleanISBN) ?? ValidationResult.Success!,
                10 => ValidateISBN10(cleanISBN) ?? ValidationResult.Success!,
                _ => new ValidationResult("ISBN must be exactly 10 or 13 digits."),
            };
        }

        private static ValidationResult? ValidateISBN13(string isbn)
        {
            if (!long.TryParse(isbn, out _)) return new ValidationResult("ISBN-13 must only contain digits.");

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = isbn[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int remainder = sum % 10;
            int checkDigit = (10 - remainder) % 10;
            int actualCheckDigit = isbn[12] - '0';

            if (checkDigit != actualCheckDigit)
            {
                return new ValidationResult("Invalid ISBN-13 checksum.");
            }

            return null;
        }

        private static ValidationResult? ValidateISBN10(string isbn)
        {
            int sum = 0;

            for (int i = 0; i < 9; i++)
            {
                char c = isbn[i];
                if (!char.IsDigit(c)) return new ValidationResult("First 9 characters of ISBN-10 must be digits.");

                sum += (c - '0') * (10 - i);
            }
            
            char lastChar = isbn[9];
            int lastValue;

            if (lastChar == 'X')
            {
                lastValue = 10;
            }
            else if (char.IsDigit(lastChar))
            {
                lastValue = lastChar - '0';
            }
            else
            {
                return new ValidationResult("Last character of ISBN-10 must be a digit or 'X'.");
            }

            sum += lastValue;

            if (sum % 11 != 0)
            {
                return new ValidationResult("Invalid ISBN-10 checksum.");
            }

            return null;
        }
    }
    //ReCaptcha Implementation
    public static async Task<bool> VerifyReCaptcha(string responseToken, string secretKey)
    {
        if (string.IsNullOrEmpty(responseToken)) return false;

        using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={responseToken}");

            using (var document = JsonDocument.Parse(response))
            {
                // Check if "success" is true
                return document.RootElement.GetProperty("success").GetBoolean();
            }
        }
    }
}