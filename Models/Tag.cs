using System;
using System.Collections.Generic;

namespace ProjektM.Models;

public partial class Tag
{
    public int TagId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
