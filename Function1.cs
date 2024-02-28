using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using SendGrid.Helpers.Mail;
using SendGrid;
using Azure;
using System.Threading.Tasks;

public class BlobTriggerFunction
{
    private readonly ISendGridClient _sendGridClient;

    public BlobTriggerFunction(ISendGridClient sendGridClient)
    {
        _sendGridClient = sendGridClient;
    }

    [Function("BlobTriggerFunction")]
    public async Task Run(
        [BlobTrigger("file-storage/{name}", Connection = "AzureWebJobsStorage")] string myBlob,
        string name,
         // Додавання нового параметра для адреси електронної пошти
        FunctionContext context)
    {
        var logger = context.GetLogger("BlobTriggerFunction");
        logger.LogInformation($"C# Blob trigger function Processed blob\n Name:{name}");

       

        // Generate SAS token
        var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        var blobContainerClient = blobServiceClient.GetBlobContainerClient("file-storage");
        var blobClient = blobContainerClient.GetBlobClient(name);

        var blobProperties = await blobClient.GetPropertiesAsync();
        string emailAddress = blobProperties.Value.Metadata["email"];

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobContainerClient.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        var sasToken = blobClient.GenerateSasUri(sasBuilder);

        // Construct URL
        string blobUrl = sasToken.ToString();

        // Prepare email
        var message = new SendGridMessage();
        message.AddTo(emailAddress);
        message.PlainTextContent = $"File has been uploaded. You can download it from {blobUrl}";
        message.Subject = "New file uploaded";
        message.From = new EmailAddress("ok.useruser26@gmail.com");

        // Send email
        try
        {
            var response = await _sendGridClient.SendEmailAsync(message);
            logger.LogInformation($"SendGrid API response: {response.StatusCode}");
            logger.LogInformation($"SendGrid API response body: {await response.Body.ReadAsStringAsync()}");
            logger.LogInformation("Email sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error sending email: {ex.Message}");
            // Handle or rethrow the exception
        }
    }
}
