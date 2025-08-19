using System;
using System.Collections.Generic;

namespace WpfMrpSimulatorApp.Models;

public partial class Setting
{
    /// <summary>
    /// 기본코드
    /// </summary>
    public string BasicCode { get; set; } = null!;

    /// <summary>
    /// 코드명
    /// </summary>
    public string CodeName { get; set; } = null!;

    /// <summary>
    /// 코드설명
    /// </summary>
    public string? CodeDesc { get; set; }

    public DateTime? RegDt { get; set; }

    public DateTime? ModDt { get; set; }
}
