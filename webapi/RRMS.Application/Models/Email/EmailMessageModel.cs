namespace RRMS.Application.Models.Email;

public class EmailMessageModel
{
    public required string Recipient { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
}
