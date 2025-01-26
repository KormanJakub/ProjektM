namespace ProjektM.Requests;

public class ResetPasswordRequest
{
    public int UserId { get; set; }
    public string ResetToken { get; set; }
    public string NewPassword { get; set; }
}