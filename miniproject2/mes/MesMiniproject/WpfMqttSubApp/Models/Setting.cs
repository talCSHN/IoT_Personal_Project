using System;
using System.Collections.Generic;

namespace WpfMqttSubApp.Models;

public partial class Setting
{
    public string BasicCode { get; set; } = null!;

    public string CodeName { get; set; } = null!;

    /// <summary>
    /// 코드 설명
    /// </summary>
    public string? CodeDesc { get; set; }

    public DateTime? RegDt { get; set; }

    public DateTime? ModDt { get; set; }
}
