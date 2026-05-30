namespace PJATK_APBD_Cw8_s31003.DTOs;

public class BedAssignmentPostDto
{
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
    public string BedType { get; set; } = null!;
    public string Ward { get; set; } = null!;
}