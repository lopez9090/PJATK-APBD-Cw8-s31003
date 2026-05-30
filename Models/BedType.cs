using System;
using System.Collections.Generic;

namespace PJATK_APBD_Cw8_s31003.Models;

public partial class BedType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
}
